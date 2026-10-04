// backend/src/Sentinel.Api/Program.cs
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sentinel.Api;
using Sentinel.Api.Auth;
using Sentinel.Identity;
using Sentinel.Identity.Data;
using Sentinel.Identity.Keycloak;
using Sentinel.Identity.Onboarding;
using Sentinel.Identity.Organizations;
using Sentinel.Identity.SupportEngagements;
using Sentinel.Licensing;

var builder = WebApplication.CreateBuilder(args);

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
var keycloakAudience = builder.Configuration["Keycloak:Audience"]!;
var keycloakBaseUrl = new Uri(keycloakAuthority).GetLeftPart(UriPartial.Authority);

// ---------------------------------------------------------------- Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = keycloakAudience;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Keycloak puts realm roles in the nested `realm_access.roles` claim, which
        // ASP.NET does not turn into role claims. Without this every
        // [Authorize(Policy = ...)] that calls RequireRole returns 403.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                KeycloakRoleClaims.Apply(ctx.Principal);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerOnly", p => p.RequireRole("owner"));
    options.AddPolicy("SentinelCompany", p => p.RequireRole("owner", "support-team"));
    options.AddPolicy("CsoOrAbove", p => p.RequireRole("owner", "cso"));
    options.AddPolicy("SecurityAdministratorOrAbove", p =>
        p.RequireRole("owner", "cso", "security-administrator"));
    options.AddPolicy("AnyOrganizationRole", p =>
        p.RequireRole("cso", "security-administrator", "security-analyst"));
});

// ---------------------------------------------------------------- CORS
// The dashboard runs on a different origin (localhost:3000 in dev).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
    options.AddPolicy("Dashboard", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ---------------------------------------------------------------- Rate limiting
// The application form is public; keep it from being used as a spam cannon.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("applications-submit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10)
            }));
});

// ---------------------------------------------------------------- Errors
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---------------------------------------------------------------- Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddSingleton<ISentinelDataSource, SentinelDataSource>();
builder.Services.AddScoped<IOrganizationsService, OrganizationsService>();
builder.Services.AddScoped<IApplicationsService, ApplicationsService>();
builder.Services.AddScoped<ISupportEngagementService, SupportEngagementService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddSingleton<IAuthorizationHandler, SameOrganizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CanManageUserHandler>();

// Real Keycloak provisioning when an admin secret is configured. The stub (which hands
// out a fixed, well-known temporary password) is allowed in Development only.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Keycloak:AdminClientSecret"]))
{
    builder.Services.AddHttpClient<IKeycloakAdminProvisioningService, KeycloakUserProvisioningService>(
        client => client.BaseAddress = new Uri(keycloakBaseUrl));
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IKeycloakAdminProvisioningService, StubKeycloakAdminProvisioningService>();
}
else
{
    throw new InvalidOperationException(
        "Keycloak:AdminClientSecret is required outside Development.");
}

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sentinel.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Dashboard");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<OrganizationIsolationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

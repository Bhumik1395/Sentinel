// backend/src/Sentinel.Api/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Sentinel.Identity;
using Sentinel.Api;
using Sentinel.Identity.Data;
using Sentinel.Identity.Organizations;
using Sentinel.Identity.SupportEngagements;
using Sentinel.Licensing;


var builder = WebApplication.CreateBuilder(args);
var keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
var keycloakAudience = builder.Configuration["Keycloak:Audience"]!;

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddSingleton<ISentinelDataSource, SentinelDataSource>();
builder.Services.AddScoped<IOrganizationsService, OrganizationsService>();
builder.Services.AddScoped<IKeycloakUserProvisioningService, StubKeycloakUserProvisioningService>();
builder.Services.AddScoped<ISupportEngagementService, SupportEngagementService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddSingleton<IAuthorizationHandler, SameOrganizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CanManageUserHandler>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<OrganizationIsolationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

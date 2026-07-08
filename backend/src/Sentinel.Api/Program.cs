// backend/src/Sentinel.Api/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Sentinel.Identity;
using Sentinel.Api;


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

builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddSingleton<IAuthorizationHandler, SameOrganizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CanManageUserHandler>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseMiddleware<OrganizationIsolationMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

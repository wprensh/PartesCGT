using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Auth;

public static class AuthSetup
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAdminAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.PostConfigure<JwtOptions>(jwt => EnsureSigningKey(jwt, environment));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Issuer,
                    IssuerSigningKey = JwtTokenIssuer.SigningKey(jwt),
                    NameClaimType = AuthClaims.Name,
                    ClockSkew = ClockSkew
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = AttachCurrentPermissionsAsync };
            });

        // Una política por permiso: [HasPermission(Permissions.X)] = "el usuario tiene el claim perm = X".
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
                options.AddPolicy(permission.Code, policy => policy.RequireClaim(AuthClaims.Permission, permission.Code));
        });

        services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<AdminAuthService>();
        return services;
    }

    /// <summary>
    /// En cada petición autenticada: rechaza el token si el usuario ya no existe, está inactivo o cambió su sello
    /// (contraseña, rol o estado), y agrega como claims los permisos vigentes de su rol.
    /// </summary>
    private static async Task AttachCurrentPermissionsAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        if (!int.TryParse(principal.FindFirst(AuthClaims.Subject)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            context.Fail("Token sin usuario.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.AdminUsers.AsNoTracking()
            .Include(u => u.Role).ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);

        if (user?.Role is null || !user.IsActive || user.SecurityStamp != principal.FindFirst(AuthClaims.SecurityStamp)?.Value)
        {
            context.Fail("La sesión ya no es válida.");
            return;
        }

        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var permission in user.Role.EffectivePermissions())
            identity.AddClaim(new Claim(AuthClaims.Permission, permission));
    }

    /// <summary>
    /// En producción la clave es obligatoria. En desarrollo, si falta, se genera una aleatoria por ejecución
    /// (las sesiones del panel se pierden al reiniciar).
    /// </summary>
    private static void EnsureSigningKey(JwtOptions jwt, IHostEnvironment environment)
    {
        if (Encoding.UTF8.GetByteCount(jwt.Key) >= JwtOptions.MinKeyBytes) return;
        if (!environment.IsDevelopment())
            throw new InvalidOperationException($"Jwt:Key debe tener al menos {JwtOptions.MinKeyBytes} caracteres.");
        jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }
}

using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Auth;

/// <summary>Emite los tokens del panel. Separado del login para poder cambiar el formato sin tocar la validación.</summary>
public interface ITokenIssuer
{
    LoginResponse Issue(AdminUser user);
}

/// <summary>
/// El token solo lleva la identidad (id, correo) y el sello de seguridad. Los permisos NO van en el token:
/// se leen de la base en cada petición, así un cambio de rol aplica de inmediato.
/// </summary>
public class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : ITokenIssuer
{
    public LoginResponse Issue(AdminUser user)
    {
        var jwt = options.Value;
        var expires = clock.GetUtcNow().UtcDateTime.AddHours(jwt.ExpiresHours);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(AuthClaims.Subject, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(AuthClaims.Name, user.Email),
                new Claim(AuthClaims.SecurityStamp, user.SecurityStamp)
            ]),
            Expires = expires,
            Issuer = jwt.Issuer,
            Audience = jwt.Issuer,
            SigningCredentials = new SigningCredentials(SigningKey(jwt), SecurityAlgorithms.HmacSha256)
        });
        return new LoginResponse(token, expires);
    }

    public static SymmetricSecurityKey SigningKey(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.Key));
}

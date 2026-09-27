using Microsoft.AspNetCore.Authorization;

namespace TiendaVirtual.Api.Common.Security;

/// <summary>Nombres de los claims del token del panel.</summary>
public static class AuthClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string SecurityStamp = "stamp";
    /// <summary>Se agrega en cada petición con los permisos vigentes del usuario (no viaja en el token).</summary>
    public const string Permission = "perm";
}

/// <summary>Exige un permiso del panel. Uso: [HasPermission(Permissions.ProductsManage)].</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(policy: permission);

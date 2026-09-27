using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Domain.Suppliers;

namespace TiendaVirtual.Api.Tests.Domain;

public class PermissionsTests
{
    [Fact]
    public void Gestionar_incluye_ver_y_se_ignoran_permisos_desconocidos() =>
        Assert.Equal(
            new[] { Permissions.ProductsManage, Permissions.ProductsView }.Order(),
            Permissions.Expand([Permissions.ProductsManage, "no.existe", Permissions.ProductsManage]).Order());
}

public class RoleTests
{
    private static Role ACustomRole() => new() { Id = 7, Name = "Ventas" };

    [Fact]
    public void El_rol_de_sistema_tiene_todos_los_permisos_aunque_no_esten_guardados()
    {
        var admin = new Role { Id = Role.AdministratorId, Name = "Administrador", IsSystem = true };
        Assert.Equal(Permissions.All.Count, admin.EffectivePermissions().Count);
        Assert.True(admin.Can(Permissions.UsersManage));
    }

    [Fact]
    public void SetPermissions_reemplaza_la_lista_y_calcula_los_implicitos()
    {
        var role = ACustomRole();
        role.SetPermissions([Permissions.CategoriesManage, Permissions.SuppliersView]);
        role.SetPermissions([Permissions.SuppliersManage]);

        Assert.Equal([Permissions.SuppliersManage], role.Permissions.Select(p => p.Permission));
        Assert.True(role.Can(Permissions.SuppliersView));
        Assert.False(role.Can(Permissions.CategoriesManage));
    }

    [Fact]
    public void SetPermissions_rechaza_permisos_inexistentes_y_no_toca_el_rol_de_sistema()
    {
        Assert.Contains("no existe", ACustomRole().SetPermissions(["borrar.todo"])!.Message);
        Assert.Equal(ErrorType.Validation, new Role { Name = "Administrador", IsSystem = true }.SetPermissions([])!.Type);
    }

    [Fact]
    public void Solo_se_borra_un_rol_normal_y_sin_usuarios()
    {
        Assert.Null(ACustomRole().ValidateDeletion(0));
        Assert.Equal(ErrorType.Conflict, ACustomRole().ValidateDeletion(2)!.Type);
        Assert.Equal(ErrorType.Validation, new Role { Name = "Administrador", IsSystem = true }.ValidateDeletion(0)!.Type);
    }

    [Theory]
    [InlineData("  Ventas  ", null)]
    [InlineData("AB", "entre 3")]
    public void Rename_recorta_y_valida_el_largo(string name, string? error)
    {
        var role = ACustomRole();
        var result = role.Rename(name, "  ");
        if (error is null) { Assert.Null(result); Assert.Equal("Ventas", role.Name); Assert.Null(role.Description); }
        else Assert.Contains(error, result!.Message);
    }
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("corta1", "al menos")]
    [InlineData("solamenteletras", "letras y números")]
    [InlineData("12345678901", "letras y números")]
    public void Rechaza_contrasenas_debiles(string password, string message) =>
        Assert.Contains(message, PasswordPolicy.Validate(password)!.Message);

    [Fact]
    public void Acepta_una_contrasena_de_10_caracteres_con_letras_y_numeros() =>
        Assert.Null(PasswordPolicy.Validate("tienda2026ok"));

    [Fact]
    public void Nadie_cambia_su_propio_acceso()
    {
        Assert.NotNull(AccessRules.ValidateSelfChange(5, 5, changesRoleOrDisables: true));
        Assert.Null(AccessRules.ValidateSelfChange(5, 5, changesRoleOrDisables: false));
        Assert.Null(AccessRules.ValidateSelfChange(5, 6, changesRoleOrDisables: true));
    }
}

public class SupplierRulesTests
{
    [Fact]
    public void El_NIT_se_normaliza_para_detectar_repetidos() =>
        Assert.Equal("900.123.456-7", Supplier.NormalizeTaxId(" 900.123. 456-7 "));

    [Fact]
    public void Update_limpia_textos_y_deja_null_los_vacios()
    {
        var supplier = Supplier.Create(new SupplierDetails(" Distribuidora ", "900 1", "  ", " VENTAS@Dist.co ", null, null, " Cartagena ", null, true));
        Assert.Equal(("Distribuidora", "9001", null, "ventas@dist.co", "Cartagena"),
            (supplier.Name, supplier.TaxId, supplier.ContactName, supplier.Email, supplier.City));
    }

    [Fact]
    public void Las_ofertas_no_repiten_proveedor_ni_tienen_costos_negativos_ni_dos_preferidos()
    {
        Assert.Null(SupplierOffers.Validate([new(1, 100, null, true), new(2, 90, "X-1", false)]));
        Assert.Contains("más de una vez", SupplierOffers.Validate([new(1, 100, null, false), new(1, 90, null, false)])!.Message);
        Assert.Contains("negativo", SupplierOffers.Validate([new(1, -1, null, false)])!.Message);
        Assert.Contains("preferido", SupplierOffers.Validate([new(1, 1, null, true), new(2, 1, null, true)])!.Message);
    }

    [Fact]
    public void Un_proveedor_con_productos_no_se_borra() =>
        Assert.Equal(ErrorType.Conflict, new Supplier { Name = "X", TaxId = "1" }.ValidateDeletion(3)!.Type);
}

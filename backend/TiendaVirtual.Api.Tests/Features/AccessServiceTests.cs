using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Features.Auth;
using TiendaVirtual.Api.Features.Roles;
using TiendaVirtual.Api.Features.Suppliers;
using TiendaVirtual.Api.Features.Users;
using TiendaVirtual.Api.Tests.Support;

namespace TiendaVirtual.Api.Tests.Features;

/// <summary>Base común: una base con los 3 roles semilla y un usuario administrador (Id 1).</summary>
public abstract class AccessTestBase : IDisposable
{
    protected const string AdminPassword = "clave-admin-2026";
    protected const int CatalogRoleId = 2;
    protected const int PurchasingRoleId = 3;

    protected readonly TestDatabase Database = new();
    protected readonly IPasswordHasher<AdminUser> Hasher = new PasswordHasher<AdminUser>();
    protected readonly int AdminId;

    protected AccessTestBase() => AdminId = AddUser("admin@tienda.local", Role.AdministratorId, AdminPassword);

    public void Dispose() => Database.Dispose();

    protected int AddUser(string email, int roleId, string password, bool active = true)
    {
        using var db = Database.CreateContext();
        var user = new AdminUser { Email = email, FullName = email, PasswordHash = "", RoleId = roleId, IsActive = active };
        user.PasswordHash = Hasher.HashPassword(user, password);
        db.AdminUsers.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    protected UserService Users() => new(Database.CreateContext(), Hasher, FixedClock.Default);
    protected RoleService Roles() => new(Database.CreateContext());
}

public sealed class AdminAuthServiceTests : AccessTestBase
{
    private sealed class FakeTokens : ITokenIssuer
    {
        public LoginResponse Issue(AdminUser user) => new($"token-{user.Id}-{user.SecurityStamp}", DateTime.UnixEpoch);
    }

    private AdminAuthService Service() => new(Database.CreateContext(), Hasher, new FakeTokens(), FixedClock.Default);

    [Fact]
    public async Task Credenciales_correctas_emiten_token_y_registran_el_ingreso()
    {
        var result = await Service().LoginAsync(new LoginRequest(" ADMIN@tienda.local ", AdminPassword), default);

        Assert.StartsWith($"token-{AdminId}-", result.Value.Token);
        await using var db = Database.CreateContext();
        Assert.Equal(FixedClock.Default.GetUtcNow().UtcDateTime, (await db.AdminUsers.SingleAsync(u => u.Id == AdminId)).LastLoginAt);
    }

    [Theory]
    [InlineData("admin@tienda.local", "otra-clave-1")]
    [InlineData("nadie@tienda.local", AdminPassword)]
    public async Task Credenciales_incorrectas_dan_el_mismo_error(string email, string password) =>
        Assert.Equal("Correo o contraseña incorrectos.", (await Service().LoginAsync(new LoginRequest(email, password), default)).Error!.Message);

    [Fact]
    public async Task Un_usuario_inactivo_no_puede_entrar()
    {
        AddUser("inactivo@tienda.local", CatalogRoleId, "clave-inactiva-1", active: false);
        Assert.Equal(ErrorType.Unauthorized, (await Service().LoginAsync(new LoginRequest("inactivo@tienda.local", "clave-inactiva-1"), default)).Error!.Type);
    }

    [Fact]
    public async Task Me_devuelve_los_permisos_efectivos_del_rol()
    {
        var id = AddUser("compras@tienda.local", PurchasingRoleId, "clave-compras-1");
        var me = (await Service().GetMeAsync(id, default)).Value;

        Assert.Equal("Compras", me.Role);
        Assert.Contains(Permissions.SuppliersView, me.Permissions);   // implícito por suppliers.manage
        Assert.DoesNotContain(Permissions.ProductsManage, me.Permissions);
    }

    [Fact]
    public async Task Cambiar_la_contrasena_exige_la_actual_y_rota_el_sello()
    {
        Assert.Contains("actual", (await Service().ChangePasswordAsync(AdminId, new("mala-clave-1", "nueva-clave-2026"), default)).Error!.Message);

        string stampBefore;
        await using (var db = Database.CreateContext()) stampBefore = (await db.AdminUsers.SingleAsync(u => u.Id == AdminId)).SecurityStamp;

        var result = await Service().ChangePasswordAsync(AdminId, new(AdminPassword, "nueva-clave-2026"), default);
        Assert.DoesNotContain(stampBefore, result.Value.Token);
        Assert.True((await Service().LoginAsync(new LoginRequest("admin@tienda.local", "nueva-clave-2026"), default)).IsSuccess);
    }

    [Fact]
    public async Task El_primer_administrador_solo_se_crea_si_no_hay_usuarios()
    {
        var result = await Service().CreateFirstAdminAsync(new("otro@tienda.local", "Otro", "clave-nueva-2026"), default);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        await using var db = Database.CreateContext();
        Assert.Equal(1, await db.AdminUsers.CountAsync());
    }

    [Fact]
    public async Task Con_la_tabla_vacia_crea_el_administrador_y_su_sesion()
    {
        await using (var db = Database.CreateContext()) await db.AdminUsers.ExecuteDeleteAsync();

        var result = await Service().CreateFirstAdminAsync(new(" Duena@Tienda.local ", " Dueña ", "clave-nueva-2026"), default);

        Assert.StartsWith("token-", result.Value.Token);
        await using var check = Database.CreateContext();
        var user = await check.AdminUsers.SingleAsync();
        Assert.Equal(("duena@tienda.local", "Dueña", Role.AdministratorId), (user.Email, user.FullName, user.RoleId));
        Assert.True((await Service().LoginAsync(new LoginRequest("duena@tienda.local", "clave-nueva-2026"), default)).IsSuccess);
    }

    [Fact]
    public async Task El_primer_administrador_respeta_la_politica_de_contrasenas()
    {
        await using (var db = Database.CreateContext()) await db.AdminUsers.ExecuteDeleteAsync();

        Assert.Equal(ErrorType.Validation, (await Service().CreateFirstAdminAsync(new("a@tienda.local", "Admin", "corta1"), default)).Error!.Type);
    }
}

public sealed class UserServiceTests : AccessTestBase
{
    [Fact]
    public async Task Crear_usuario_normaliza_el_correo_y_valida_la_contrasena()
    {
        Assert.Contains("al menos", (await Users().CreateAsync(new UserCreate("a@b.co", "Ana Pérez", CatalogRoleId, "corta1"), default)).Error!.Message);

        var created = await Users().CreateAsync(new UserCreate(" Ana@Tienda.Local ", " Ana Pérez ", CatalogRoleId, "clave-ana-2026"), default);
        Assert.Equal(("ana@tienda.local", "Ana Pérez", "Catálogo"), (created.Value.Email, created.Value.FullName, created.Value.Role));

        Assert.Equal(ErrorType.Conflict, (await Users().CreateAsync(new UserCreate("ANA@tienda.local", "Otra Ana", CatalogRoleId, "clave-ana-2026"), default)).Error!.Type);
    }

    [Fact]
    public async Task Nadie_puede_quitarse_su_propio_rol_ni_desactivarse()
    {
        var result = await Users().UpdateAsync(AdminId, AdminId, new UserUpdate("Admin", CatalogRoleId, IsActive: true), default);
        Assert.Contains("propio", result.Error!.Message);
        Assert.Equal(ErrorType.Validation, (await Users().DeleteAsync(AdminId, AdminId, default)).Error!.Type);
    }

    [Fact]
    public async Task No_se_puede_dejar_el_panel_sin_nadie_que_administre_usuarios()
    {
        var other = AddUser("otro@tienda.local", CatalogRoleId, "clave-otro-2026");
        // El "otro" (sin users.manage) intentaría degradar al único administrador.
        var result = await Users().UpdateAsync(other, AdminId, new UserUpdate("Admin", CatalogRoleId, IsActive: true), default);
        Assert.Equal(AccessRules.LastManager, result.Error);
    }

    [Fact]
    public async Task Cambiar_rol_o_estado_invalida_las_sesiones_del_usuario()
    {
        var id = AddUser("cat@tienda.local", CatalogRoleId, "clave-cat-2026");
        string before;
        await using (var db = Database.CreateContext()) before = (await db.AdminUsers.SingleAsync(u => u.Id == id)).SecurityStamp;

        await Users().UpdateAsync(AdminId, id, new UserUpdate("Cat", PurchasingRoleId, IsActive: true), default);

        await using var check = Database.CreateContext();
        Assert.NotEqual(before, (await check.AdminUsers.SingleAsync(u => u.Id == id)).SecurityStamp);
    }
}

public sealed class RoleServiceTests : AccessTestBase
{
    [Fact]
    public async Task Crea_roles_con_nombre_unico_y_permisos_validos()
    {
        var created = await Roles().CreateAsync(new RoleUpsert("Ventas", "Mira pedidos", [Permissions.ProductsView]), default);
        Assert.Equal([Permissions.ProductsView], created.Value.EffectivePermissions);
        Assert.Equal(ErrorType.Conflict, (await Roles().CreateAsync(new RoleUpsert("VENTAS", null, []), default)).Error!.Type);
    }

    [Fact]
    public async Task No_se_quita_users_manage_si_es_el_unico_rol_que_lo_da_a_alguien()
    {
        var managers = (await Roles().CreateAsync(new RoleUpsert("Jefes", null, [Permissions.UsersManage]), default)).Value;
        await using (var db = Database.CreateContext())
        {
            // El admin pasa a "Jefes": ahora es el único rol con usuarios que administran.
            (await db.AdminUsers.SingleAsync(u => u.Id == AdminId)).RoleId = managers.Id;
            await db.SaveChangesAsync();
        }

        var result = await Roles().UpdateAsync(managers.Id, new RoleUpsert("Jefes", null, [Permissions.ProductsView]), default);
        Assert.Equal(AccessRules.LastManager, result.Error);
    }

    [Fact]
    public async Task Solo_se_borran_roles_normales_sin_usuarios()
    {
        AddUser("cat@tienda.local", CatalogRoleId, "clave-cat-2026");

        Assert.Equal(ErrorType.Validation, (await Roles().DeleteAsync(Role.AdministratorId, default)).Error!.Type);
        Assert.Equal(ErrorType.Conflict, (await Roles().DeleteAsync(CatalogRoleId, default)).Error!.Type);
        Assert.True((await Roles().DeleteAsync(PurchasingRoleId, default)).IsSuccess);
    }
}

public sealed class SupplierServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    private SupplierService Service() => new(_database.CreateContext());

    private static SupplierUpsert ASupplier(string taxId = "900123456-7", bool active = true) =>
        new("Distribuidora Caribe", taxId, "Luis", "ventas@caribe.co", "300 000 0000", null, "Cartagena", null, active);

    [Fact]
    public async Task El_NIT_no_se_repite()
    {
        await Service().CreateAsync(ASupplier(), default);
        Assert.Equal(ErrorType.Conflict, (await Service().CreateAsync(ASupplier(" 900123456-7 "), default)).Error!.Type);
    }

    [Fact]
    public async Task Asignar_proveedores_a_un_producto_con_costo_y_preferido()
    {
        var a = (await Service().CreateAsync(ASupplier("111111"), default)).Value;
        var b = (await Service().CreateAsync(ASupplier("222222"), default)).Value;

        var result = await Service().ReplaceForProductAsync(SeedIds.NvmeSsd,
            [new(a.Id, 180_000, " KNV2 ", IsPreferred: false), new(b.Id, 175_000, null, IsPreferred: true)], default);

        Assert.Equal([b.Id, a.Id], result.Value.Select(s => s.SupplierId));   // el preferido primero
        Assert.Equal("KNV2", result.Value[1].SupplierSku);
        Assert.Equal(1, (await Service().FindAsync(a.Id, default)).Value.ProductCount);
    }

    [Fact]
    public async Task No_se_agrega_un_proveedor_inactivo_pero_se_conserva_si_ya_estaba()
    {
        var supplier = (await Service().CreateAsync(ASupplier("333333"), default)).Value;
        await Service().ReplaceForProductAsync(SeedIds.SataSsd, [new(supplier.Id, 100, null, false)], default);
        await Service().UpdateAsync(supplier.Id, ASupplier("333333", active: false), default);

        Assert.True((await Service().ReplaceForProductAsync(SeedIds.SataSsd, [new(supplier.Id, 120, null, false)], default)).IsSuccess);
        Assert.Contains("inactivo", (await Service().ReplaceForProductAsync(SeedIds.NvmeSsd, [new(supplier.Id, 1, null, false)], default)).Error!.Message);
    }

    [Fact]
    public async Task Un_proveedor_que_surte_productos_no_se_borra()
    {
        var supplier = (await Service().CreateAsync(ASupplier("444444"), default)).Value;
        await Service().ReplaceForProductAsync(SeedIds.Rtx4060, [new(supplier.Id, 1_200_000, null, true)], default);

        Assert.Equal(ErrorType.Conflict, (await Service().DeleteAsync(supplier.Id, default)).Error!.Type);
        await Service().ReplaceForProductAsync(SeedIds.Rtx4060, [], default);
        Assert.True((await Service().DeleteAsync(supplier.Id, default)).IsSuccess);
    }
}

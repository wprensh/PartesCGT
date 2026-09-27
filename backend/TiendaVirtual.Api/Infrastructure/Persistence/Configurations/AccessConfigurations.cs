using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> e)
    {
        e.Property(r => r.Name).HasMaxLength(Role.MaxNameLength);
        e.HasIndex(r => r.Name).IsUnique();
        e.Property(r => r.Description).HasMaxLength(200);
        e.HasMany(r => r.Permissions).WithOne().HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Cascade);
        // Un rol con usuarios no se puede borrar (también lo valida Role.ValidateDeletion).
        e.HasMany(r => r.Users).WithOne(u => u.Role).HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);

        e.HasData(
            new Role { Id = Role.AdministratorId, Name = "Administrador", Description = "Acceso total, incluidos usuarios y roles.", IsSystem = true },
            new Role { Id = 2, Name = "Catálogo", Description = "Mantiene categorías y productos." },
            new Role { Id = 3, Name = "Compras", Description = "Mantiene proveedores y sus costos; ve el catálogo." });
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> e)
    {
        e.HasKey(p => new { p.RoleId, p.Permission });
        e.Property(p => p.Permission).HasMaxLength(60);
        e.HasData(
            new RolePermission { RoleId = 2, Permission = Permissions.CategoriesManage },
            new RolePermission { RoleId = 2, Permission = Permissions.ProductsManage },
            new RolePermission { RoleId = 2, Permission = Permissions.SuppliersView },
            new RolePermission { RoleId = 3, Permission = Permissions.SuppliersManage },
            new RolePermission { RoleId = 3, Permission = Permissions.ProductsView },
            new RolePermission { RoleId = 3, Permission = Permissions.CategoriesView });
    }
}

internal sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> e)
    {
        e.Property(u => u.Email).HasMaxLength(AdminUser.MaxEmailLength);
        e.HasIndex(u => u.Email).IsUnique();
        e.Property(u => u.FullName).HasMaxLength(AdminUser.MaxNameLength);
        e.Property(u => u.PasswordHash).HasMaxLength(200);
        e.Property(u => u.SecurityStamp).HasMaxLength(64);
    }
}

using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;
using System.Reflection.Emit;

namespace OrderFlow.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Configuración de entidades ----

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
            e.Property(p => p.Sku).HasMaxLength(64).IsRequired();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasIndex(i => new { i.TenantId, i.ProductId }).IsUnique();
            e.HasOne(i => i.Product)
             .WithOne(p => p.Inventory)
             .HasForeignKey<InventoryItem>(i => i.ProductId);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
            e.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
            e.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.HasOne(o => o.CreatedBy)
             .WithMany()
             .HasForeignKey(o => o.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            e.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId);
            e.HasOne(oi => oi.Product)
             .WithMany()
             .HasForeignKey(oi => oi.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Global query filters: tenant + soft delete ----
        // Se aplica dinámicamente a toda entidad ITenantScoped,
        // así no se olvida ninguna al agregar entidades futuras.

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
            else if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Entidades sin tenant (ej. Tenant mismo): solo soft delete
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder)
        where T : BaseEntity, ITenantScoped
    {
        modelBuilder.Entity<T>().HasQueryFilter(e =>
            !e.IsDeleted &&
            _tenantProvider.TenantId != null &&
            e.TenantId == _tenantProvider.TenantId);
    }

    private void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder)
        where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.TenantId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Asignación automática de tenant en inserts
                    if (entry.Entity is ITenantScoped scoped &&
                        scoped.TenantId == Guid.Empty &&
                        tenantId.HasValue)
                    {
                        scoped.TenantId = tenantId.Value;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Soft delete: convertir deletes físicos en lógicos
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

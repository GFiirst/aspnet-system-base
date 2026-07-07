using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options
    ): base(options) {}

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<Permissions> Permissions { get; set; }

    public DbSet<RolesPermissions> RolesPermissions { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    //gera e atualiza CreatedAt e UpdatedAt automaticamente
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var entries = ChangeTracker
            .Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(
            cancellationToken
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {   
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly
        );


        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(
                    entityType.ClrType,
                    "e"
                );

                var property = Expression.Property(
                    parameter,
                    nameof(BaseEntity.DeletedAt)
                );

                var condition = Expression.Equal(
                    property,
                    Expression.Constant(null)
                );

                var lambda = Expression.Lambda(
                    condition,
                    parameter
                );

                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(lambda);
            }
        }
    }
    
}
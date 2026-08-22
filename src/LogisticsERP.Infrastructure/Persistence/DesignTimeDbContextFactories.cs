using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogisticsERP.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DesignTimeDatabase.ConnectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName);
                sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration");
            })
            .Options;

        return new ApplicationDbContext(options);
    }
}

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(DesignTimeDatabase.ConnectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(IdentityDbContextFactory).Assembly.FullName);
                sql.MigrationsHistoryTable("__IdentityMigrationsHistory", "migration");
            })
            .Options;

        return new IdentityDbContext(options);
    }
}

internal static class DesignTimeDatabase
{
    public const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=LogisticsERP_Design;Trusted_Connection=True;TrustServerCertificate=True";
}

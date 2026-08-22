using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

await using var context = new ApplicationDbContext(
    new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FleetCompile;Trusted_Connection=True;TrustServerCertificate=True")
        .Options);

_ = context.Model.GetRelationalModel();
Console.WriteLine("Fleet EF model validated.");

await using var identityContext = new IdentityDbContext(
    new DbContextOptionsBuilder<IdentityDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=IdentityCompile;Trusted_Connection=True;TrustServerCertificate=True")
        .Options);
_ = identityContext.Model.GetRelationalModel();
Console.WriteLine("Identity EF model validated.");

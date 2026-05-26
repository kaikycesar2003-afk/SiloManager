using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SiloManager.Infrastructure.Data
{
    // Usado apenas pelo EF Core em tempo de design (migrations)
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=silomanager_design.db")
                .Options;

            return new AppDbContext(opts);
        }
    }
}
using Microsoft.EntityFrameworkCore;

namespace Techtoon.Data
{
    public class TechtoonDbContext : DbContext
    {
        public TechtoonDbContext() { }
        public TechtoonDbContext(DbContextOptions<TechtoonDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var cs = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                if (string.IsNullOrWhiteSpace(cs))
                {
                    throw new InvalidOperationException(
                        "TECHTOON_PG_CONNECTION environment variable is not set.");
                }
                optionsBuilder.UseNpgsql(cs);
            }
        }
    }
}
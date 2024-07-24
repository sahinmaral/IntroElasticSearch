using IntroElasticSearch.Models;

using Microsoft.EntityFrameworkCore;

namespace IntroElasticSearch.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Travel> Travels { get; set; }
    }
}

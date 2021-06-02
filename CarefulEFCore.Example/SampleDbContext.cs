using Microsoft.EntityFrameworkCore;

namespace RealGoodApps.CarefulEFCore.Example
{
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DB set for instances of <see cref="Category"/>.
        /// </summary>
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Strategy9Website.Models;

namespace Strategy9Website.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ContactRequest> ContactRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ContactRequest
            modelBuilder.Entity<ContactRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.SubmittedAt);
            });
        }
    }
}
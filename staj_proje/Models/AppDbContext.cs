using Microsoft.EntityFrameworkCore;
using staj_proje.Models;

namespace staj_proje.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User tablosu konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(e => e.Password)
                      .IsRequired()
                      .HasMaxLength(500);
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");

                // Username benzersiz olmalı
                entity.HasIndex(e => e.Username)
                      .IsUnique();
            });
        }
    }
}
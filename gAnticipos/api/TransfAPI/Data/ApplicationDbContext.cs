using Microsoft.EntityFrameworkCore;
using TransfAPI.Models;

namespace TransfAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<empresaMedios> empresa_medios { get; set; } = null!;
        public DbSet<gCobroAnticipo> g_cobro_antiipo { get; set; } = null!;
        public DbSet<clientes> clientes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<gCobroAnticipo>(entity =>
            {
                entity.ToTable("g_cobro_antiipo");
                entity.HasKey(e => e.id);
                
                entity.Property(e => e.fecha_registro)
                      .HasColumnName("fecha_registro")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP") // Usar el valor por defecto de la BD
                      .ValueGeneratedOnAdd(); // Indica que se genera al insertar

                entity.HasIndex(e => new { e.id_medio_cobro, e.numero_referencia })
                      .IsUnique();
            });
        }
    }
    
}
using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.Data {
    public class HmlContext: DbContext {
        public HmlContext(DbContextOptions<HmlContext> options): base(options) { }

        public DbSet<NotasEntrada> NotasEntradas { get; set; } 
        public DbSet<ItemNota> ItemNota { get; set; }
        public DbSet<Produto> Produto { get; set; }
        public DbSet<Familia> Familia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {

            modelBuilder.Entity<NotasEntrada>(e =>
            {
                e.ToTable("E440NFC", t => t.ExcludeFromMigrations());
                e.HasKey(n => new { n.CodEmp, n.CodFil, n.CodFor, n.NumNfc, n.CodSnf });
                e.Property(n => n.SitNfc).IsUnicode(false);
            });

            modelBuilder.Entity<ItemNota>(e => {
                e.ToTable("E440IPC", t => t.ExcludeFromMigrations());
                e.HasKey(i => new { i.CodEmp, i.CodFil, i.CodFor, i.NumNfc, i.CodSnf, i.SeqIpc });

                e.HasOne(i => i.Nota)
                 .WithMany(n => n.ItemNotas)
                 .HasForeignKey(i => new { i.CodEmp, i.CodFil, i.CodFor, i.NumNfc, i.CodSnf });

            });

            modelBuilder.Entity<Familia>().ToTable("E012FAM", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Familia)
                .WithMany()
                .HasForeignKey(p => p.CodFam);
            modelBuilder.Entity<Fornecedor>().ToTable("E095FOR", t => t.ExcludeFromMigrations());
        }
    }
}

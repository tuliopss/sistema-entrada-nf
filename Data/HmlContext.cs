using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.Data {
    public class HmlContext: DbContext {
        public HmlContext(DbContextOptions<HmlContext> options): base(options) { }

        public DbSet<NotasEntrada> NotasEntradas { get; set; } 
        public DbSet<ItemNotaEntrada> ItemNota { get; set; }
        public DbSet<NotaVenda> NotasVenda => Set<NotaVenda>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<ItemNotaVenda> ItensNotasVenda => Set<ItemNotaVenda>();
        public DbSet<Produto> Produto { get; set; }
        public DbSet<Familia> Familia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {

            modelBuilder.Entity<NotasEntrada>(e =>
            {
                e.ToTable("E440NFC", t => t.ExcludeFromMigrations());
                e.HasKey(n => new { n.CodEmp, n.CodFil, n.CodFor, n.NumNfc, n.CodSnf });
                e.Property(n => n.SitNfc).IsUnicode(false);
                e.HasOne(n => n.Transportadora)
                 .WithMany()
                 .HasForeignKey(n => n.CodTra)
                 .IsRequired(false);
            });

            modelBuilder.Entity<ItemNotaEntrada>(e => {
                e.ToTable("E440IPC", t => t.ExcludeFromMigrations());
                e.HasKey(i => new { i.CodEmp, i.CodFil, i.CodFor, i.NumNfc, i.CodSnf, i.SeqIpc });

                e.HasOne(i => i.Nota)
                 .WithMany(n => n.ItemNotas)
                 .HasForeignKey(i => new { i.CodEmp, i.CodFil, i.CodFor, i.NumNfc, i.CodSnf });

                e.Property(i => i.SnfNfv).IsUnicode(false);
                e.Property(i => i.UniNfc).IsUnicode(false);
                e.HasOne(i => i.ItemVendaOrigem)
                 .WithMany()
                 .HasForeignKey(i => new { i.EmpNfv, i.FilNfv, i.SnfNfv, i.NumNfv, i.SeqIpv });
            });

            modelBuilder.Entity<NotaVenda>(e => {
                e.ToTable("E140NFV", t => t.ExcludeFromMigrations());
                e.HasKey(n => new { n.CodEmp, n.CodFil, n.CodSnf, n.NumNfv });
                e.Property(n => n.CodSnf).HasMaxLength(3).IsUnicode(false);
                e.Property(n => n.DatEmi).HasColumnType("datetime");
                e.Property(n => n.SitNfv).HasMaxLength(1).IsUnicode(false);
                e.Property(n => n.VlrLiq).HasPrecision(15, 2);
            });

            modelBuilder.Entity<ItemNotaVenda>(e => {
                e.ToTable("E140IPV", t => t.ExcludeFromMigrations());
                e.HasKey(i => new { i.CodEmp, i.CodFil, i.CodSnf, i.NumNfv, i.SeqIpv });
                e.Property(i => i.CodSnf).HasMaxLength(3).IsUnicode(false);
                e.Property(i => i.CodPro).HasMaxLength(14).IsUnicode(false);
                e.Property(i => i.CodDer).HasMaxLength(7).IsUnicode(false);
                e.Property(i => i.QtdFat).HasPrecision(14, 5);
                e.Property(i => i.QtdDev).HasPrecision(14, 5);
                e.Property(i => i.UniMed).HasMaxLength(3).IsUnicode(false);
                e.Property(i => i.PreUni).HasPrecision(21, 10);
                e.Property(i => i.VlrBru).HasPrecision(15, 2);
                e.Property(i => i.VlrLiq).HasPrecision(15, 2);
                e.HasOne(i => i.Nota)
                 .WithMany(n => n.Itens)
                 .HasForeignKey(i => new { i.CodEmp, i.CodFil, i.CodSnf, i.NumNfv });
            });

            modelBuilder.Entity<Familia>().ToTable("E012FAM", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Familia)
                .WithMany()
                .HasForeignKey(p => p.CodFam);
            modelBuilder.Entity<Fornecedor>(e => {
                e.ToTable("E095FOR", t => t.ExcludeFromMigrations());
                e.HasOne(f => f.Cliente)
                 .WithMany()
                 .HasForeignKey(f => f.CodCli);
            });

            modelBuilder.Entity<Cliente>(e => {
                e.ToTable("E085CLI", t => t.ExcludeFromMigrations());
                e.HasOne(c => c.Fornecedor)
                 .WithMany()
                 .HasForeignKey(c => c.CodFor);
            });

            modelBuilder.Entity<Transportadora>().ToTable("E073TRA", t => t.ExcludeFromMigrations());
        }
    }
}

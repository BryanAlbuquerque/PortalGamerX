using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalGamerX.Models;
using PortalGamerX.Models.Entities;

namespace PortalGamerX.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Banner> Banners { get; set; }

        public DbSet<Carrinho> Carrinhos { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Favorito> Favoritos { get; set; }

        public DbSet<HistoricoCompra> HistoricosCompras { get; set; }

        public DbSet<Imagem> Imagens { get; set; }

        public DbSet<ItemCarrinho> ItensCarrinho { get; set; }

        public DbSet<ItemPedido> ItensPedidos { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<Jogo> Jogos { get; set; }

        public DbSet<Pagamento> Pagamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigurarRelacionamentos(builder);
        }

        private static void ConfigurarRelacionamentos(ModelBuilder modelBuilder)
        {
            #region USUARIO

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Carrinhos)
                .WithOne(c => c.Usuario)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Favoritos)
                .WithOne(f => f.Usuario)
                .HasForeignKey(f => f.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Pedidos)
                .WithOne(p => p.Usuario)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.HistoricosCompras)
                .WithOne(h => h.Usuario)
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion


            #region CARRINHO

            modelBuilder.Entity<Carrinho>()
                .HasMany(c => c.Itens)
                .WithOne(i => i.Carrinho)
                .HasForeignKey(i => i.CarrinhoId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion


            #region CATEGORIA

            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.Jogos)
                .WithOne(j => j.Categoria)
                .HasForeignKey(j => j.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion


            #region JOGO

            modelBuilder.Entity<Jogo>()
                .HasMany(j => j.Imagens)
                .WithOne(i => i.Jogo)
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Jogo>()
                .HasMany(j => j.Favoritos)
                .WithOne(f => f.Jogo)
                .HasForeignKey(f => f.JogoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Jogo>()
                .HasMany(j => j.ItensCarrinho)
                .WithOne(i => i.Jogo)
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Jogo>()
                .HasMany(j => j.ItensPedido)
                .WithOne(i => i.Jogo)
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion


            #region PEDIDO

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Itens)
                .WithOne(i => i.Pedido)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Pagamentos)
                .WithOne(p => p.Pedido)
                .HasForeignKey(p => p.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.HistoricosCompras)
                .WithOne(h => h.Pedido)
                .HasForeignKey(h => h.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion
        }
    }
}
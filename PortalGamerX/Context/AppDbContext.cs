using Microsoft.EntityFrameworkCore;
using PortalGamerX.Models.Entities;

namespace PortalGamerX.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedidos { get; set; }
        public DbSet<Carrinho> Carrinhos { get; set; }
        public DbSet<ItemCarrinho> ItensCarrinhos { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<HistoricoCompra> HistoricoCompras { get; set; }
        public DbSet<Imagem> Imagens { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Banner> Banners { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigurarEntidades(builder);
            ConfigurarRelacionamentos(builder);
            ConfigurarDecimais(builder);
        }

        private static void ConfigurarEntidades(ModelBuilder builder)
        {
            // CLIENTE

            builder.Entity<Cliente>()
                .ToTable("Clientes");

            builder.Entity<Cliente>()
                .HasKey(c => c.Id);

            builder.Entity<Cliente>()
                .Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Cliente>()
                .Property(c => c.Sobrenome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Cliente>()
                .Property(c => c.CPF)
                .HasMaxLength(14);

            builder.Entity<Cliente>()
                .Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Cliente>()
                .Property(c => c.Telefone)
                .HasMaxLength(20);

            // JOGO

            builder.Entity<Jogo>()
                .ToTable("Jogos");

            builder.Entity<Jogo>()
                .HasKey(j => j.JogoId);

            builder.Entity<Jogo>()
                .Property(j => j.Nome)
                .IsRequired()
                .HasMaxLength(80);

            builder.Entity<Jogo>()
                .Property(j => j.Descricao)
                .IsRequired()
                .HasMaxLength(300);

            builder.Entity<Jogo>()
                .Property(j => j.ImagemUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            // CATEGORIA

            builder.Entity<Categoria>()
                .ToTable("Categorias");

            builder.Entity<Categoria>()
                .HasKey(c => c.CategoriaId);

            builder.Entity<Categoria>()
                .Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Categoria>()
                .Property(c => c.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<Categoria>()
                .Property(c => c.TipoCategoria)
                .IsRequired()
                .HasMaxLength(50);

            // PEDIDO

            builder.Entity<Pedido>()
                .ToTable("Pedidos");

            builder.Entity<Pedido>()
                .HasKey(p => p.Id);

            builder.Entity<Pedido>()
                .Property(p => p.NumeroPedido)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Pedido>()
                .Property(p => p.FormaPagamento)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Pedido>()
                .Property(p => p.CPFCliente)
                .HasMaxLength(14);

            builder.Entity<Pedido>()
                .Property(p => p.NomeCliente)
                .HasMaxLength(100);

            builder.Entity<Pedido>()
                .Property(p => p.EmailCliente)
                .HasMaxLength(100);

            builder.Entity<Pedido>()
                .Property(p => p.MotivoReembolso)
                .HasMaxLength(500);

            // PAGAMENTO

            builder.Entity<Pagamento>()
                .ToTable("Pagamentos");

            builder.Entity<Pagamento>()
                .HasKey(p => p.Id);

            builder.Entity<Pagamento>()
                .Property(p => p.MetodoPagamento)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Pagamento>()
                .Property(p => p.StatusPagamento)
                .IsRequired()
                .HasMaxLength(30);

            builder.Entity<Pagamento>()
                .Property(p => p.UsuarioId)
                .HasMaxLength(450);

            builder.Entity<Pagamento>()
                .Property(p => p.ChavePix)
                .HasMaxLength(200);

            builder.Entity<Pagamento>()
                .Property(p => p.QrCodePix)
                .HasMaxLength(1000);

            builder.Entity<Pagamento>()
                .Property(p => p.CopiaEColaPix)
                .HasMaxLength(1000);

            builder.Entity<Pagamento>()
                .Property(p => p.Ultimos4DigitosCartao)
                .HasMaxLength(4);

            builder.Entity<Pagamento>()
                .Property(p => p.BandeiraCartao)
                .HasMaxLength(30);

            // ITEM PEDIDO

            builder.Entity<ItemPedido>()
                .ToTable("ItensPedidos");

            builder.Entity<ItemPedido>()
                .HasKey(i => i.Id);

            // CARRINHO

            builder.Entity<Carrinho>()
                .ToTable("Carrinhos");

            builder.Entity<Carrinho>()
                .HasKey(c => c.Id);

            builder.Entity<Carrinho>()
                .Property(c => c.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            // ITEM CARRINHO

            builder.Entity<ItemCarrinho>()
                .ToTable("ItensCarrinhos");

            builder.Entity<ItemCarrinho>()
                .HasKey(i => i.Id);

            // FAVORITO

            builder.Entity<Favorito>()
                .ToTable("Favoritos");

            builder.Entity<Favorito>()
                .HasKey(f => new { f.UsuarioId, f.JogoId });

            builder.Entity<Favorito>()
                .Property(f => f.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            // IMAGEM

            builder.Entity<Imagem>()
                .ToTable("Imagens");

            builder.Entity<Imagem>()
                .HasKey(i => i.Id);

            builder.Entity<Imagem>()
                .Property(i => i.UrlImagem)
                .IsRequired()
                .HasMaxLength(500);

            // HISTÓRICO DE COMPRA

            builder.Entity<HistoricoCompra>()
                .ToTable("HistoricoCompras");

            builder.Entity<HistoricoCompra>()
                .HasKey(h => h.Id);

            builder.Entity<HistoricoCompra>()
                .Property(h => h.Acao)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<HistoricoCompra>()
                .Property(h => h.Descricao)
                .HasMaxLength(500);

            builder.Entity<HistoricoCompra>()
                .Property(h => h.UsuarioId)
                .HasMaxLength(450);

            // BANNER

            builder.Entity<Banner>()
                .ToTable("Banners");

            builder.Entity<Banner>()
                .HasKey(b => b.Id);

            builder.Entity<Banner>()
                .Property(b => b.Titulo)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Banner>()
                .Property(b => b.Imagem)
                .IsRequired()
                .HasMaxLength(500);

            builder.Entity<Banner>()
                .Property(b => b.LinkDestino)
                .HasMaxLength(500);
        }

        private static void ConfigurarRelacionamentos(ModelBuilder builder)
        {
            // CATEGORIA -> JOGOS

            builder.Entity<Categoria>()
                .HasMany(c => c.Jogos)
                .WithOne(j => j.Categoria)
                .HasForeignKey(j => j.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // CLIENTE -> PEDIDOS

            builder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(c => c.Pedidos)
                .OnDelete(DeleteBehavior.Restrict);

            // PEDIDO -> ITENS

            builder.Entity<ItemPedido>()
                .HasOne(i => i.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PEDIDO -> PAGAMENTOS

            builder.Entity<Pagamento>()
                .HasOne(p => p.Pedido)
                .WithMany(p => p.Pagamentos)
                .HasForeignKey(p => p.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PAGAMENTO -> CLIENTE

            builder.Entity<Pagamento>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ITEM PEDIDO -> JOGO

            builder.Entity<ItemPedido>()
                .HasOne(i => i.Jogo)
                .WithMany(j => j.ItensPedido)
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Restrict);

            // CLIENTE -> CARRINHOS

            builder.Entity<Carrinho>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Carrinho)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // CARRINHO -> ITENS

            builder.Entity<ItemCarrinho>()
                .HasOne(i => i.Carrinho)
                .WithMany(c => c.Itens)
                .HasForeignKey(i => i.CarrinhoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ITEM CARRINHO -> JOGO

            builder.Entity<ItemCarrinho>()
                .HasOne(i => i.Jogo)
                .WithMany()
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Restrict);

            // CLIENTE -> FAVORITOS

            builder.Entity<Favorito>()
                .HasOne(f => f.Usuario)
                .WithMany(u => u.Favoritos)
                .HasForeignKey(f => f.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // JOGO -> FAVORITOS

            builder.Entity<Favorito>()
                .HasOne(f => f.Jogo)
                .WithMany(j => j.Favoritos)
                .HasForeignKey(f => f.JogoId)
                .OnDelete(DeleteBehavior.Cascade);

            // JOGO -> IMAGENS

            builder.Entity<Imagem>()
                .HasOne(i => i.Jogo)
                .WithMany(j => j.Imagens)
                .HasForeignKey(i => i.JogoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PEDIDO -> HISTÓRICO

            builder.Entity<HistoricoCompra>()
                .HasOne(h => h.Pedido)
                .WithMany(p => p.HistoricosCompras)
                .HasForeignKey(h => h.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // HISTÓRICO -> CLIENTE

            builder.Entity<HistoricoCompra>()
                .HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UsuarioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarDecimais(ModelBuilder builder)
        {
            // JOGO

            builder.Entity<Jogo>()
                .Property(j => j.Preco)
                .HasPrecision(18, 2);

            // PEDIDO

            builder.Entity<Pedido>()
                .Property(p => p.ValorProdutos)
                .HasPrecision(18, 2);

            builder.Entity<Pedido>()
                .Property(p => p.ValorDesconto)
                .HasPrecision(18, 2);

            builder.Entity<Pedido>()
                .Property(p => p.ValorTotal)
                .HasPrecision(18, 2);

            // PAGAMENTO

            builder.Entity<Pagamento>()
                .Property(p => p.Valor)
                .HasPrecision(18, 2);

            // ITEM PEDIDO

            builder.Entity<ItemPedido>()
                .Property(i => i.Valor)
                .HasPrecision(18, 2);

            // ITEM CARRINHO

            builder.Entity<ItemCarrinho>()
                .Property(i => i.PrecoUnitario)
                .HasPrecision(18, 2);

            builder.Entity<ItemCarrinho>()
                .Property(i => i.ValorTotal)
                .HasPrecision(18, 2);
        }
    }
}
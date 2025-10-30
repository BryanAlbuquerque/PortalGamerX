using Microsoft.EntityFrameworkCore;
using PortalGamerX.Models;

namespace PortalGamerX.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Produto> Produtos { get; set; }
    }
}

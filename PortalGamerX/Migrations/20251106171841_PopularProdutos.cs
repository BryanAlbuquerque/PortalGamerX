using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGamerX.Migrations
{
    /// <inheritdoc />
    public partial class PopularProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO Produtos
                (Nome, Descricao, Preco, Quantidade, ImagemUrl, CategoriaId) 
                VALUES
                ('Camiseta Gamer', 'Camiseta temática com estampas de jogos populares.', 59.90, 50, 'https://example.com/camiseta_gamer.jpg', 22),
                ('Boné Nerd', 'Boné estilizado com logos de franquias famosas.', 39.90, 80, 'https://example.com/bone_nerd.jpg', 23),
                ('Action Figure Herói', 'Figura colecionável detalhada de um herói icônico.', 129.90, 30, 'https://example.com/action_figure_heroi.jpg', 24),
                ('Caneca Personalizada', 'Caneca com arte exclusiva de jogos e séries.', 29.90, 100, 'https://example.com/caneca_personalizada.jpg', 25),
                ('Poster Decorativo', 'Pôster de alta qualidade para decorar seu espaço gamer.', 19.90, 60, 'https://example.com/poster_decorativo.jpg', 26),
                ('Mousepad Gamer', 'Mousepad grande com design temático para gamers.', 49.90, 70, 'https://example.com/mousepad_gamer.jpg', 27),
                ('Funko Pop Exclusivo', 'Miniatura Funko Pop de edição limitada.', 89.90, 40, 'https://example.com/funko_pop_exclusivo.jpg', 28);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ProdutosGeek WHERE Nome IN (
                    'Camiseta Gamer',
                    'Boné Nerd',
                    'Action Figure Herói',
                    'Caneca Personalizada',
                    'Poster Decorativo',
                    'Mousepad Gamer',
                    'Funko Pop Exclusivo'
                );
            ");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGamerX.Migrations
{
    /// <inheritdoc />
    public partial class PopularJogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO Jogos 
                (Nome, Descricao, Preco, Categoria, Plataforma, ImagemUrl) 
                VALUES
                ('The Legend of Zelda: Breath of the Wild', 'Explore the vast kingdom of Hyrule in this open-world adventure game.', 59.99, 'Aventura', 'Nintendo Switch', 'https://example.com/zelda.jpg'),
                ('God of War', 'Join Kratos on a journey through Norse mythology in this action-packed game.', 49.99, 'Ação', 'PlayStation 4', 'https://example.com/godofwar.jpg'),
                ('Minecraft', 'Build and explore your own worlds in this sandbox game.', 26.95, 'Sandbox', 'Multiplataforma', 'https://example.com/minecraft.jpg'),
                ('Cyberpunk 2077', 'Experience a dystopian future in this open-world RPG.', 39.99, 'RPG', 'PC', 'https://example.com/cyberpunk2077.jpg'),
                ('Among Us', 'Work together to complete tasks while identifying impostors on your spaceship.', 4.99, 'Multijogador', 'Multiplataforma', 'https://example.com/amongus.jpg');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM JOGOS");
        }
    }
}

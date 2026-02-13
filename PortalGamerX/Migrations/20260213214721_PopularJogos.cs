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
            migrationBuilder.Sql(@"
                INSERT INTO Jogos (Nome, DataLancamento, Descricao, Preco, ImagemUrl, CategoriaId)
                VALUES
                ('The Legend of Zelda: Breath of the Wild','2017-03-03','Explore o vasto reino de Hyrule em uma aventura de mundo aberto com liberdade total.',299.90,'https://www.adrenaline.com.br/wp-content/uploads/2025/04/Speedrunner-Zelda-Breath-Wild-Switch-2.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Aventura')),

                ('God of War Ragnarök','2022-11-09','Kratos e Atreus enfrentam deuses nórdicos em uma jornada épica.',349.90,'https://example.com/gowragnarok.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Ação')),

                ('Elden Ring','2022-02-25','Mundo aberto sombrio criado por Miyazaki e George R. R. Martin.',279.90,'https://example.com/eldenring.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'RPG')),

                ('Cyberpunk 2077','2020-12-10','RPG futurista em Night City.',199.90,'https://example.com/cyberpunk.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'RPG')),

                ('Minecraft','2011-11-18','Crie, explore e sobreviva em mundos infinitos.',99.90,'https://example.com/minecraft.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Simulação')),

                ('Resident Evil 4 Remake','2023-03-24','Reimaginação moderna do clássico.',299.90,'https://example.com/re4remake.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Terror')),

                ('Hollow Knight','2017-02-24','Metroidvania desafiador e belo.',59.90,'https://example.com/hollowknight.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Aventura')),

                ('Spider-Man: Miles Morales','2020-11-12','Proteja Nova York com novos poderes.',249.90,'https://example.com/spidermanmm.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Ação')),

                ('Stardew Valley','2016-02-26','Construa sua fazenda e viva no campo.',39.90,'https://example.com/stardewvalley.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Simulação')),

                ('Hades','2020-09-17','Roguelike premiado da Supergiant.',89.90,'https://example.com/hades.jpg',
                 (SELECT CategoriaId FROM Categorias WHERE Nome = 'Ação'));
                ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE * FROM Jogos");

        }
    }
}

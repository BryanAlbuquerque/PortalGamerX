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
                    INSERT INTO Jogos 
                    (Nome, DataLancamento, Descricao, Preco, ImagemUrl, CategoriaId) 
                    VALUES
                    ('The Legend of Zelda: Breath of the Wild', '2017-03-03', 'Explore o vasto reino de Hyrule em uma aventura de mundo aberto com liberdade total.', 299.90, 'https://example.com/zelda.jpg', 16),
                    ('God of War Ragnarök', '2022-11-09', 'Kratos e Atreus enfrentam deuses nórdicos em uma jornada épica de redenção e destino.', 349.90, 'https://example.com/gowragnarok.jpg', 12),
                    ('Elden Ring', '2022-02-25', 'Um mundo aberto sombrio e expansivo criado por Hidetaka Miyazaki e George R. R. Martin.', 279.90, 'https://example.com/eldenring.jpg', 13),
                    ('Cyberpunk 2077', '2020-12-10', 'Um RPG futurista em Night City, onde cada escolha molda o destino de V.', 199.90, 'https://example.com/cyberpunk.jpg', 13),
                    ('Minecraft', '2011-11-18', 'Crie, explore e sobreviva em mundos infinitos de blocos.', 99.90, 'https://example.com/minecraft.jpg', 17),
                    ('Resident Evil 4 Remake', '2023-03-24', 'Leon S. Kennedy retorna em uma reimaginação moderna do clássico de terror e ação.', 299.90, 'https://example.com/re4remake.jpg', 18),
                    ('Hollow Knight', '2017-02-24', 'Explore um vasto mundo subterrâneo em um metroidvania desafiador e belo.', 59.90, 'https://example.com/hollowknight.jpg', 14),
                    ('Spider-Man: Miles Morales', '2020-11-12', 'Assuma o papel de Miles Morales e proteja Nova York com seus novos poderes.', 249.90, 'https://example.com/spidermanmm.jpg', 16),
                    ('Stardew Valley', '2016-02-26', 'Construa sua fazenda, socialize com aldeões e viva uma vida pacífica no campo.', 39.90, 'https://example.com/stardewvalley.jpg', 17),
                    ('Hades', '2020-09-17', 'Lute para escapar do submundo neste premiado roguelike da Supergiant Games.', 89.90, 'https://example.com/hades.jpg', 13);
");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE * FROM Jogos");
        }
    }
}

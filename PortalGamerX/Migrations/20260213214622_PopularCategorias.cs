using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGamerX.Migrations
{
    /// <inheritdoc />
    public partial class PopularCategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO Categorias (Nome, Descricao, TipoCategoria)
                VALUES
                ('Ação', 'Jogos com foco em combate e reflexos rápidos', 'Jogo'),
                ('RPG', 'Jogos com progressão de personagem e história', 'Jogo'),
                ('Aventura', 'Jogos que envolvem exploração, narrativa e descobertas.', 'Jogo'),
                ('Estratégia', 'Jogos que exigem planejamento e raciocínio tático.', 'Jogo'),
                ('Mundo Aberto', 'Jogos com liberdade de exploração e missões não lineares.', 'Jogo'),
                ('Simulação', 'Jogos que reproduzem atividades da vida real ou sistemas complexos.', 'Jogo'),
                ('Terror', 'Experiências focadas em suspense, medo e sobrevivência.', 'Jogo'),

                ('Roupas', 'Roupas temáticas inspiradas em games, séries e filmes.', 'ProdutoGeek'),
                ('Bonés', 'Acessórios de cabeça com logos e personagens famosos.', 'ProdutoGeek'),
                ('Action Figures', 'Bonecos e estátuas detalhadas de personagens icônicos.', 'ProdutoGeek'),
                ('Canecas', 'Itens personalizados para fãs de franquias famosas.', 'ProdutoGeek'),
                ('Posters', 'Pôsteres e quadros decorativos de jogos, séries e animes.', 'ProdutoGeek'),
                ('Mousepads', 'Mousepads personalizados com arte gamer e personagens.', 'ProdutoGeek'),
                ('Funko Pop', 'Miniaturas estilizadas de personagens famosos.', 'ProdutoGeek');
                ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql(@"
                    DELETE FROM Categorias
                    WHERE Nome IN ('Ação', 'RPG', 'Aventura', 'Estratégia', 'Mundo Aberto', 'Simulação', 'Terror',
                                'Roupas', 'Bonés', 'Action Figures', 'Canecas', 'Posters', 'Mousepads', 'Funko Pop');
                    ");
        }
    }
}

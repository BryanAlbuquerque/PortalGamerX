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
            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Ação', 'Jogos com foco em combate e reflexos rápidos', 'Jogo')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('RPG', 'Jogos com progressão de personagem e história', 'Jogo')");

            migrationBuilder.Sql
               ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
               "VALUES " +
               "('Aventura', 'Jogos que envolvem exploração, narrativa e descobertas.', 'Jogo')");

            migrationBuilder.Sql
               ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
               "VALUES " +
               "('Estratégia', 'Jogos que exigem planejamento e raciocínio tático.', 'Jogo')");


            migrationBuilder.Sql
               ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
               "VALUES " +
               "('Mundo Aberto', 'Jogos com liberdade de exploração e missões não lineares.', 'Jogo')");

            migrationBuilder.Sql
               ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
               "VALUES " +
               "('Simulação', 'Jogos que reproduzem atividades da vida real ou sistemas complexos.', 'Jogo')");

            migrationBuilder.Sql
              ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
              "VALUES " +
              "('Terror', 'Experiências focadas em suspense, medo e sobrevivência.', 'Jogo')");

            migrationBuilder.Sql
              ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
              "VALUES " +
              "('Esporte', 'Simulações e competições de esportes reais ou fictícios.', 'Jogo')");

            migrationBuilder.Sql
              ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
              "VALUES " +
              "('Corrida', 'Jogos de velocidade com carros, motos ou veículos futuristas', 'Jogo')");

            migrationBuilder.Sql
              ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
              "VALUES " +
              "('Luta', 'Confrontos diretos entre personagens com golpes e combos.', 'Jogo')");


            // Categorias de Produtos

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Roupas', 'Roupas temáticas inspiradas em games, séries e filmes.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Bonés', 'Acessórios de cabeça com logos e personagens famosos.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Action Figures', 'Bonecos e estátuas detalhadas de personagens icônicos.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Canecas', 'Itens personalizados para fãs de franquias famosas.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Posters', 'Pôsteres e quadros decorativos de jogos, séries e animes.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Mousepads', 'Mousepads personalizados com arte gamer e personagens.', 'ProdutoGeek')");

            migrationBuilder.Sql
                ("INSERT INTO Categorias (Nome, Descricao, TipoCategoria) " +
                "VALUES " +
                "('Funko Pop', 'Miniaturas estilizadas de personagens famosos.', 'ProdutoGeek')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Categorias");
        }
    }
}

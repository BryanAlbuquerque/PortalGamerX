using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGamerX.Migrations
{
    /// <summary>
    /// Migra a plataforma de "produtos" para "jogos": renomeia tabelas, colunas, constraints e índices
    /// (os dados existentes são preservados) e cria os campos específicos de jogos e os campos fiscais.
    /// </summary>
    public partial class ProdutoParaJogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1) Renomeações (tabelas, colunas, constraints, índices) ----
            migrationBuilder.RenameTable(name: "Products", newName: "Jogos");
            migrationBuilder.RenameTable(name: "ProductImages", newName: "JogoImages");
            migrationBuilder.RenameTable(name: "CouponProducts", newName: "CouponJogos");

            migrationBuilder.RenameColumn(name: "ProductId", table: "CartItems", newName: "JogoId");
            migrationBuilder.RenameColumn(name: "ProductId", table: "OrderItems", newName: "JogoId");
            migrationBuilder.RenameColumn(name: "ProductName", table: "OrderItems", newName: "JogoName");
            migrationBuilder.RenameColumn(name: "ProductId", table: "JogoImages", newName: "JogoId");
            migrationBuilder.RenameColumn(name: "ProductId", table: "StockMovements", newName: "JogoId");
            migrationBuilder.RenameColumn(name: "ProductsId", table: "CouponJogos", newName: "JogosId");
            migrationBuilder.RenameColumn(name: "MaxItemQuantityPerProduct", table: "StoreSettings", newName: "MaxItemQuantityPerJogo");

            migrationBuilder.Sql("ALTER TABLE \"Jogos\" RENAME CONSTRAINT \"PK_Products\" TO \"PK_Jogos\";");
            migrationBuilder.Sql("ALTER TABLE \"Jogos\" RENAME CONSTRAINT \"CK_Products_Price_Positive\" TO \"CK_Jogos_Price_Positive\";");
            migrationBuilder.Sql("ALTER TABLE \"Jogos\" RENAME CONSTRAINT \"CK_Products_Stock_NonNegative\" TO \"CK_Jogos_Stock_NonNegative\";");
            migrationBuilder.Sql("ALTER TABLE \"Jogos\" RENAME CONSTRAINT \"FK_Products_Brands_BrandId\" TO \"FK_Jogos_Brands_BrandId\";");
            migrationBuilder.Sql("ALTER TABLE \"Jogos\" RENAME CONSTRAINT \"FK_Products_Categories_CategoryId\" TO \"FK_Jogos_Categories_CategoryId\";");
            migrationBuilder.Sql("ALTER TABLE \"JogoImages\" RENAME CONSTRAINT \"PK_ProductImages\" TO \"PK_JogoImages\";");
            migrationBuilder.Sql("ALTER TABLE \"JogoImages\" RENAME CONSTRAINT \"FK_ProductImages_Products_ProductId\" TO \"FK_JogoImages_Jogos_JogoId\";");
            migrationBuilder.Sql("ALTER TABLE \"CartItems\" RENAME CONSTRAINT \"FK_CartItems_Products_ProductId\" TO \"FK_CartItems_Jogos_JogoId\";");
            migrationBuilder.Sql("ALTER TABLE \"OrderItems\" RENAME CONSTRAINT \"FK_OrderItems_Products_ProductId\" TO \"FK_OrderItems_Jogos_JogoId\";");
            migrationBuilder.Sql("ALTER TABLE \"StockMovements\" RENAME CONSTRAINT \"FK_StockMovements_Products_ProductId\" TO \"FK_StockMovements_Jogos_JogoId\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponJogos\" RENAME CONSTRAINT \"PK_CouponProducts\" TO \"PK_CouponJogos\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponJogos\" RENAME CONSTRAINT \"FK_CouponProducts_Coupons_CouponId\" TO \"FK_CouponJogos_Coupons_CouponId\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponJogos\" RENAME CONSTRAINT \"FK_CouponProducts_Products_ProductsId\" TO \"FK_CouponJogos_Jogos_JogosId\";");

            migrationBuilder.RenameIndex(name: "IX_CartItems_CartId_ProductId", table: "CartItems", newName: "IX_CartItems_CartId_JogoId");
            migrationBuilder.RenameIndex(name: "IX_CartItems_ProductId", table: "CartItems", newName: "IX_CartItems_JogoId");
            migrationBuilder.RenameIndex(name: "IX_CouponProducts_ProductsId", table: "CouponJogos", newName: "IX_CouponJogos_JogosId");
            migrationBuilder.RenameIndex(name: "IX_OrderItems_ProductId", table: "OrderItems", newName: "IX_OrderItems_JogoId");
            migrationBuilder.RenameIndex(name: "IX_ProductImages_ProductId", table: "JogoImages", newName: "IX_JogoImages_JogoId");
            migrationBuilder.RenameIndex(name: "IX_Products_BrandId", table: "Jogos", newName: "IX_Jogos_BrandId");
            migrationBuilder.RenameIndex(name: "IX_Products_CategoryId", table: "Jogos", newName: "IX_Jogos_CategoryId");
            migrationBuilder.RenameIndex(name: "IX_Products_IsActive_IsFeatured", table: "Jogos", newName: "IX_Jogos_IsActive_IsFeatured");
            migrationBuilder.RenameIndex(name: "IX_Products_Sku", table: "Jogos", newName: "IX_Jogos_Sku");
            migrationBuilder.RenameIndex(name: "IX_Products_Slug", table: "Jogos", newName: "IX_Jogos_Slug");
            migrationBuilder.RenameIndex(name: "IX_StockMovements_ProductId_CreatedAtUtc", table: "StockMovements", newName: "IX_StockMovements_JogoId_CreatedAtUtc");

            // ---- 2) Novos campos do jogo ----
            migrationBuilder.AddColumn<int>(name: "Platform", table: "Jogos", type: "integer", nullable: false, defaultValue: 99); // 99 = Outra plataforma (ajuste no painel)
            migrationBuilder.AddColumn<int>(name: "MediaType", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "AgeRating", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateOnly>(name: "ReleaseDate", table: "Jogos", type: "date", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Cest", table: "Jogos", type: "character varying(7)", maxLength: 7, nullable: true);
            migrationBuilder.AddColumn<int>(name: "OriginCode", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.CreateIndex(name: "IX_Jogos_Platform", table: "Jogos", column: "Platform");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Jogos_Platform", table: "Jogos");

            migrationBuilder.DropColumn(name: "Platform", table: "Jogos");
            migrationBuilder.DropColumn(name: "MediaType", table: "Jogos");
            migrationBuilder.DropColumn(name: "AgeRating", table: "Jogos");
            migrationBuilder.DropColumn(name: "ReleaseDate", table: "Jogos");
            migrationBuilder.DropColumn(name: "Cest", table: "Jogos");
            migrationBuilder.DropColumn(name: "OriginCode", table: "Jogos");

            migrationBuilder.RenameTable(name: "CouponJogos", newName: "CouponProducts");
            migrationBuilder.RenameTable(name: "JogoImages", newName: "ProductImages");
            migrationBuilder.RenameTable(name: "Jogos", newName: "Products");

            migrationBuilder.RenameColumn(name: "MaxItemQuantityPerJogo", table: "StoreSettings", newName: "MaxItemQuantityPerProduct");
            migrationBuilder.RenameColumn(name: "JogosId", table: "CouponProducts", newName: "ProductsId");
            migrationBuilder.RenameColumn(name: "JogoId", table: "StockMovements", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "JogoId", table: "ProductImages", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "JogoName", table: "OrderItems", newName: "ProductName");
            migrationBuilder.RenameColumn(name: "JogoId", table: "OrderItems", newName: "ProductId");
            migrationBuilder.RenameColumn(name: "JogoId", table: "CartItems", newName: "ProductId");

            migrationBuilder.Sql("ALTER TABLE \"Products\" RENAME CONSTRAINT \"PK_Jogos\" TO \"PK_Products\";");
            migrationBuilder.Sql("ALTER TABLE \"Products\" RENAME CONSTRAINT \"CK_Jogos_Price_Positive\" TO \"CK_Products_Price_Positive\";");
            migrationBuilder.Sql("ALTER TABLE \"Products\" RENAME CONSTRAINT \"CK_Jogos_Stock_NonNegative\" TO \"CK_Products_Stock_NonNegative\";");
            migrationBuilder.Sql("ALTER TABLE \"Products\" RENAME CONSTRAINT \"FK_Jogos_Brands_BrandId\" TO \"FK_Products_Brands_BrandId\";");
            migrationBuilder.Sql("ALTER TABLE \"Products\" RENAME CONSTRAINT \"FK_Jogos_Categories_CategoryId\" TO \"FK_Products_Categories_CategoryId\";");
            migrationBuilder.Sql("ALTER TABLE \"ProductImages\" RENAME CONSTRAINT \"PK_JogoImages\" TO \"PK_ProductImages\";");
            migrationBuilder.Sql("ALTER TABLE \"ProductImages\" RENAME CONSTRAINT \"FK_JogoImages_Jogos_JogoId\" TO \"FK_ProductImages_Products_ProductId\";");
            migrationBuilder.Sql("ALTER TABLE \"CartItems\" RENAME CONSTRAINT \"FK_CartItems_Jogos_JogoId\" TO \"FK_CartItems_Products_ProductId\";");
            migrationBuilder.Sql("ALTER TABLE \"OrderItems\" RENAME CONSTRAINT \"FK_OrderItems_Jogos_JogoId\" TO \"FK_OrderItems_Products_ProductId\";");
            migrationBuilder.Sql("ALTER TABLE \"StockMovements\" RENAME CONSTRAINT \"FK_StockMovements_Jogos_JogoId\" TO \"FK_StockMovements_Products_ProductId\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponProducts\" RENAME CONSTRAINT \"PK_CouponJogos\" TO \"PK_CouponProducts\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponProducts\" RENAME CONSTRAINT \"FK_CouponJogos_Coupons_CouponId\" TO \"FK_CouponProducts_Coupons_CouponId\";");
            migrationBuilder.Sql("ALTER TABLE \"CouponProducts\" RENAME CONSTRAINT \"FK_CouponJogos_Jogos_JogosId\" TO \"FK_CouponProducts_Products_ProductsId\";");

            migrationBuilder.RenameIndex(name: "IX_CartItems_CartId_JogoId", table: "CartItems", newName: "IX_CartItems_CartId_ProductId");
            migrationBuilder.RenameIndex(name: "IX_CartItems_JogoId", table: "CartItems", newName: "IX_CartItems_ProductId");
            migrationBuilder.RenameIndex(name: "IX_CouponJogos_JogosId", table: "CouponProducts", newName: "IX_CouponProducts_ProductsId");
            migrationBuilder.RenameIndex(name: "IX_OrderItems_JogoId", table: "OrderItems", newName: "IX_OrderItems_ProductId");
            migrationBuilder.RenameIndex(name: "IX_JogoImages_JogoId", table: "ProductImages", newName: "IX_ProductImages_ProductId");
            migrationBuilder.RenameIndex(name: "IX_Jogos_BrandId", table: "Products", newName: "IX_Products_BrandId");
            migrationBuilder.RenameIndex(name: "IX_Jogos_CategoryId", table: "Products", newName: "IX_Products_CategoryId");
            migrationBuilder.RenameIndex(name: "IX_Jogos_IsActive_IsFeatured", table: "Products", newName: "IX_Products_IsActive_IsFeatured");
            migrationBuilder.RenameIndex(name: "IX_Jogos_Sku", table: "Products", newName: "IX_Products_Sku");
            migrationBuilder.RenameIndex(name: "IX_Jogos_Slug", table: "Products", newName: "IX_Products_Slug");
            migrationBuilder.RenameIndex(name: "IX_StockMovements_JogoId_CreatedAtUtc", table: "StockMovements", newName: "IX_StockMovements_ProductId_CreatedAtUtc");
        }
    }
}

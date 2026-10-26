using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PortalGamerX.Migrations
{
    /// <summary>
    /// Converte a loja para venda de KEYS digitais: remove campos de mídia física e de frete, cria o inventário de keys
    /// (<c>GameKeys</c>) e renomeia o endereço do pedido para endereço de cobrança.
    /// ATENÇÃO: dados de frete/rastreio dos pedidos antigos são descartados e o saldo de estoque antigo é zerado (as keys
    /// precisam ser importadas). Faça backup antes de aplicar.
    /// </summary>
    public partial class LojaDeKeysDigitais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Jogos: campos físicos saem; entram loja de ativação, região e instruções ----
            foreach (var col in new[] { "Barcode", "Ncm", "Cest", "OriginCode", "MediaType", "WeightKg", "HeightCm", "WidthCm", "LengthCm" })
                migrationBuilder.DropColumn(name: col, table: "Jogos");

            migrationBuilder.AddColumn<int>(name: "ActivationStore", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "Region", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "ActivationInstructions", table: "Jogos", type: "character varying(2000)", maxLength: 2000, nullable: true);

            // O saldo de um jogo passa a ser o número de keys disponíveis. Ainda não há keys cadastradas: zera o saldo antigo
            // (estoque físico), senão o contador divergiria do inventário. Importe as keys em Admin > Estoque de keys.
            migrationBuilder.Sql("UPDATE \"Jogos\" SET \"StockQuantity\" = 0;");

            // ---- Pedidos: sem frete/rastreio; o endereço vira endereço de cobrança (nota fiscal) ----
            foreach (var col in new[] { "ShippingTotal", "ShippingServiceCode", "ShippingServiceName", "ShippingCarrier", "ShippingDeliveryDays", "ShippingIsSimulated", "TrackingCode", "ShipRecipient" })
                migrationBuilder.DropColumn(name: col, table: "Orders");
            foreach (var part in new[] { "PostalCode", "Street", "Number", "Complement", "District", "City", "State" })
                migrationBuilder.RenameColumn(name: "Ship" + part, table: "Orders", newName: "Billing" + part);

            // ---- Textos institucionais ----
            migrationBuilder.RenameColumn(name: "ShippingPolicyText", table: "StoreSettings", newName: "DeliveryPolicyText");

            // ---- Inventário de keys ----
            migrationBuilder.CreateTable(
                name: "GameKeys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JogoId = table.Column<int>(type: "integer", nullable: false),
                    CodeEncrypted = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeHint = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameKeys", x => x.Id);
                    table.ForeignKey(name: "FK_GameKeys_Jogos_JogoId", column: x => x.JogoId, principalTable: "Jogos", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_GameKeys_CodeHash", table: "GameKeys", column: "CodeHash", unique: true);
            migrationBuilder.CreateIndex(name: "IX_GameKeys_JogoId_Status", table: "GameKeys", columns: new[] { "JogoId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_GameKeys_OrderId", table: "GameKeys", column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GameKeys");

            migrationBuilder.RenameColumn(name: "DeliveryPolicyText", table: "StoreSettings", newName: "ShippingPolicyText");

            foreach (var part in new[] { "PostalCode", "Street", "Number", "Complement", "District", "City", "State" })
                migrationBuilder.RenameColumn(name: "Billing" + part, table: "Orders", newName: "Ship" + part);
            // Os dados de frete/rastreio apagados no Up não podem ser recuperados: as colunas voltam vazias.
            migrationBuilder.AddColumn<decimal>(name: "ShippingTotal", table: "Orders", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "ShippingServiceCode", table: "Orders", type: "character varying(60)", maxLength: 60, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ShippingServiceName", table: "Orders", type: "character varying(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ShippingCarrier", table: "Orders", type: "character varying(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<int>(name: "ShippingDeliveryDays", table: "Orders", type: "integer", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "ShippingIsSimulated", table: "Orders", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "TrackingCode", table: "Orders", type: "character varying(60)", maxLength: 60, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ShipRecipient", table: "Orders", type: "character varying(150)", maxLength: 150, nullable: false, defaultValue: "");

            migrationBuilder.DropColumn(name: "ActivationInstructions", table: "Jogos");
            migrationBuilder.DropColumn(name: "Region", table: "Jogos");
            migrationBuilder.DropColumn(name: "ActivationStore", table: "Jogos");

            migrationBuilder.AddColumn<string>(name: "Barcode", table: "Jogos", type: "character varying(60)", maxLength: 60, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Ncm", table: "Jogos", type: "character varying(8)", maxLength: 8, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Cest", table: "Jogos", type: "character varying(7)", maxLength: 7, nullable: true);
            migrationBuilder.AddColumn<int>(name: "OriginCode", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "MediaType", table: "Jogos", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "WeightKg", table: "Jogos", type: "numeric(10,3)", precision: 10, scale: 3, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "HeightCm", table: "Jogos", type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "WidthCm", table: "Jogos", type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "LengthCm", table: "Jogos", type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m);
        }
    }
}

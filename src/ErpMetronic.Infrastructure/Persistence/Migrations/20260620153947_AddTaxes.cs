using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpMetronic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "SalesOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "SalesOrders",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingTaxId",
                table: "SalesOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SalesOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "SalesOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SalesOrderItems",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "SalesInvoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "SalesInvoices",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingTaxId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "SalesInvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SalesInvoiceLines",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "PurchaseOrders",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingTaxId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "PurchaseOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "PurchaseOrderItems",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "PurchaseInvoices",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingTaxId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseInvoiceLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "PurchaseInvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "PurchaseInvoiceLines",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Taxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AppliesTo = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_WithholdingTaxId",
                table: "SalesOrders",
                column: "WithholdingTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_TaxId",
                table: "SalesOrderItems",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_WithholdingTaxId",
                table: "SalesInvoices",
                column: "WithholdingTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TaxId",
                table: "SalesInvoiceLines",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WithholdingTaxId",
                table: "PurchaseOrders",
                column: "WithholdingTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TaxId",
                table: "PurchaseOrderItems",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_WithholdingTaxId",
                table: "PurchaseInvoices",
                column: "WithholdingTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_TaxId",
                table: "PurchaseInvoiceLines",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_Code",
                table: "Taxes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoiceLines_Taxes_TaxId",
                table: "PurchaseInvoiceLines",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Taxes_WithholdingTaxId",
                table: "PurchaseInvoices",
                column: "WithholdingTaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Taxes_TaxId",
                table: "PurchaseOrderItems",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Taxes_WithholdingTaxId",
                table: "PurchaseOrders",
                column: "WithholdingTaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_Taxes_TaxId",
                table: "SalesInvoiceLines",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Taxes_WithholdingTaxId",
                table: "SalesInvoices",
                column: "WithholdingTaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderItems_Taxes_TaxId",
                table: "SalesOrderItems",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Taxes_WithholdingTaxId",
                table: "SalesOrders",
                column: "WithholdingTaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoiceLines_Taxes_TaxId",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Taxes_WithholdingTaxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Taxes_TaxId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Taxes_WithholdingTaxId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceLines_Taxes_TaxId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Taxes_WithholdingTaxId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderItems_Taxes_TaxId",
                table: "SalesOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Taxes_WithholdingTaxId",
                table: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_WithholdingTaxId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderItems_TaxId",
                table: "SalesOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_WithholdingTaxId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceLines_TaxId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_WithholdingTaxId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_TaxId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_WithholdingTaxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoiceLines_TaxId",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "PurchaseInvoiceLines");
        }
    }
}

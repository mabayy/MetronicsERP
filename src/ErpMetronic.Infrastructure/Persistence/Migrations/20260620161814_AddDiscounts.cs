using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpMetronic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountAmount",
                table: "SalesOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountPercent",
                table: "SalesOrders",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SalesOrderItems",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountAmount",
                table: "SalesInvoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountPercent",
                table: "SalesInvoices",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SalesInvoiceLines",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountPercent",
                table: "PurchaseOrders",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "PurchaseOrderItems",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountAmount",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HeaderDiscountPercent",
                table: "PurchaseInvoices",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "PurchaseInvoiceLines",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderDiscountAmount",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountPercent",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountAmount",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountPercent",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountPercent",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountAmount",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "HeaderDiscountPercent",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PurchaseInvoiceLines");
        }
    }
}

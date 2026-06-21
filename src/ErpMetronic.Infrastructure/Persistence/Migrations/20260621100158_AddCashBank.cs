using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpMetronic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashBankAccountId",
                table: "SalesPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashBankAccountId",
                table: "PurchasePayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "JournalLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledDate",
                table: "JournalLines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayments_CashBankAccountId",
                table: "SalesPayments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_CashBankAccountId",
                table: "PurchasePayments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBankAccounts_Code",
                table: "CashBankAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchasePayments_CashBankAccounts_CashBankAccountId",
                table: "PurchasePayments",
                column: "CashBankAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPayments_CashBankAccounts_CashBankAccountId",
                table: "SalesPayments",
                column: "CashBankAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchasePayments_CashBankAccounts_CashBankAccountId",
                table: "PurchasePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPayments_CashBankAccounts_CashBankAccountId",
                table: "SalesPayments");

            migrationBuilder.DropTable(
                name: "CashBankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SalesPayments_CashBankAccountId",
                table: "SalesPayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_CashBankAccountId",
                table: "PurchasePayments");

            migrationBuilder.DropColumn(
                name: "CashBankAccountId",
                table: "SalesPayments");

            migrationBuilder.DropColumn(
                name: "CashBankAccountId",
                table: "PurchasePayments");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "ReconciledDate",
                table: "JournalLines");
        }
    }
}

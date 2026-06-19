using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpMetronic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tambah kolom baru (Code sementara default "")
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DocumentNumberSequences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "DocumentNumberSequences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2. Migrasikan data dari DocumentType (enum lama) ke Code, tandai bawaan sistem
            migrationBuilder.Sql(@"
UPDATE [DocumentNumberSequences]
SET [Code] = CASE [DocumentType]
        WHEN 1 THEN 'PO'  WHEN 2 THEN 'GR'  WHEN 3 THEN 'DO'
        WHEN 4 THEN 'IN'  WHEN 5 THEN 'OUT' WHEN 6 THEN 'TRF' WHEN 7 THEN 'ADJ'
        ELSE CONCAT('DOC', CAST([Id] AS varchar(10))) END,
    [IsSystem] = 1;");

            // 3. Hapus indeks & kolom lama
            migrationBuilder.DropIndex(
                name: "IX_DocumentNumberSequences_DocumentType",
                table: "DocumentNumberSequences");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "DocumentNumberSequences");

            // 4. Indeks unik pada Code
            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_Code",
                table: "DocumentNumberSequences",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "DocumentNumberSequences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE [DocumentNumberSequences]
SET [DocumentType] = CASE [Code]
        WHEN 'PO' THEN 1 WHEN 'GR' THEN 2 WHEN 'DO' THEN 3
        WHEN 'IN' THEN 4 WHEN 'OUT' THEN 5 WHEN 'TRF' THEN 6 WHEN 'ADJ' THEN 7
        ELSE 0 END;");

            migrationBuilder.DropIndex(
                name: "IX_DocumentNumberSequences_Code",
                table: "DocumentNumberSequences");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DocumentNumberSequences");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "DocumentNumberSequences");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_DocumentType",
                table: "DocumentNumberSequences",
                column: "DocumentType",
                unique: true);
        }
    }
}

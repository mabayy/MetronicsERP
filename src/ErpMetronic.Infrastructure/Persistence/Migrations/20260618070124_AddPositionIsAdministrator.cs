using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpMetronic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionIsAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "Positions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdministrator",
                table: "Positions");
        }
    }
}

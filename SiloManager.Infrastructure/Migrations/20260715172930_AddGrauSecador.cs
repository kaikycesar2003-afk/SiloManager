using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiloManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGrauSecador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GrauSecador",
                table: "Medicoes",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrauSecador",
                table: "Medicoes");
        }
    }
}

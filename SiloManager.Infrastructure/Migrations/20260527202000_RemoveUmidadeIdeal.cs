using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiloManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUmidadeIdeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UmidadeIdeal",
                table: "Produtos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "UmidadeIdeal",
                table: "Produtos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}

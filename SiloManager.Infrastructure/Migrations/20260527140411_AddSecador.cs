using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiloManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecadorId",
                table: "Medicoes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Secadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Secadores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_SecadorId",
                table: "Medicoes",
                column: "SecadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Secadores_EmpresaId",
                table: "Secadores",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medicoes_Secadores_SecadorId",
                table: "Medicoes",
                column: "SecadorId",
                principalTable: "Secadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicoes_Secadores_SecadorId",
                table: "Medicoes");

            migrationBuilder.DropTable(
                name: "Secadores");

            migrationBuilder.DropIndex(
                name: "IX_Medicoes_SecadorId",
                table: "Medicoes");

            migrationBuilder.DropColumn(
                name: "SecadorId",
                table: "Medicoes");
        }
    }
}

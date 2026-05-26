using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiloManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CNPJ = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UmidadeMinima = table.Column<double>(type: "REAL", nullable: false),
                    UmidadeIdeal = table.Column<double>(type: "REAL", nullable: false),
                    UmidadeMaxima = table.Column<double>(type: "REAL", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervaloMinimoSegundos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 900)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuracoes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumeroSerie = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PortaCOM = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BaudRate = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 9600),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipamentos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Login = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Silos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRetrabalho = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Silos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Silos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Silos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Medicoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    SiloDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Umidade = table.Column<double>(type: "REAL", nullable: false),
                    DataHoraSistema = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataHoraEquipamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IntervaloSegundos = table.Column<int>(type: "INTEGER", nullable: true),
                    IsRetrabalho = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DadosBrutosSerial = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicoes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Silos_SiloDestinoId",
                        column: x => x.SiloDestinoId,
                        principalTable: "Silos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_EmpresaId",
                table: "Configuracoes",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_EmpresaId",
                table: "Equipamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId",
                table: "Medicoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EquipamentoId",
                table: "Medicoes",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_ProdutoId",
                table: "Medicoes",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_SiloDestinoId",
                table: "Medicoes",
                column: "SiloDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_UsuarioId",
                table: "Medicoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Silos_EmpresaId",
                table: "Silos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Silos_ProdutoId",
                table: "Silos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId_Login",
                table: "Usuarios",
                columns: new[] { "EmpresaId", "Login" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropTable(
                name: "Medicoes");

            migrationBuilder.DropTable(
                name: "Equipamentos");

            migrationBuilder.DropTable(
                name: "Silos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}

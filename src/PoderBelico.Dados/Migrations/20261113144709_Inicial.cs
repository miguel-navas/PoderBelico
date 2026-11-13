using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PoderBelico.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evento",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", nullable: false),
                    evento_pai_id = table.Column<int>(type: "INTEGER", nullable: true),
                    data_inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    local = table.Column<string>(type: "TEXT", nullable: true),
                    descricao = table.Column<string>(type: "TEXT", nullable: true),
                    resultado = table.Column<string>(type: "TEXT", nullable: true),
                    confiabilidade = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento", x => x.id);
                    table.CheckConstraint("ck_evento_confiabilidade", "confiabilidade IN ('CONFIRMADO','ESTIMADO','CONTESTADO')");
                    table.CheckConstraint("ck_evento_tipo", "tipo IN ('GUERRA','CAMPANHA','BATALHA')");
                    table.ForeignKey(
                        name: "FK_evento_evento_evento_pai_id",
                        column: x => x.evento_pai_id,
                        principalTable: "evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fonte",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tipo = table.Column<string>(type: "TEXT", nullable: false),
                    titulo = table.Column<string>(type: "TEXT", nullable: false),
                    autor = table.Column<string>(type: "TEXT", nullable: true),
                    ano = table.Column<int>(type: "INTEGER", nullable: true),
                    url = table.Column<string>(type: "TEXT", nullable: true),
                    acessado_em = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fonte", x => x.id);
                    table.CheckConstraint("ck_fonte_tipo", "tipo IN ('LIVRO','ARTIGO','SITE','ARQUIVO')");
                });

            migrationBuilder.CreateTable(
                name: "forca",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    codigo = table.Column<string>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forca", x => x.id);
                    table.CheckConstraint("ck_forca_codigo", "codigo IN ('MARINHA','AEREA','EXERCITO')");
                });

            migrationBuilder.CreateTable(
                name: "pais",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    codigo_iso = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false),
                    ano_inicio = table.Column<int>(type: "INTEGER", nullable: true),
                    ano_fim = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pais", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evento_fonte",
                columns: table => new
                {
                    evento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    fonte_id = table.Column<int>(type: "INTEGER", nullable: false),
                    referencia = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_fonte", x => new { x.evento_id, x.fonte_id });
                    table.ForeignKey(
                        name: "FK_evento_fonte_evento_evento_id",
                        column: x => x.evento_id,
                        principalTable: "evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_fonte_fonte_fonte_id",
                        column: x => x.fonte_id,
                        principalTable: "fonte",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    forca_id = table.Column<int>(type: "INTEGER", nullable: false),
                    codigo = table.Column<string>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_categoria_forca_forca_id",
                        column: x => x.forca_id,
                        principalTable: "forca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_pais",
                columns: table => new
                {
                    evento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pais_id = table.Column<int>(type: "INTEGER", nullable: false),
                    lado = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_pais", x => new { x.evento_id, x.pais_id });
                    table.ForeignKey(
                        name: "FK_evento_pais_evento_evento_id",
                        column: x => x.evento_id,
                        principalTable: "evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_pais_pais_pais_id",
                        column: x => x.pais_id,
                        principalTable: "pais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipamento",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false),
                    variante = table.Column<string>(type: "TEXT", nullable: true),
                    pais_id = table.Column<int>(type: "INTEGER", nullable: false),
                    forca_id = table.Column<int>(type: "INTEGER", nullable: false),
                    categoria_id = table.Column<int>(type: "INTEGER", nullable: false),
                    fabricante = table.Column<string>(type: "TEXT", nullable: true),
                    ano_primeiro_uso = table.Column<int>(type: "INTEGER", nullable: true),
                    ano_entrada_servico = table.Column<int>(type: "INTEGER", nullable: true),
                    unidades_produzidas = table.Column<int>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    descricao_curta = table.Column<string>(type: "TEXT", nullable: true),
                    curiosidade = table.Column<string>(type: "TEXT", nullable: true),
                    imagem_url = table.Column<string>(type: "TEXT", nullable: true),
                    imagem_credito = table.Column<string>(type: "TEXT", nullable: true),
                    especificacoes = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    confiabilidade = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamento", x => x.id);
                    table.CheckConstraint("ck_equipamento_confiabilidade", "confiabilidade IN ('CONFIRMADO','ESTIMADO','CONTESTADO')");
                    table.CheckConstraint("ck_equipamento_especificacoes", "json_valid(especificacoes)");
                    table.CheckConstraint("ck_equipamento_status", "status IN ('ATIVO','APOSENTADO','PROTOTIPO')");
                    table.ForeignKey(
                        name: "FK_equipamento_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipamento_forca_forca_id",
                        column: x => x.forca_id,
                        principalTable: "forca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipamento_pais_pais_id",
                        column: x => x.pais_id,
                        principalTable: "pais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipamento_fonte",
                columns: table => new
                {
                    equipamento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    fonte_id = table.Column<int>(type: "INTEGER", nullable: false),
                    referencia = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamento_fonte", x => new { x.equipamento_id, x.fonte_id });
                    table.ForeignKey(
                        name: "FK_equipamento_fonte_equipamento_equipamento_id",
                        column: x => x.equipamento_id,
                        principalTable: "equipamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipamento_fonte_fonte_fonte_id",
                        column: x => x.fonte_id,
                        principalTable: "fonte",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipamento_operador",
                columns: table => new
                {
                    equipamento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pais_id = table.Column<int>(type: "INTEGER", nullable: false),
                    observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamento_operador", x => new { x.equipamento_id, x.pais_id });
                    table.ForeignKey(
                        name: "FK_equipamento_operador_equipamento_equipamento_id",
                        column: x => x.equipamento_id,
                        principalTable: "equipamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipamento_operador_pais_pais_id",
                        column: x => x.pais_id,
                        principalTable: "pais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    equipamento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    evento_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pais_id = table.Column<int>(type: "INTEGER", nullable: false),
                    papel = table.Column<string>(type: "TEXT", nullable: false),
                    quantidade_aprox = table.Column<int>(type: "INTEGER", nullable: true),
                    observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participacao", x => x.id);
                    table.ForeignKey(
                        name: "FK_participacao_equipamento_equipamento_id",
                        column: x => x.equipamento_id,
                        principalTable: "equipamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_participacao_evento_evento_id",
                        column: x => x.evento_id,
                        principalTable: "evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_participacao_pais_pais_id",
                        column: x => x.pais_id,
                        principalTable: "pais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "forca",
                columns: new[] { "id", "codigo", "nome" },
                values: new object[,]
                {
                    { 1, "MARINHA", "Marinha" },
                    { 2, "AEREA", "Força Aérea" },
                    { 3, "EXERCITO", "Exército" }
                });

            migrationBuilder.InsertData(
                table: "categoria",
                columns: new[] { "id", "codigo", "forca_id", "nome" },
                values: new object[,]
                {
                    { 1, "NAVIO", 1, "Navio" },
                    { 2, "AERONAVE", 2, "Aeronave" },
                    { 3, "TANQUE", 3, "Tanque" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_categoria_codigo",
                table: "categoria",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categoria_forca_id",
                table: "categoria",
                column: "forca_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_categoria_id",
                table: "equipamento",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipamento_filtros",
                table: "equipamento",
                columns: new[] { "forca_id", "categoria_id", "pais_id", "ano_entrada_servico" });

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_pais_id",
                table: "equipamento",
                column: "pais_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_slug",
                table: "equipamento",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_fonte_fonte_id",
                table: "equipamento_fonte",
                column: "fonte_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_operador_pais_id",
                table: "equipamento_operador",
                column: "pais_id");

            migrationBuilder.CreateIndex(
                name: "IX_evento_evento_pai_id",
                table: "evento",
                column: "evento_pai_id");

            migrationBuilder.CreateIndex(
                name: "ix_evento_periodo",
                table: "evento",
                column: "data_inicio");

            migrationBuilder.CreateIndex(
                name: "IX_evento_slug",
                table: "evento",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evento_fonte_fonte_id",
                table: "evento_fonte",
                column: "fonte_id");

            migrationBuilder.CreateIndex(
                name: "IX_evento_pais_pais_id",
                table: "evento_pais",
                column: "pais_id");

            migrationBuilder.CreateIndex(
                name: "IX_forca_codigo",
                table: "forca",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pais_codigo_iso",
                table: "pais",
                column: "codigo_iso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participacao_equipamento_id_evento_id_pais_id",
                table: "participacao",
                columns: new[] { "equipamento_id", "evento_id", "pais_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participacao_evento_id",
                table: "participacao",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "IX_participacao_pais_id",
                table: "participacao",
                column: "pais_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipamento_fonte");

            migrationBuilder.DropTable(
                name: "equipamento_operador");

            migrationBuilder.DropTable(
                name: "evento_fonte");

            migrationBuilder.DropTable(
                name: "evento_pais");

            migrationBuilder.DropTable(
                name: "participacao");

            migrationBuilder.DropTable(
                name: "fonte");

            migrationBuilder.DropTable(
                name: "equipamento");

            migrationBuilder.DropTable(
                name: "evento");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "pais");

            migrationBuilder.DropTable(
                name: "forca");
        }
    }
}

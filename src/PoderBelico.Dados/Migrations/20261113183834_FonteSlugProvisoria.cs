using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoderBelico.Dados.Migrations
{
    /// <inheritdoc />
    public partial class FonteSlugProvisoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "provisoria",
                table: "fonte",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "fonte",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_fonte_slug",
                table: "fonte",
                column: "slug",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_fonte_provisoria",
                table: "fonte",
                sql: "provisoria IN (0,1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fonte_slug",
                table: "fonte");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fonte_provisoria",
                table: "fonte");

            migrationBuilder.DropColumn(
                name: "provisoria",
                table: "fonte");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "fonte");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asada.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5bUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "debe_cambiar_contrasena",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "debe_cambiar_contrasena",
                table: "usuarios");
        }
    }
}

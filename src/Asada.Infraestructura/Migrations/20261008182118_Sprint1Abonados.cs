using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asada.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Sprint1Abonados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ultimo_correlativo_abonado",
                table: "organizaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "abonados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    correlativo = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abonados", x => x.id);
                    table.ForeignKey(
                        name: "fk_abonados_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "propiedades",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    abonado_id = table.Column<int>(type: "integer", nullable: false),
                    direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    provincia = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    canton = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    distrito = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    referencia = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_propiedades", x => x.id);
                    table.ForeignKey(
                        name: "fk_propiedades_abonados_abonado_id",
                        column: x => x.abonado_id,
                        principalTable: "abonados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_propiedades_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "servicios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    propiedad_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    es_moroso = table.Column<bool>(type: "boolean", nullable: false),
                    mensualidades_pendientes = table.Column<int>(type: "integer", nullable: false),
                    monto_pendiente = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicios", x => x.id);
                    table.ForeignKey(
                        name: "fk_servicios_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_servicios_propiedades_propiedad_id",
                        column: x => x.propiedad_id,
                        principalTable: "propiedades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abonados_organizacion_id_codigo",
                table: "abonados",
                columns: new[] { "organizacion_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_abonados_organizacion_id_correlativo",
                table: "abonados",
                columns: new[] { "organizacion_id", "correlativo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_abonados_organizacion_id_tipo_identificacion_identificacion",
                table: "abonados",
                columns: new[] { "organizacion_id", "tipo_identificacion", "identificacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_propiedades_abonado_id",
                table: "propiedades",
                column: "abonado_id");

            migrationBuilder.CreateIndex(
                name: "ix_propiedades_organizacion_id",
                table: "propiedades",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicios_organizacion_id",
                table: "servicios",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicios_propiedad_id",
                table: "servicios",
                column: "propiedad_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servicios");

            migrationBuilder.DropTable(
                name: "propiedades");

            migrationBuilder.DropTable(
                name: "abonados");

            migrationBuilder.DropColumn(
                name: "ultimo_correlativo_abonado",
                table: "organizaciones");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asada.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5aSeguridadAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mediciones_servicio_id_periodo_id",
                table: "mediciones");

            migrationBuilder.AddColumn<string>(
                name: "anulado_por_nombre",
                table: "recibos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "anulado_por_usuario_id",
                table: "recibos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_anulacion",
                table: "recibos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "generado_por_nombre",
                table: "recibos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "generado_por_usuario_id",
                table: "recibos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                table: "recibos",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cerrado_por_nombre",
                table: "periodos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cerrado_por_usuario_id",
                table: "periodos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_cierre",
                table: "periodos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_reapertura",
                table: "periodos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_reapertura",
                table: "periodos",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reabierto_por_nombre",
                table: "periodos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reabierto_por_usuario_id",
                table: "periodos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_consumo_anormal",
                table: "organizaciones",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<string>(
                name: "anulada_por_nombre",
                table: "mediciones",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "anulada_por_usuario_id",
                table: "mediciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "corregida_por_nombre",
                table: "mediciones",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corregida_por_usuario_id",
                table: "mediciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "mediciones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Vigente");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_anulacion",
                table: "mediciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_ultima_correccion",
                table: "mediciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                table: "mediciones",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_correccion",
                table: "mediciones",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registrada_por_nombre",
                table: "mediciones",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "registrada_por_usuario_id",
                table: "mediciones",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidad_id = table.Column<long>(type: "bigint", nullable: false),
                    cambios = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_servicio_id_periodo_id",
                table: "mediciones",
                columns: new[] { "servicio_id", "periodo_id" },
                unique: true,
                filter: "estado = 'Vigente'");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_entidad_entidad_id",
                table: "auditoria",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_organizacion_id_fecha",
                table: "auditoria",
                columns: new[] { "organizacion_id", "fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropIndex(
                name: "ix_mediciones_servicio_id_periodo_id",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "anulado_por_nombre",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "anulado_por_usuario_id",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "fecha_anulacion",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "generado_por_nombre",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "generado_por_usuario_id",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                table: "recibos");

            migrationBuilder.DropColumn(
                name: "cerrado_por_nombre",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "cerrado_por_usuario_id",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "fecha_cierre",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "fecha_reapertura",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "motivo_reapertura",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "reabierto_por_nombre",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "reabierto_por_usuario_id",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "porcentaje_consumo_anormal",
                table: "organizaciones");

            migrationBuilder.DropColumn(
                name: "anulada_por_nombre",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "anulada_por_usuario_id",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "corregida_por_nombre",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "corregida_por_usuario_id",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "fecha_anulacion",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "fecha_ultima_correccion",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "motivo_correccion",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "registrada_por_nombre",
                table: "mediciones");

            migrationBuilder.DropColumn(
                name: "registrada_por_usuario_id",
                table: "mediciones");

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_servicio_id_periodo_id",
                table: "mediciones",
                columns: new[] { "servicio_id", "periodo_id" },
                unique: true);
        }
    }
}

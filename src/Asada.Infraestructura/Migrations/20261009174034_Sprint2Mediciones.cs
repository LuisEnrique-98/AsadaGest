using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asada.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2Mediciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medidores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha_instalacion = table.Column<DateOnly>(type: "date", nullable: false),
                    lectura_inicial = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_retiro = table.Column<DateOnly>(type: "date", nullable: true),
                    lectura_final = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medidores", x => x.id);
                    table.ForeignKey(
                        name: "fk_medidores_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medidores_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periodos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periodos", x => x.id);
                    table.ForeignKey(
                        name: "fk_periodos_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mediciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    medidor_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_lectura = table.Column<DateOnly>(type: "date", nullable: false),
                    lectura = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    consumo = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    requiere_revision = table.Column<bool>(type: "boolean", nullable: false),
                    motivos_revision = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mediciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_mediciones_medidores_medidor_id",
                        column: x => x.medidor_id,
                        principalTable: "medidores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mediciones_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mediciones_periodos_periodo_id",
                        column: x => x.periodo_id,
                        principalTable: "periodos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mediciones_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seguimientos_lectura",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_id = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nota = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seguimientos_lectura", x => x.id);
                    table.ForeignKey(
                        name: "fk_seguimientos_lectura_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_seguimientos_lectura_periodos_periodo_id",
                        column: x => x.periodo_id,
                        principalTable: "periodos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_seguimientos_lectura_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_medidor_id",
                table: "mediciones",
                column: "medidor_id");

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_organizacion_id",
                table: "mediciones",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_periodo_id",
                table: "mediciones",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ix_mediciones_servicio_id_periodo_id",
                table: "mediciones",
                columns: new[] { "servicio_id", "periodo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medidores_organizacion_id_numero_serie",
                table: "medidores",
                columns: new[] { "organizacion_id", "numero_serie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medidores_servicio_id",
                table: "medidores",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_medidores_servicio_id1",
                table: "medidores",
                column: "servicio_id",
                unique: true,
                filter: "estado = 'Activo'");

            migrationBuilder.CreateIndex(
                name: "ix_periodos_organizacion_id_anio_mes",
                table: "periodos",
                columns: new[] { "organizacion_id", "anio", "mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seguimientos_lectura_organizacion_id",
                table: "seguimientos_lectura",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_seguimientos_lectura_periodo_id",
                table: "seguimientos_lectura",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ix_seguimientos_lectura_servicio_id_periodo_id",
                table: "seguimientos_lectura",
                columns: new[] { "servicio_id", "periodo_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mediciones");

            migrationBuilder.DropTable(
                name: "seguimientos_lectura");

            migrationBuilder.DropTable(
                name: "medidores");

            migrationBuilder.DropTable(
                name: "periodos");
        }
    }
}

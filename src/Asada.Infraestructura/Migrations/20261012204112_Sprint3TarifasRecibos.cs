using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asada.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3TarifasRecibos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "categoria_id",
                table: "servicios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias_servicio",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias_servicio", x => x.id);
                    table.ForeignKey(
                        name: "fk_categorias_servicio_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "correlativos_recibo",
                columns: table => new
                {
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    ultimo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_correlativos_recibo", x => new { x.organizacion_id, x.anio });
                    table.ForeignKey(
                        name: "fk_correlativos_recibo_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tarifas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: false),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    cargo_fijo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    modo_hidrante = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor_hidrante = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarifas", x => x.id);
                    table.ForeignKey(
                        name: "fk_tarifas_categorias_servicio_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tarifas_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recibos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion_id = table.Column<int>(type: "integer", nullable: false),
                    servicio_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_id = table.Column<int>(type: "integer", nullable: false),
                    medicion_id = table.Column<int>(type: "integer", nullable: false),
                    tarifa_id = table.Column<int>(type: "integer", nullable: false),
                    correlativo = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    abonado_codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    abonado_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    propiedad_direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    categoria_nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    numero_serie_medidor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lectura_anterior = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    lectura_actual = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    fecha_lectura = table.Column<DateOnly>(type: "date", nullable: false),
                    consumo = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    hubo_cambio_medidor = table.Column<bool>(type: "boolean", nullable: false),
                    lectura_requiere_revision = table.Column<bool>(type: "boolean", nullable: false),
                    motivos_revision = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    subtotal_agua = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    monto_hidrante = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recibos", x => x.id);
                    table.ForeignKey(
                        name: "fk_recibos_mediciones_medicion_id",
                        column: x => x.medicion_id,
                        principalTable: "mediciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recibos_organizaciones_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recibos_periodos_periodo_id",
                        column: x => x.periodo_id,
                        principalTable: "periodos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recibos_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recibos_tarifas_tarifa_id",
                        column: x => x.tarifa_id,
                        principalTable: "tarifas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tarifa_bloques",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tarifa_id = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    limite_superior = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    precio_por_metro_cubico = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarifa_bloques", x => x.id);
                    table.ForeignKey(
                        name: "fk_tarifa_bloques_tarifas_tarifa_id",
                        column: x => x.tarifa_id,
                        principalTable: "tarifas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recibo_detalles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recibo_id = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    concepto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    precio_unitario = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recibo_detalles", x => x.id);
                    table.ForeignKey(
                        name: "fk_recibo_detalles_recibos_recibo_id",
                        column: x => x.recibo_id,
                        principalTable: "recibos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_servicios_categoria_id",
                table: "servicios",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_servicio_organizacion_id_nombre",
                table: "categorias_servicio",
                columns: new[] { "organizacion_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recibo_detalles_recibo_id_orden",
                table: "recibo_detalles",
                columns: new[] { "recibo_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recibos_medicion_id",
                table: "recibos",
                column: "medicion_id");

            migrationBuilder.CreateIndex(
                name: "ix_recibos_organizacion_id_numero",
                table: "recibos",
                columns: new[] { "organizacion_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recibos_periodo_id",
                table: "recibos",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ix_recibos_servicio_id_periodo_id",
                table: "recibos",
                columns: new[] { "servicio_id", "periodo_id" },
                unique: true,
                filter: "estado = 'Emitido'");

            migrationBuilder.CreateIndex(
                name: "ix_recibos_tarifa_id",
                table: "recibos",
                column: "tarifa_id");

            migrationBuilder.CreateIndex(
                name: "ix_tarifa_bloques_tarifa_id_orden",
                table: "tarifa_bloques",
                columns: new[] { "tarifa_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_categoria_id_vigente_desde",
                table: "tarifas",
                columns: new[] { "categoria_id", "vigente_desde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_organizacion_id",
                table: "tarifas",
                column: "organizacion_id");

            migrationBuilder.AddForeignKey(
                name: "fk_servicios_categorias_servicio_categoria_id",
                table: "servicios",
                column: "categoria_id",
                principalTable: "categorias_servicio",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_servicios_categorias_servicio_categoria_id",
                table: "servicios");

            migrationBuilder.DropTable(
                name: "correlativos_recibo");

            migrationBuilder.DropTable(
                name: "recibo_detalles");

            migrationBuilder.DropTable(
                name: "tarifa_bloques");

            migrationBuilder.DropTable(
                name: "recibos");

            migrationBuilder.DropTable(
                name: "tarifas");

            migrationBuilder.DropTable(
                name: "categorias_servicio");

            migrationBuilder.DropIndex(
                name: "ix_servicios_categoria_id",
                table: "servicios");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "servicios");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ErpPikis.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAlertasWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Severidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    FechaLeida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "destinatarios_alerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_destinatarios_alerta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones_whatsapp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProximoIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AlertaId = table.Column<int>(type: "integer", nullable: true),
                    DestinatarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones_whatsapp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notificaciones_whatsapp_alertas_AlertaId",
                        column: x => x.AlertaId,
                        principalTable: "alertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notificaciones_whatsapp_destinatarios_alerta_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "destinatarios_alerta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_clave",
                table: "alertas",
                column: "Clave");

            migrationBuilder.CreateIndex(
                name: "IX_alertas_Leida_Fecha",
                table: "alertas",
                columns: new[] { "Leida", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "ux_alertas_clave_activa",
                table: "alertas",
                column: "Clave",
                unique: true,
                filter: "\"Activa\"");

            migrationBuilder.CreateIndex(
                name: "IX_destinatarios_alerta_Telefono",
                table: "destinatarios_alerta",
                column: "Telefono",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_whatsapp_AlertaId",
                table: "notificaciones_whatsapp",
                column: "AlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_whatsapp_DestinatarioId",
                table: "notificaciones_whatsapp",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_whatsapp_Estado_ProximoIntento",
                table: "notificaciones_whatsapp",
                columns: new[] { "Estado", "ProximoIntento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificaciones_whatsapp");

            migrationBuilder.DropTable(
                name: "alertas");

            migrationBuilder.DropTable(
                name: "destinatarios_alerta");
        }
    }
}

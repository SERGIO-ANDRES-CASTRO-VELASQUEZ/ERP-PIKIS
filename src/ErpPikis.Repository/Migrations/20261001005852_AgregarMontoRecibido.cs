using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpPikis.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMontoRecibido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "ventas",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "ventas");
        }
    }
}

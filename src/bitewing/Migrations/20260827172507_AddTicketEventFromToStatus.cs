using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bitewing.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEventFromToStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FromStatus",
                table: "TicketEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToStatus",
                table: "TicketEvents",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromStatus",
                table: "TicketEvents");

            migrationBuilder.DropColumn(
                name: "ToStatus",
                table: "TicketEvents");
        }
    }
}

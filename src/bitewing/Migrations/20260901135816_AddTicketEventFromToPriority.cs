using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bitewing.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEventFromToPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FromPriority",
                table: "TicketEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToPriority",
                table: "TicketEvents",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromPriority",
                table: "TicketEvents");

            migrationBuilder.DropColumn(
                name: "ToPriority",
                table: "TicketEvents");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventsynс.PostgresMigrations
{
    /// <inheritdoc />
    public partial class AddEventLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventLocation_Address",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventLocation_Coordinates",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventLocation_Address",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventLocation_Coordinates",
                table: "Events");
        }
    }
}

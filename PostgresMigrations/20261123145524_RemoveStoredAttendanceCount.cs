using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventsynс.PostgresMigrations
{
    /// <inheritdoc />
    public partial class RemoveStoredAttendanceCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendedEventCount",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendedEventCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

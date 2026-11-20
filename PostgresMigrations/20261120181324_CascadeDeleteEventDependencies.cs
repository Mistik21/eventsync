using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventsynс.PostgresMigrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteEventDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Events_EventId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Events_EventId",
                table: "Cars");

            migrationBuilder.DropForeignKey(
                name: "FK_Costs_Events_EventId",
                table: "Costs");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Events_EventId",
                table: "Items");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Events_EventId",
                table: "Activities",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Events_EventId",
                table: "Cars",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_Events_EventId",
                table: "Costs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Events_EventId",
                table: "Items",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Events_EventId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Events_EventId",
                table: "Cars");

            migrationBuilder.DropForeignKey(
                name: "FK_Costs_Events_EventId",
                table: "Costs");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Events_EventId",
                table: "Items");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Events_EventId",
                table: "Activities",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Events_EventId",
                table: "Cars",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Costs_Events_EventId",
                table: "Costs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Events_EventId",
                table: "Items",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }
    }
}

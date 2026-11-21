using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventsynс.PostgresMigrations
{
    /// <inheritdoc />
    public partial class StoreEventDebts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Debts",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            // Rebuild debts for events whose collection was opened before this column existed.
            migrationBuilder.Sql("""
                WITH participant_shares AS (
                    SELECT e."Id" AS event_id,
                           p."ParticipantsId" AS participant_id,
                           e."Budget" / COUNT(*) OVER (PARTITION BY e."Id") AS amount
                    FROM "Events" AS e
                    JOIN "EventParticipants" AS p ON p."EventId" = e."Id"
                    WHERE e."CollectionStatus" = 1
                ), event_debts AS (
                    SELECT event_id, jsonb_object_agg(participant_id::text, amount) AS debts
                    FROM participant_shares
                    GROUP BY event_id
                )
                UPDATE "Events" AS e
                SET "Debts" = event_debts.debts
                FROM event_debts
                WHERE e."Id" = event_debts.event_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Debts",
                table: "Events");
        }
    }
}

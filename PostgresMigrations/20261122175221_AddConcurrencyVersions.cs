using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventsynс.PostgresMigrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL already provides the xmin system column on both tables.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

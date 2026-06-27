using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSavedSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.saved_search (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                query_json TEXT NOT NULL,
                user_account_id UUID NOT NULL,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_saved_search_user_account_id
                ON app.saved_search (user_account_id);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

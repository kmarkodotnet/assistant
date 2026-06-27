using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRevokedSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE app.revoked_session (
                id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                session_id  varchar(256) NOT NULL UNIQUE,
                revoked_utc timestamptz NOT NULL DEFAULT NOW()
            );
            CREATE INDEX ix_revoked_session_session_id ON app.revoked_session (session_id);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.revoked_session;");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000004_AddAuditLog")]
public partial class AddAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'audit_action') THEN
                    CREATE TYPE app.audit_action AS ENUM (
                        'Create', 'Update', 'Delete', 'Login', 'LoginFailed',
                        'Approve', 'Reject', 'AiCall', 'FileAccess', 'PermissionChange', 'ExternalApiCall'
                    );
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.audit_log (
                id uuid PRIMARY KEY,
                occurred_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                user_account_id uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
                action app.audit_action NOT NULL,
                entity_type text NULL,
                entity_id uuid NULL,
                ip_address varchar(45) NULL,
                user_agent varchar(512) NULL,
                details_json jsonb NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_audit_log_occurred ON app.audit_log(occurred_utc DESC);
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_audit_log_user_occurred ON app.audit_log(user_account_id, occurred_utc DESC);
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_audit_log_entity ON app.audit_log(entity_type, entity_id);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

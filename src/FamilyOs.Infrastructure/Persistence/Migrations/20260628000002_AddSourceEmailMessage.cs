using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260628000002_AddSourceEmailMessage")]
public partial class AddSourceEmailMessage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.source (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(200) NOT NULL,
                kind VARCHAR(50) NOT NULL,
                config_json JSONB NOT NULL DEFAULT '{}',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                last_sync_utc TIMESTAMPTZ NULL,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                deleted_utc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_source_kind_active
                ON app.source (kind, is_active)
                WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.email_message (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                source_id UUID NOT NULL REFERENCES app.source(id) ON DELETE CASCADE,
                gmail_message_id VARCHAR(200) NOT NULL,
                thread_id VARCHAR(200) NULL,
                from_address VARCHAR(500) NOT NULL,
                to_addresses VARCHAR(2000) NOT NULL,
                subject VARCHAR(1000) NOT NULL,
                received_utc TIMESTAMPTZ NOT NULL,
                body_text TEXT NULL,
                body_html TEXT NULL,
                snippet VARCHAR(500) NULL,
                has_attachments BOOLEAN NOT NULL DEFAULT FALSE,
                ingest_status VARCHAR(20) NOT NULL DEFAULT 'Pending',
                processed_utc TIMESTAMPTZ NULL,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uix_email_message_source_gmail UNIQUE (source_id, gmail_message_id)
            );

            CREATE INDEX IF NOT EXISTS ix_email_message_ingest_status_pending
                ON app.email_message (ingest_status)
                WHERE ingest_status IN ('Pending', 'Failed');

            CREATE INDEX IF NOT EXISTS ix_email_message_received_utc
                ON app.email_message (received_utc DESC);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.email_message;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.source;");
    }
}

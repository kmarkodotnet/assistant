using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000011_AddDeadlineTaskEntities")]
public partial class AddDeadlineTaskEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.deadline (
                id UUID PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT NULL,
                due_date_utc TIMESTAMPTZ NOT NULL,
                status TEXT NOT NULL DEFAULT 'Upcoming',
                category TEXT NOT NULL DEFAULT 'Other',
                origin app.origin NOT NULL,
                source_document_id UUID NULL REFERENCES app.document(id) ON DELETE SET NULL,
                related_family_member_id UUID NULL,
                created_by_user_account_id UUID NOT NULL,
                is_private BOOLEAN NOT NULL DEFAULT false,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                deleted_utc TIMESTAMPTZ NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_deadline_source_title
                ON app.deadline (source_document_id, title)
                WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.task (
                id UUID PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT NULL,
                due_date_utc TIMESTAMPTZ NULL,
                status TEXT NOT NULL DEFAULT 'Suggested',
                priority TEXT NOT NULL DEFAULT 'Normal',
                origin app.origin NOT NULL,
                source_document_id UUID NULL REFERENCES app.document(id) ON DELETE SET NULL,
                assigned_to_family_member_id UUID NULL,
                created_by_user_account_id UUID NOT NULL,
                is_private BOOLEAN NOT NULL DEFAULT false,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                deleted_utc TIMESTAMPTZ NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_task_source_title
                ON app.task (source_document_id, title)
                WHERE deleted_utc IS NULL;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

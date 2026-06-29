using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000007_AddAiProcessingJob")]
public partial class AddAiProcessingJob : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.ai_processing_job (
                id uuid PRIMARY KEY,
                job_type text NOT NULL,
                target_type text NOT NULL,
                target_id uuid NOT NULL,
                status text NOT NULL DEFAULT 'Queued',
                attempt int NOT NULL DEFAULT 0,
                max_attempts int NOT NULL DEFAULT 5,
                error_message text NULL,
                next_attempt_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_ai_processing_job_pending
                ON app.ai_processing_job (next_attempt_utc, created_utc)
                WHERE status IN ('Queued', 'Failed');
        ");

        migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS trg_ai_processing_job_set_updated ON app.ai_processing_job;
            CREATE TRIGGER trg_ai_processing_job_set_updated
                BEFORE UPDATE ON app.ai_processing_job
                FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

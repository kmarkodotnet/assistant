using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260710000002_AddTaskDeadlineTsvector")]
public partial class AddTaskDeadlineTsvector : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.task ADD COLUMN IF NOT EXISTS tsv tsvector
                GENERATED ALWAYS AS (to_tsvector('hungarian_unaccent',
                    coalesce(title,'') || ' ' || coalesce(description,''))) STORED;

            CREATE INDEX IF NOT EXISTS ix_task_tsv
                ON app.task USING gin (tsv);
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE app.deadline ADD COLUMN IF NOT EXISTS tsv tsvector
                GENERATED ALWAYS AS (to_tsvector('hungarian_unaccent',
                    coalesce(title,'') || ' ' || coalesce(description,''))) STORED;

            CREATE INDEX IF NOT EXISTS ix_deadline_tsv
                ON app.deadline USING gin (tsv);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

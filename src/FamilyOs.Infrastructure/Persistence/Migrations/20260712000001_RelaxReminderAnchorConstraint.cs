using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260712000001_RelaxReminderAnchorConstraint")]
public partial class RelaxReminderAnchorConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.reminder DROP CONSTRAINT IF EXISTS chk_reminder_xor;
            ALTER TABLE app.reminder ADD CONSTRAINT chk_reminder_xor
                CHECK (NOT (task_id IS NOT NULL AND deadline_id IS NOT NULL));
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // NOTE: This rollback restores the strict XOR constraint. It will FAIL
        // if any standalone reminders (task_id IS NULL AND deadline_id IS NULL)
        // were created while the relaxed constraint was in effect. Before
        // rolling back, either delete/reassign those rows or convert them to
        // an anchored reminder (backfill task_id or deadline_id).
        migrationBuilder.Sql(@"
            ALTER TABLE app.reminder DROP CONSTRAINT IF EXISTS chk_reminder_xor;
            ALTER TABLE app.reminder ADD CONSTRAINT chk_reminder_xor
                CHECK ((task_id IS NOT NULL AND deadline_id IS NULL)
                    OR (task_id IS NULL AND deadline_id IS NOT NULL));
        ");
    }
}

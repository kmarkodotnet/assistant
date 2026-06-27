using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTaskDeadlineApprovalFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.task
                ADD COLUMN IF NOT EXISTS approved_by_user_account_id UUID NULL,
                ADD COLUMN IF NOT EXISTS approved_utc TIMESTAMPTZ NULL,
                ADD COLUMN IF NOT EXISTS completed_utc TIMESTAMPTZ NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE app.deadline
                ADD COLUMN IF NOT EXISTS approved_by_user_account_id UUID NULL,
                ADD COLUMN IF NOT EXISTS approved_utc TIMESTAMPTZ NULL;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

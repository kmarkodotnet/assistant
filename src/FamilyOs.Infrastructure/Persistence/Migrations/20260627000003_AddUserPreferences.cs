using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000003_AddUserPreferences")]
public partial class AddUserPreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.user_account
                ADD COLUMN IF NOT EXISTS email_enabled      boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS quiet_hours_start  varchar(5) NULL,
                ADD COLUMN IF NOT EXISTS quiet_hours_end    varchar(5) NULL;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.user_account
                DROP COLUMN IF EXISTS email_enabled,
                DROP COLUMN IF EXISTS quiet_hours_start,
                DROP COLUMN IF EXISTS quiet_hours_end;
        ");
    }
}

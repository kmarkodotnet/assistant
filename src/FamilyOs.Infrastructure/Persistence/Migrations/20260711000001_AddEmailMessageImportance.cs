using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260711000001_AddEmailMessageImportance")]
public partial class AddEmailMessageImportance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.email_message
                ADD COLUMN IF NOT EXISTS importance         varchar(10) NULL,
                ADD COLUMN IF NOT EXISTS category           varchar(100) NULL,
                ADD COLUMN IF NOT EXISTS has_deadline_hint  boolean NULL;

            CREATE INDEX IF NOT EXISTS ix_email_message_importance_high
                ON app.email_message (importance)
                WHERE importance = 'High';
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DROP INDEX IF EXISTS app.ix_email_message_importance_high;

            ALTER TABLE app.email_message
                DROP COLUMN IF EXISTS importance,
                DROP COLUMN IF EXISTS category,
                DROP COLUMN IF EXISTS has_deadline_hint;
        ");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260628000001_AddTagColorTopicIconSortOrder")]
public partial class AddTagColorTopicIconSortOrder : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE app.tag
            ADD COLUMN IF NOT EXISTS color TEXT NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE app.topic
            ADD COLUMN IF NOT EXISTS icon TEXT NULL,
            ADD COLUMN IF NOT EXISTS sort_order INT NOT NULL DEFAULT 0;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

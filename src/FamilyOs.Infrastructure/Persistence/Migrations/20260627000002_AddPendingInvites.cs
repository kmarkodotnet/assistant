using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000002_AddPendingInvites")]
public partial class AddPendingInvites : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE app.pending_invite (
                id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                email             varchar(320) NOT NULL,
                family_member_id  uuid NOT NULL REFERENCES app.family_member(id) ON DELETE CASCADE,
                role              varchar(50) NOT NULL,
                created_utc       timestamptz NOT NULL DEFAULT NOW(),
                CONSTRAINT ux_pending_invite_email UNIQUE (email)
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.pending_invite;");
    }
}

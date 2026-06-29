using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000016_AddNoteEntities")]
public partial class AddNoteEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.note (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                title VARCHAR(500) NOT NULL,
                body TEXT NOT NULL,
                related_family_member_id UUID NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
                created_by_user_account_id UUID NOT NULL,
                is_private BOOLEAN NOT NULL DEFAULT FALSE,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                deleted_utc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_note_user_created
                ON app.note (created_by_user_account_id, created_utc)
                WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.note_chunk (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                note_id UUID NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                content TEXT NOT NULL,
                embedding vector(768) NULL,
                embedding_model VARCHAR(200) NOT NULL DEFAULT '',
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uix_note_chunk_note_index UNIQUE (note_id, chunk_index)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.note_tag (
                note_id UUID NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
                tag_id UUID NOT NULL REFERENCES app.tag(id) ON DELETE CASCADE,
                origin app.origin NOT NULL DEFAULT 'Manual',
                is_approved BOOLEAN NOT NULL DEFAULT FALSE,
                PRIMARY KEY (note_id, tag_id)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.note_topic (
                note_id UUID NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
                topic_id UUID NOT NULL REFERENCES app.topic(id) ON DELETE CASCADE,
                PRIMARY KEY (note_id, topic_id)
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

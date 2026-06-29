using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000010_AddTagTopicEntities")]
public partial class AddTagTopicEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.tag (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                usage_count INT NOT NULL DEFAULT 1,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS uix_tag_name ON app.tag (name);
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.topic (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                slug TEXT NOT NULL,
                parent_id UUID NULL REFERENCES app.topic(id) ON DELETE RESTRICT,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS uix_topic_slug ON app.topic (slug);
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document_tag (
                document_id UUID NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                tag_id UUID NOT NULL REFERENCES app.tag(id) ON DELETE CASCADE,
                origin app.origin NOT NULL,
                is_approved BOOLEAN NOT NULL DEFAULT false,
                PRIMARY KEY (document_id, tag_id)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document_topic (
                document_id UUID NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                topic_id UUID NOT NULL REFERENCES app.topic(id) ON DELETE CASCADE,
                origin app.origin NOT NULL,
                is_approved BOOLEAN NOT NULL DEFAULT false,
                PRIMARY KEY (document_id, topic_id)
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

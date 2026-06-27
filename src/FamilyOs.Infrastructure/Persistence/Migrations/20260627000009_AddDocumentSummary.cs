using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentSummary : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document_summary (
                id UUID PRIMARY KEY,
                document_id UUID NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                content TEXT NOT NULL,
                model_name TEXT NOT NULL,
                prompt_version TEXT NOT NULL,
                is_current BOOLEAN NOT NULL DEFAULT true,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS uix_document_summary_current
                ON app.document_summary (document_id)
                WHERE is_current = true;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

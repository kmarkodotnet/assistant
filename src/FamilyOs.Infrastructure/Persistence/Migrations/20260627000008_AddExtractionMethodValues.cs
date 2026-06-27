using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddExtractionMethodValues : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add PlainText and DocxExtract to the extraction_method enum
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_enum e
                    JOIN pg_type t ON t.oid = e.enumtypid
                    JOIN pg_namespace n ON n.oid = t.typnamespace
                    WHERE n.nspname = 'app' AND t.typname = 'extraction_method' AND e.enumlabel = 'PlainText'
                ) THEN
                    ALTER TYPE app.extraction_method ADD VALUE 'PlainText';
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_enum e
                    JOIN pg_type t ON t.oid = e.enumtypid
                    JOIN pg_namespace n ON n.oid = t.typnamespace
                    WHERE n.nspname = 'app' AND t.typname = 'extraction_method' AND e.enumlabel = 'DocxExtract'
                ) THEN
                    ALTER TYPE app.extraction_method ADD VALUE 'DocxExtract';
                END IF;
            END $$;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

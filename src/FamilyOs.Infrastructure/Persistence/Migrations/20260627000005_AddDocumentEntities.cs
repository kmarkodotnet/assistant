using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000005_AddDocumentEntities")]
public partial class AddDocumentEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create required enum types if they don't exist
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'processing_status') THEN
                    CREATE TYPE app.processing_status AS ENUM ('Pending', 'Extracting', 'Analyzing', 'Done', 'Failed');
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'source_type') THEN
                    CREATE TYPE app.source_type AS ENUM ('Upload', 'Email', 'Manual');
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'origin') THEN
                    CREATE TYPE app.origin AS ENUM ('Manual', 'AiSuggested', 'AiApproved', 'ImportedEmail', 'ImportedFile');
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'extraction_method') THEN
                    CREATE TYPE app.extraction_method AS ENUM ('PdfTextLayer', 'TesseractOcr', 'ManualPaste', 'EmailBody');
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document (
                id uuid PRIMARY KEY,
                title text NOT NULL,
                original_file_name text NOT NULL,
                mime_type text NOT NULL,
                size_bytes bigint NOT NULL,
                storage_path text NOT NULL,
                sha256 text NOT NULL,
                source_type app.source_type NOT NULL,
                source_email_message_id uuid NULL,
                language text NULL,
                document_date date NULL,
                related_family_member_id uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
                is_private boolean NOT NULL DEFAULT false,
                processing_status app.processing_status NOT NULL DEFAULT 'Pending',
                origin app.origin NOT NULL,
                created_by_user_account_id uuid NOT NULL REFERENCES app.user_account(id) ON DELETE RESTRICT,
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc timestamptz NULL,
                CONSTRAINT ck_document_size CHECK (size_bytes > 0),
                CONSTRAINT ck_document_sha CHECK (char_length(sha256) = 64)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sha256 ON app.document(sha256) WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_document_source_type_created ON app.document(source_type, created_utc DESC) WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_document_family_member ON app.document(related_family_member_id) WHERE deleted_utc IS NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_document_processing_pending ON app.document(processing_status) WHERE processing_status IN ('Pending','Extracting','Analyzing','Failed');
        ");

        // Create trigram extension if not exists (required for gin_trgm_ops)
        migrationBuilder.Sql(@"
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_document_title_trgm ON app.document USING gin (title gin_trgm_ops) WHERE deleted_utc IS NULL;
        ");

        // set_updated_utc trigger function (idempotent)
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION app.set_updated_utc()
            RETURNS TRIGGER LANGUAGE plpgsql AS $$
            BEGIN
                NEW.updated_utc = (now() AT TIME ZONE 'UTC');
                RETURN NEW;
            END;
            $$;
        ");

        migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS trg_document_set_updated ON app.document;
            CREATE TRIGGER trg_document_set_updated BEFORE UPDATE ON app.document
                FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
        ");

        // document_text table
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document_text (
                id uuid PRIMARY KEY,
                document_id uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
                content text NOT NULL DEFAULT '',
                original_content text NULL,
                extraction_method app.extraction_method NOT NULL,
                ocr_confidence numeric(5,2) NULL,
                char_count int NOT NULL DEFAULT 0,
                language_detected text NULL,
                is_manually_edited boolean NOT NULL DEFAULT false,
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
            );
        ");

        migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS trg_document_text_set_updated ON app.document_text;
            CREATE TRIGGER trg_document_text_set_updated BEFORE UPDATE ON app.document_text
                FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

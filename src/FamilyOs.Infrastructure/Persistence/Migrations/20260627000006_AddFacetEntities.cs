using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFacetEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // medical_record_type enum
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'medical_record_type') THEN
                    CREATE TYPE app.medical_record_type AS ENUM (
                        'Prescription', 'LabResult', 'Imaging', 'Vaccination',
                        'DoctorNote', 'Referral', 'Discharge', 'Other'
                    );
                END IF;
            END $$;
        ");

        // financial_record_type enum
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'financial_record_type') THEN
                    CREATE TYPE app.financial_record_type AS ENUM (
                        'Invoice', 'Receipt', 'BankStatement', 'TaxDocument',
                        'Insurance', 'Subscription', 'Salary', 'Other'
                    );
                END IF;
            END $$;
        ");

        // recurrence_period enum
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
                               WHERE n.nspname = 'app' AND t.typname = 'recurrence_period') THEN
                    CREATE TYPE app.recurrence_period AS ENUM (
                        'None', 'Daily', 'Weekly', 'Monthly', 'Quarterly', 'Yearly'
                    );
                END IF;
            END $$;
        ");

        // warranty table
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.warranty (
                id uuid PRIMARY KEY,
                document_id uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
                product_name text NOT NULL,
                brand text NULL,
                model text NULL,
                serial_number text NULL,
                purchase_date date NULL,
                purchase_price numeric(18,2) NULL,
                currency varchar(3) NULL,
                warranty_months int NULL,
                warranty_end_date date NULL,
                seller text NULL,
                related_family_member_id uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc timestamptz NULL
            );
        ");

        // medical_record table
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.medical_record (
                id uuid PRIMARY KEY,
                document_id uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                family_member_id uuid NOT NULL REFERENCES app.family_member(id) ON DELETE RESTRICT,
                record_type app.medical_record_type NOT NULL,
                record_date date NOT NULL,
                provider text NULL,
                title text NOT NULL,
                structured_json jsonb NULL,
                is_private boolean NOT NULL DEFAULT true,
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc timestamptz NULL
            );
        ");

        // financial_record table
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.financial_record (
                id uuid PRIMARY KEY,
                document_id uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                record_type app.financial_record_type NOT NULL,
                vendor text NULL,
                amount numeric(18,2) NULL,
                currency varchar(3) NULL,
                issue_date date NULL,
                due_date date NULL,
                paid_date date NULL,
                is_paid boolean NOT NULL DEFAULT false,
                recurrence_period app.recurrence_period NOT NULL DEFAULT 'None',
                related_family_member_id uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
                created_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc timestamptz NULL
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

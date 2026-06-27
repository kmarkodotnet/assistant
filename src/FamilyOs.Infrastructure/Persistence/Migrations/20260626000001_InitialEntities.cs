using Microsoft.EntityFrameworkCore.Migrations;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PostgreSQL enums
        migrationBuilder.Sql("CREATE TYPE app.user_role AS ENUM ('Admin','Adult','Child');");
        migrationBuilder.Sql("CREATE TYPE app.relation AS ENUM ('Self','Spouse','Child','Parent','Other');");
        migrationBuilder.Sql("CREATE TYPE app.source_type AS ENUM ('Upload','Email','Manual');");
        migrationBuilder.Sql("CREATE TYPE app.processing_status AS ENUM ('Pending','Extracting','Analyzing','Done','Failed');");
        migrationBuilder.Sql("CREATE TYPE app.origin AS ENUM ('Manual','AiSuggested','AiApproved','ImportedEmail','ImportedFile');");
        migrationBuilder.Sql("CREATE TYPE app.extraction_method AS ENUM ('PdfTextLayer','TesseractOcr','ManualPaste','EmailBody');");
        migrationBuilder.Sql("CREATE TYPE app.task_status AS ENUM ('Suggested','Open','InProgress','Done','Cancelled');");
        migrationBuilder.Sql("CREATE TYPE app.priority AS ENUM ('Low','Normal','High');");
        migrationBuilder.Sql("CREATE TYPE app.deadline_status AS ENUM ('Upcoming','Due','Passed','Resolved','Dismissed');");
        migrationBuilder.Sql("CREATE TYPE app.deadline_category AS ENUM ('Insurance','Invoice','Inspection','School','Medical','Subscription','Personal','Other');");
        migrationBuilder.Sql("CREATE TYPE app.notification_channel AS ENUM ('InApp','Email');");
        migrationBuilder.Sql("CREATE TYPE app.reminder_status AS ENUM ('Scheduled','Fired','Acknowledged','Skipped','Failed','Cancelled');");
        migrationBuilder.Sql("CREATE TYPE app.source_kind AS ENUM ('Upload','GmailAccount','FileWatch');");
        migrationBuilder.Sql("CREATE TYPE app.ingest_status AS ENUM ('Pending','Processed','Skipped','Failed');");
        migrationBuilder.Sql("CREATE TYPE app.ai_job_type AS ENUM ('ExtractText','DetectLanguage','Summarize','ExtractEntities','ExtractDeadlines','ExtractTasks','Classify','Embed');");
        migrationBuilder.Sql("CREATE TYPE app.job_target_type AS ENUM ('Document','Note','EmailMessage');");
        migrationBuilder.Sql("CREATE TYPE app.job_status AS ENUM ('Queued','Running','Completed','Failed','Cancelled');");
        migrationBuilder.Sql("CREATE TYPE app.audit_action AS ENUM ('Create','Update','Delete','Login','LoginFailed','Approve','Reject','AiCall','FileAccess','PermissionChange','ExternalApiCall');");
        migrationBuilder.Sql("CREATE TYPE app.medical_record_type AS ENUM ('LabResult','Prescription','Vaccination','Imaging','Diagnosis','AppointmentNote','Other');");
        migrationBuilder.Sql("CREATE TYPE app.financial_record_type AS ENUM ('Invoice','Receipt','Insurance','Subscription','BankStatement','Contract','Other');");
        migrationBuilder.Sql("CREATE TYPE app.recurrence_period AS ENUM ('None','Monthly','Quarterly','Yearly');");

        // family_member table
        migrationBuilder.Sql(@"
            CREATE TABLE app.family_member (
                id                          uuid PRIMARY KEY,
                display_name                text NOT NULL,
                full_name                   text NULL,
                relation                    app.relation NOT NULL,
                birth_date                  date NULL,
                has_user_account            boolean NOT NULL DEFAULT false,
                notes                       text NULL,
                created_utc                 timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc                 timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc                 timestamptz NULL,
                CONSTRAINT ck_family_member_display_name_len CHECK (char_length(display_name) BETWEEN 1 AND 100),
                CONSTRAINT ck_family_member_full_name_len    CHECK (full_name IS NULL OR char_length(full_name) <= 200),
                CONSTRAINT ck_family_member_birth_date       CHECK (birth_date IS NULL OR birth_date <= current_date)
            );
            CREATE INDEX ix_family_member_relation ON app.family_member(relation) WHERE deleted_utc IS NULL;
            CREATE INDEX ix_family_member_has_user_account ON app.family_member(has_user_account) WHERE deleted_utc IS NULL;
            CREATE TRIGGER trg_family_member_set_updated BEFORE UPDATE ON app.family_member
                FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
        ");

        // user_account table
        migrationBuilder.Sql(@"
            CREATE TABLE app.user_account (
                id                          uuid PRIMARY KEY,
                family_member_id            uuid NOT NULL UNIQUE REFERENCES app.family_member(id) ON DELETE RESTRICT,
                google_subject              text NOT NULL,
                email                       text NOT NULL,
                display_name                text NOT NULL,
                role                        app.user_role NOT NULL,
                last_login_utc              timestamptz NULL,
                is_active                   boolean NOT NULL DEFAULT true,
                created_utc                 timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                updated_utc                 timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                deleted_utc                 timestamptz NULL,
                CONSTRAINT ck_user_account_email_lower CHECK (email = lower(email))
            );
            CREATE UNIQUE INDEX ux_user_account_google_subject ON app.user_account(google_subject) WHERE deleted_utc IS NULL;
            CREATE UNIQUE INDEX ux_user_account_email ON app.user_account(email) WHERE deleted_utc IS NULL;
            CREATE INDEX ix_user_account_is_active ON app.user_account(is_active) WHERE deleted_utc IS NULL;
            CREATE TRIGGER trg_user_account_set_updated BEFORE UPDATE ON app.user_account
                FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.user_account;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS app.family_member;");

        migrationBuilder.Sql("DROP TYPE IF EXISTS app.recurrence_period;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.financial_record_type;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.medical_record_type;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.audit_action;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.job_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.job_target_type;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.ai_job_type;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.ingest_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.source_kind;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.reminder_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.notification_channel;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.deadline_category;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.deadline_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.priority;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.task_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.extraction_method;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.origin;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.processing_status;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.source_type;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.relation;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS app.user_role;");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000015_AddReminderAndNotificationFeed")]
public partial class AddReminderAndNotificationFeed : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.reminder (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                task_id UUID NULL REFERENCES app.task(id) ON DELETE CASCADE,
                deadline_id UUID NULL REFERENCES app.deadline(id) ON DELETE CASCADE,
                target_user_account_id UUID NOT NULL,
                channel VARCHAR(50) NOT NULL DEFAULT 'InApp',
                status VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
                trigger_utc TIMESTAMPTZ NOT NULL,
                fired_utc TIMESTAMPTZ NULL,
                acknowledged_utc TIMESTAMPTZ NULL,
                rrule_expression VARCHAR(500) NULL,
                escalation_level INT NOT NULL DEFAULT 0,
                snooze_note VARCHAR(500) NULL,
                created_by_user_account_id UUID NOT NULL,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                deleted_utc TIMESTAMPTZ NULL,
                CONSTRAINT chk_reminder_xor CHECK (
                    (task_id IS NOT NULL AND deadline_id IS NULL) OR
                    (task_id IS NULL AND deadline_id IS NOT NULL)
                )
            );

            CREATE INDEX IF NOT EXISTS ix_reminder_user_trigger
                ON app.reminder (target_user_account_id, trigger_utc)
                WHERE status = 'Scheduled';
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.notification_feed (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                target_user_account_id UUID NOT NULL,
                type VARCHAR(100) NOT NULL,
                title VARCHAR(500) NOT NULL,
                body VARCHAR(2000) NULL,
                action_url VARCHAR(500) NULL,
                idempotency_key VARCHAR(256) NULL,
                read_utc TIMESTAMPTZ NULL,
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uix_notification_feed_idempotency_key
                ON app.notification_feed (idempotency_key)
                WHERE idempotency_key IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_notification_feed_user_created
                ON app.notification_feed (target_user_account_id, created_utc DESC)
                WHERE read_utc IS NULL;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

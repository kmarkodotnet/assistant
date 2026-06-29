using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FamilyOsDbContext))]
[Migration("00000000000000_InitialSetup")]
public partial class InitialSetup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Extensions
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gin;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        // Schema
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS app;");
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS hangfire;");

        // Roles (IF NOT EXISTS guard)
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'family_app') THEN
                    CREATE ROLE family_app LOGIN PASSWORD 'changeme_app';
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'family_migrator') THEN
                    CREATE ROLE family_migrator LOGIN PASSWORD 'changeme_migrator';
                END IF;
            END $$;
        ");

        // Full-text search configuration
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_ts_config WHERE cfgname = 'hungarian_unaccent'
                ) THEN
                    CREATE TEXT SEARCH CONFIGURATION hungarian_unaccent ( COPY = hungarian );
                    ALTER TEXT SEARCH CONFIGURATION hungarian_unaccent
                        ALTER MAPPING FOR hword, hword_part, word
                        WITH unaccent, hungarian_stem;
                END IF;
            END $$;
        ");

        // set_updated_utc trigger function
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION app.set_updated_utc()
            RETURNS TRIGGER LANGUAGE plpgsql AS $$
            BEGIN
                NEW.updated_utc := now() AT TIME ZONE 'UTC';
                RETURN NEW;
            END $$;
        ");

        // audit_log_immutable trigger function
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION app.audit_log_immutable()
            RETURNS TRIGGER LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'audit_log immutable';
            END $$;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.audit_log_immutable();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.set_updated_utc();");
        // Note: Extensions and schemas are not dropped on rollback (too destructive)
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260710000001_AddTaskDeadlineChunks")]
public partial class AddTaskDeadlineChunks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.task_chunk (
                id UUID PRIMARY KEY,
                task_id UUID NOT NULL REFERENCES app.task(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                content TEXT NOT NULL,
                embedding vector(768) NULL,
                embedding_model VARCHAR(200) NOT NULL DEFAULT '',
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uix_task_chunk_task_index UNIQUE (task_id, chunk_index)
            );

            CREATE INDEX IF NOT EXISTS ix_task_chunk_hnsw
                ON app.task_chunk
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.deadline_chunk (
                id UUID PRIMARY KEY,
                deadline_id UUID NOT NULL REFERENCES app.deadline(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                content TEXT NOT NULL,
                embedding vector(768) NULL,
                embedding_model VARCHAR(200) NOT NULL DEFAULT '',
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uix_deadline_chunk_deadline_index UNIQUE (deadline_id, chunk_index)
            );

            CREATE INDEX IF NOT EXISTS ix_deadline_chunk_hnsw
                ON app.deadline_chunk
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(FamilyOsDbContext))]
[Migration("20260627000012_AddDocumentChunk")]
public partial class AddDocumentChunk : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS app.document_chunk (
                id UUID PRIMARY KEY,
                document_id UUID NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                content TEXT NOT NULL,
                embedding vector(768) NULL,
                embedding_model TEXT NOT NULL DEFAULT '',
                created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE (document_id, chunk_index)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_document_chunk_hnsw
                ON app.document_chunk
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported.");
    }
}

#!/bin/sh
# Napi pg_dump + age titkosítás + manifest
# Ezt a backup Docker service futtatja cron-ból napi egyszer (02:00)

set -e

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/data/backups/db"
MANIFEST="/data/backups/manifest.txt"
RETAIN_DAYS="${BACKUP_RETAIN_DAYS:-30}"

mkdir -p "$BACKUP_DIR"

DUMP_FILE="$BACKUP_DIR/family-os-$TIMESTAMP.dump"

echo "[$TIMESTAMP] Starting backup..."

# pg_dump
PGPASSWORD="$POSTGRES_PASSWORD" pg_dump \
  -h "$POSTGRES_HOST" \
  -U "$POSTGRES_USER" \
  -d "$POSTGRES_DB" \
  -Fc \
  -f "$DUMP_FILE"

# Encrypt with age if public key is set
if [ -n "$BACKUP_AGE_PUBKEY" ]; then
  age -r "$BACKUP_AGE_PUBKEY" -o "$DUMP_FILE.age" "$DUMP_FILE"
  rm -f "$DUMP_FILE"
  FINAL_FILE="$DUMP_FILE.age"
else
  FINAL_FILE="$DUMP_FILE"
fi

# SHA-256 hash to manifest (append-only)
HASH=$(sha256sum "$FINAL_FILE" | awk '{print $1}')
echo "$TIMESTAMP $HASH $(basename $FINAL_FILE)" >> "$MANIFEST"

echo "[$TIMESTAMP] Backup complete: $FINAL_FILE (sha256: $HASH)"

# Cleanup old backups
find "$BACKUP_DIR" -name "*.dump*" -mtime +"$RETAIN_DAYS" -delete
echo "[$TIMESTAMP] Cleaned up backups older than $RETAIN_DAYS days."

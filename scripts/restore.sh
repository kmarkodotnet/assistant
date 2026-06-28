#!/bin/sh
# Backup restore script
# Használat: ./scripts/restore.sh <backup_file> [--verify-only]
# Ellenőrzi a hash-t a manifest-ből, majd pg_restore-t futtat

set -e

BACKUP_FILE="$1"
MANIFEST="/data/backups/manifest.txt"

if [ -z "$BACKUP_FILE" ]; then
  echo "Használat: $0 <backup_file> [--verify-only]"
  echo "Elérhető backup-ok:"
  ls /data/backups/db/ 2>/dev/null || echo "  Nincs backup"
  exit 1
fi

VERIFY_ONLY="${2:-}"

echo "=== Family OS Backup Restore ==="
echo "Fájl: $BACKUP_FILE"
echo ""

# Hash ellenőrzés a manifest alapján
BASENAME=$(basename "$BACKUP_FILE")
EXPECTED_HASH=$(grep "$BASENAME" "$MANIFEST" | tail -1 | awk '{print $2}')

if [ -z "$EXPECTED_HASH" ]; then
  echo "HIBA: A fájl nem szerepel a manifest-ben: $BASENAME"
  echo "Folytatja? (y/N)"
  read -r CONFIRM
  if [ "$CONFIRM" != "y" ]; then exit 1; fi
else
  ACTUAL_HASH=$(sha256sum "$BACKUP_FILE" | awk '{print $1}')
  if [ "$EXPECTED_HASH" != "$ACTUAL_HASH" ]; then
    echo "HIBA: Hash eltérés! Várható: $EXPECTED_HASH, Tényleges: $ACTUAL_HASH"
    exit 1
  fi
  echo "Hash ellenőrzés OK: $ACTUAL_HASH"
fi

if [ "$VERIFY_ONLY" = "--verify-only" ]; then
  echo "Csak ellenőrzés — restore nem fut."
  exit 0
fi

# Decrypt if .age extension
RESTORE_FILE="$BACKUP_FILE"
if echo "$BACKUP_FILE" | grep -q "\.age$"; then
  RESTORE_FILE="${BACKUP_FILE%.age}"
  echo "Titkosított fájl feloldása..."
  age --decrypt -o "$RESTORE_FILE" "$BACKUP_FILE"
fi

echo ""
echo "FIGYELEM: Ez felülírja a meglévő adatbázist!"
echo "Folytatja? (y/N)"
read -r CONFIRM
if [ "$CONFIRM" != "y" ]; then
  echo "Megszakítva."
  exit 0
fi

echo "Restore futtatása..."
PGPASSWORD="$POSTGRES_PASSWORD" pg_restore \
  -h "${POSTGRES_HOST:-postgres}" \
  -U "${POSTGRES_USER:-family_migrator}" \
  -d "${POSTGRES_DB:-family_os}" \
  --clean --if-exists \
  "$RESTORE_FILE"

# Cleanup temp decrypt file
if [ "$RESTORE_FILE" != "$BACKUP_FILE" ]; then
  rm -f "$RESTORE_FILE"
fi

echo ""
echo "Restore kész! Ellenőrizd a rendszert: make up && curl http://localhost:8080/healthz/ready"

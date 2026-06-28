#!/bin/sh
# Belső CA + önaláírt tanúsítvány generálása Family OS-hez
# Használat: ./scripts/init-tls-ca.sh [hostname]
# Eredmény: docker/nginx/certs/family-os.crt és family-os.key

set -e

HOSTNAME="${1:-family-os.lan}"
CERTS_DIR="docker/nginx/certs"
mkdir -p "$CERTS_DIR"

echo "Generating CA key and certificate..."
openssl genrsa -out "$CERTS_DIR/ca.key" 4096
openssl req -new -x509 -days 3650 -key "$CERTS_DIR/ca.key" \
  -out "$CERTS_DIR/ca.crt" \
  -subj "/C=HU/O=Family OS/CN=Family OS Internal CA"

echo "Generating server key and CSR..."
openssl genrsa -out "$CERTS_DIR/family-os.key" 2048
openssl req -new -key "$CERTS_DIR/family-os.key" \
  -out "$CERTS_DIR/family-os.csr" \
  -subj "/C=HU/O=Family OS/CN=$HOSTNAME"

echo "Signing server certificate with CA..."
cat > "$CERTS_DIR/ext.cnf" << EOF
[v3_req]
subjectAltName = DNS:$HOSTNAME,DNS:localhost,IP:127.0.0.1
EOF

openssl x509 -req -days 3650 \
  -in "$CERTS_DIR/family-os.csr" \
  -CA "$CERTS_DIR/ca.crt" \
  -CAkey "$CERTS_DIR/ca.key" \
  -CAcreateserial \
  -out "$CERTS_DIR/family-os.crt" \
  -extfile "$CERTS_DIR/ext.cnf" \
  -extensions v3_req

rm -f "$CERTS_DIR/family-os.csr" "$CERTS_DIR/ext.cnf"

echo ""
echo "Done! Files created:"
echo "  $CERTS_DIR/ca.crt       — telepítsd minden háztartási eszközre"
echo "  $CERTS_DIR/family-os.crt — nginx szerver tanúsítvány"
echo "  $CERTS_DIR/family-os.key — nginx privát kulcs (tartsd titokban!)"
echo ""
echo "CA telepítés útmutató: docs/DELIVERY.md #TLS"

#!/usr/bin/env bash
set -euo pipefail

TARGET_DIR="${HOME}/.aspnet/https"
CONF_FILE="docker/certs/papermail-openssl.cnf"
CA_KEY="papermail-local-ca.key"
CA_CERT="papermail-local-ca.crt"
SERVER_KEY="papermail.local.key"
SERVER_CSR="papermail.local.csr"
SERVER_CERT="papermail.local.crt"
SERVER_PFX="papermail.local.pfx"
PFX_PASSWORD="${CERT_PASSWORD:-P@ssw0rd}"

echo "==> Generating wildcard certificate for papermail.local and *.papermail.local"
echo "==> Output directory: ${TARGET_DIR}"

mkdir -p "${TARGET_DIR}"
chmod 700 "${TARGET_DIR}"

if [[ ! -f "${CONF_FILE}" ]]; then
  echo "OpenSSL config not found at ${CONF_FILE}. Run from repo root." >&2
  exit 1
fi

pushd "${TARGET_DIR}" >/dev/null

if [[ ! -f ${CA_KEY} ]]; then
  echo "==> Creating root CA key and certificate"
  openssl genrsa -out ${CA_KEY} 4096
  openssl req -x509 -new -nodes -key ${CA_KEY} -sha256 -days 3650 -out ${CA_CERT} \
    -subj "/C=US/ST=Local/L=Local/O=papermail/OU=Dev CA/CN=papermail Local Dev CA"
else
  echo "==> Root CA already exists, skipping"
fi

echo "==> Creating server key and CSR"
openssl genrsa -out ${SERVER_KEY} 4096
openssl req -new -key ${SERVER_KEY} -out ${SERVER_CSR} -config "${OLDPWD}/${CONF_FILE}"

echo "==> Signing server certificate with CA"
openssl x509 -req -in ${SERVER_CSR} -CA ${CA_CERT} -CAkey ${CA_KEY} -CAcreateserial -out ${SERVER_CERT} -days 825 -sha256 \
  -extensions v3_server -extfile "${OLDPWD}/${CONF_FILE}"

echo "==> Creating PKCS#12 (PFX) bundle"
openssl pkcs12 -export -out ${SERVER_PFX} -inkey ${SERVER_KEY} -in ${SERVER_CERT} -certfile ${CA_CERT} -passout pass:"${PFX_PASSWORD}"

echo "==> Generated files:"
ls -1 ${CA_CERT} ${SERVER_CERT} ${SERVER_KEY} ${SERVER_PFX}

echo "==> To trust the CA inside containers, mount ${TARGET_DIR}/${CA_CERT} to /usr/local/share/ca-certificates/ and run update-ca-certificates"
echo "==> Done"

popd >/dev/null

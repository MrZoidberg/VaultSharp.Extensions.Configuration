#! /bin/sh

# Setup VAULT_ADDR and VAULT_TOKEN
export VAULT_ADDR=http://localhost:8200
export VAULT_TOKEN=root

vault secrets enable -version=2 kv
vault auth enable approle
vault policy write test-policy -<<EOF
path "secret/*" {
  capabilities = [ "read" ]
}
EOF
vault write auth/approle/role/test-role token_policies="test-policy" \
    token_ttl=1h token_max_ttl=4h

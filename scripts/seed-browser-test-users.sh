#!/usr/bin/env bash
set -euo pipefail

echo "=== Seeding test users via API & DB ==="

# 1. Register e2e_user
curl -s -X POST http://127.0.0.1:5058/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"e2e_user@example.com","userName":"e2e_user","displayName":"E2E Tester","password":"OldPassword123!"}' || true

# 2. Confirm e2e_user in database
docker compose exec -T postgres psql -U culinary -d culinary_blog -c 'UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "Email" = '\''e2e_user@example.com'\'';'

# 3. Register e2e_unconfirmed
curl -s -X POST http://127.0.0.1:5058/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"e2e_unconfirmed@example.com","userName":"e2e_unconfirmed","displayName":"E2E Unconfirmed","password":"OldPassword123!"}' || true

# 4. Insert e2e_google (no password)
docker compose exec -T postgres psql -U culinary -d culinary_blog -c '
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "DisplayName", "IsActive", "CreatedAt", "AccessFailedCount", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled"
) VALUES (
    '\''e2e-google-user-id'\'', '\''e2e_google'\'', '\''E2E_GOOGLE'\'', '\''e2e_google@example.com'\'', '\''E2E_GOOGLE@EXAMPLE.COM'\'',
    true, NULL, gen_random_uuid()::text, gen_random_uuid()::text,
    '\''E2E Google User'\'', true, NOW(), 0, false, false, false
) ON CONFLICT ("Id") DO NOTHING;
'

# 5. Clear Mailpit messages
curl -s -X DELETE http://127.0.0.1:8025/api/v1/messages || true

echo "Test users ready!"

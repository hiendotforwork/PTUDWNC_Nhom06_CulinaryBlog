#!/usr/bin/env bash
set -euo pipefail

TARGET_EMAIL="${1:-e2e_user@example.com}"

MSG_ID=$(curl -s "http://localhost:8025/api/v1/messages" | jq -r --arg email "$TARGET_EMAIL" '.messages[]? | select(.To[].Address == $email) | .ID' | head -n 1)

if [ -z "$MSG_ID" ] || [ "$MSG_ID" = "null" ]; then
  # Fallback in case messages array structure is root array
  MSG_ID=$(curl -s "http://localhost:8025/api/v1/messages" | jq -r --arg email "$TARGET_EMAIL" '.[]? | select(.To[].Address == $email) | .ID' | head -n 1)
fi

if [ -z "$MSG_ID" ] || [ "$MSG_ID" = "null" ]; then
  echo "ERROR: No email found in Mailpit for $TARGET_EMAIL" >&2
  exit 1
fi

BODY=$(curl -s "http://localhost:8025/api/v1/message/$MSG_ID" | jq -r '.Text // .HTML')

LINK=$(echo "$BODY" | grep -o 'http://localhost:3001/reset-password#[^"'"'"' <>]*' | head -n 1)

if [ -z "$LINK" ]; then
  echo "ERROR: Reset link not found in message body: $BODY" >&2
  exit 2
fi

echo "$LINK"

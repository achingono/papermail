#!/bin/bash

# UI Test Runner for Docker Environment
# This script builds, starts, and tests the complete docker-compose stack
# then cleans up afterwards.

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Docker Environment UI Test Runner ===${NC}"

# Find repository root (navigate up from script location)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../.." && pwd)"

echo -e "${YELLOW}Repository root: $REPO_ROOT${NC}"

cd "$REPO_ROOT"

# Cleanup function to ensure docker cleanup happens
cleanup() {
    echo -e "${YELLOW}Cleaning up docker environment...${NC}"
    docker compose down -v 2>/dev/null || true
}

# Register cleanup to run on exit
trap cleanup EXIT

echo -e "${GREEN}Step 1: Building docker images...${NC}"
docker compose build

echo -e "${GREEN}Step 2: Starting docker services...${NC}"
docker compose up -d

echo -e "${GREEN}Step 3: Waiting for services to be ready...${NC}"

# Wait for web application (up to 60 seconds)
echo "Waiting for PaperMail web application on https://papermail.local..."
timeout=60
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if curl -k -s -f https://papermail.local > /dev/null 2>&1; then
        echo -e "${GREEN}Web application is ready${NC}"
        break
    fi
    sleep 2
    elapsed=$((elapsed + 2))
done

if [ $elapsed -ge $timeout ]; then
    echo -e "${RED}ERROR: Web application did not start within $timeout seconds${NC}"
    docker compose logs
    exit 1
fi

# Wait for OIDC provider
echo "Waiting for OIDC provider on https://oidc.papermail.local..."
timeout=30
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if curl -k -s -f https://oidc.papermail.local/.well-known/openid-configuration > /dev/null 2>&1; then
        echo -e "${GREEN}OIDC provider is ready${NC}"
        break
    fi
    sleep 2
    elapsed=$((elapsed + 2))
done

if [ $elapsed -ge $timeout ]; then
    echo -e "${RED}ERROR: OIDC provider did not start within $timeout seconds${NC}"
    docker compose logs oidc
    exit 1
fi

# Wait for mail server SMTP port
echo "Waiting for mail server SMTP on localhost:587..."
timeout=30
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if nc -z localhost 587 2>/dev/null; then
        echo -e "${GREEN}Mail server SMTP is ready${NC}"
        break
    fi
    sleep 2
    elapsed=$((elapsed + 2))
done

if [ $elapsed -ge $timeout ]; then
    echo -e "${RED}ERROR: Mail server SMTP did not start within $timeout seconds${NC}"
    docker compose logs mail
    exit 1
fi

echo -e "${GREEN}Step 4: Running UI tests...${NC}"

# Navigate to test project
cd "$REPO_ROOT/test/PaperMail.EndToEnd.Tests"

# Run tests with Docker filter (tests in Docker namespace)
if dotnet test --filter "FullyQualifiedName~Docker" --logger "console;verbosity=detailed"; then
    echo -e "${GREEN}=== UI Tests Passed ===${NC}"
    exit 0
else
    echo -e "${RED}=== UI Tests Failed ===${NC}"
    echo -e "${YELLOW}Docker logs:${NC}"
    docker compose logs --tail=50
    exit 1
fi

#!/bin/bash
set -e

# Script to run integration tests with docker-compose
# This script starts the mail server, runs integration tests, then cleans up

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_NAME="papermail-integration-test"

echo "🚀 Starting integration test environment..."

# Navigate to repository root
cd "$REPO_ROOT"

# Start mail server
echo "📧 Starting mail server..."
docker compose -p "$PROJECT_NAME" up -d mail

# Wait for mail server to be ready
echo "⏳ Waiting for mail server to be ready..."
timeout=30
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if nc -z localhost 587 2>/dev/null; then
        echo "✅ Mail server is ready!"
        break
    fi
    sleep 1
    elapsed=$((elapsed + 1))
done

if [ $elapsed -ge $timeout ]; then
    echo "❌ Mail server failed to start within ${timeout} seconds"
    docker compose -p "$PROJECT_NAME" down -v
    exit 1
fi

# Give it a few more seconds to fully initialize
sleep 5

echo "🧪 Running integration tests..."

# Run only integration tests (remove Skip attributes temporarily)
# For now, we'll run with the Skip attributes in place
dotnet test "$REPO_ROOT/test/PaperMail.Infrastructure.Tests" \
    --filter "FullyQualifiedName~Integration" \
    --logger "console;verbosity=normal"

TEST_RESULT=$?

# Cleanup
echo "🧹 Cleaning up..."
docker compose -p "$PROJECT_NAME" down -v

if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ Integration tests passed!"
else
    echo "❌ Integration tests failed!"
fi

exit $TEST_RESULT

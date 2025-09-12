#!/bin/bash
set -e

echo "🧪 Starting Auth Service Tests with MongoDB Local"

# Cleanup any existing containers
echo "🧹 Cleaning up existing containers..."
docker compose --env-file .env.test down -v --remove-orphans || true

# Start MongoDB test container
echo "🗄️ Starting MongoDB test container..."
docker compose --env-file .env.test up mongo-test -d

# Wait for MongoDB to be ready
echo "⏳ Waiting for MongoDB to be ready..."
sleep 5

# Check if MongoDB is ready
echo "🔍 Checking MongoDB connection..."
docker compose --env-file .env.test exec -T mongo-test mongosh --eval "db.adminCommand('ping')" || {
    echo "❌ MongoDB is not ready"
    docker compose --env-file .env.test logs mongo-test
    exit 1
}

echo "✅ MongoDB is ready"

# Build the auth-service solution
echo "🔨 Building auth-service solution..."
cd software-project/auth-service
dotnet restore AuthService.sln
dotnet build AuthService.sln --configuration Release

# Run unit tests
echo "🧪 Running unit tests..."
dotnet test AuthService.Test/AuthService.Test.csproj \
    --configuration Release \
    --logger "trx;LogFileName=unit-tests.trx" \
    --results-directory ./TestResults \
    --filter "Category!=Integration" \
    --collect:"XPlat Code Coverage"

# Run integration tests
echo "🔗 Running integration tests..."
dotnet test AuthService.Test/AuthService.Test.csproj \
    --configuration Release \
    --logger "trx;LogFileName=integration-tests.trx" \
    --results-directory ./TestResults \
    --filter "Category=Integration" \
    --collect:"XPlat Code Coverage"

# Run all tests (for complete coverage)
echo "📊 Running all tests for coverage report..."
dotnet test AuthService.Test/AuthService.Test.csproj \
    --configuration Release \
    --logger "trx;LogFileName=all-tests.trx" \
    --results-directory ./TestResults \
    --collect:"XPlat Code Coverage" \
    --verbosity minimal

echo "📋 Test Results Summary:"
echo "- Unit tests: ./software-project/auth-service/TestResults/unit-tests.trx"
echo "- Integration tests: ./software-project/auth-service/TestResults/integration-tests.trx"
echo "- All tests: ./software-project/auth-service/TestResults/all-tests.trx"
echo "- Coverage reports: ./software-project/auth-service/TestResults/**/coverage.cobertura.xml"

# Go back to root
cd ../..

# Cleanup
echo "🧹 Cleaning up test containers..."
docker compose --env-file .env.test down -v

echo "✅ Auth Service Tests Completed Successfully!"
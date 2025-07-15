# Script to run integration tests with Docker containers for SQL Server and PostgreSQL

# Start Docker containers
Write-Host "Starting Docker containers for SQL Server and PostgreSQL..." -ForegroundColor Green
docker-compose up -d

# Wait for containers to be ready
Write-Host "Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Install required packages
Write-Host "Installing required NuGet packages..." -ForegroundColor Green
./add-packages.ps1

# Run integration tests
Write-Host "Running integration tests..." -ForegroundColor Green
dotnet test DapperOrmCore.Tests/DapperOrmCore.Tests.csproj --filter "FullyQualifiedName~RealDatabasePaginationTests"

# Clean up
Write-Host "Cleaning up Docker containers..." -ForegroundColor Green
docker-compose down
param(
    [switch]$SkipTests,
    [switch]$skiptest,
    [switch]$nonDestructive
)
$ErrorActionPreference = "Stop"

dotnet build
if ($LASTEXITCODE -ne 0)
{
    Write-Host "==> Build failed."
    exit $LASTEXITCODE
}
if ($nonDestructive)
{
    Write-Host "==> Skipping volume removal due to non-destructive flag."
    docker compose -f .\docker-compose.yml down
}
else
{
    Write-Host "==> Bringing down compose stack (remove volumes)..."

    docker compose -f .\docker-compose.yml down -v
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "==> Failed to bring down compose stack."
        exit $LASTEXITCODE
    }
}
Write-Host "==> Bringing up compose stack (rebuild images)..."
docker compose -f docker-compose.yml up --build -d
if ($LASTEXITCODE -ne 0)
{
    Write-Host "==> Failed to bring up compose stack."
    exit $LASTEXITCODE
}
Start-Process "http://localhost:8080/swagger/"
Write-Host "==> Done."

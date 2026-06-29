param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $PSScriptRoot "docker-compose.yml"
$environmentFile = Join-Path $PSScriptRoot ".env"

Set-Location $repositoryRoot

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing deploy/.env. Copy deploy/.env.example to deploy/.env and configure the required values."
}

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Desktop is not running."
}

$portOwners = @(docker ps --filter "publish=5000" --format "{{.Names}}")
$foreignOwner = $portOwners | Where-Object { $_ -ne "deploy-api-gateway-1" }
if ($foreignOwner) {
    throw "Port 5000 is already used by container: $($foreignOwner -join ', '). Stop that container before starting FPTU PGS."
}

$arguments = @("compose", "-f", $composeFile, "up", "-d")
if (-not $SkipBuild) {
    $arguments += "--build"
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose failed to start the system."
}

$healthUrl = "http://localhost:5000/health"
$swaggerUrl = "http://localhost:5000/swagger/index.html"
$deadline = (Get-Date).AddMinutes(3)

Write-Host "Waiting for API Gateway and services to become ready..."
while ((Get-Date) -lt $deadline) {
    try {
        $health = Invoke-WebRequest -UseBasicParsing -Uri $healthUrl -TimeoutSec 5
        $swagger = Invoke-WebRequest -UseBasicParsing -Uri $swaggerUrl -TimeoutSec 5
        if ($health.StatusCode -eq 200 -and $swagger.StatusCode -eq 200) {
            Write-Host "FPTU PGS is ready." -ForegroundColor Green
            Write-Host "Swagger: $swaggerUrl"
            Write-Host "Gateway: http://localhost:5000"
            exit 0
        }
    }
    catch {
        Start-Sleep -Seconds 3
    }
}

docker compose -f $composeFile ps
docker compose -f $composeFile logs --tail=80 api-gateway identity-api exam-api
throw "The system did not become ready within 3 minutes. Review the container logs above."

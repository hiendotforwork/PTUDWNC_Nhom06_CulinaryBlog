# Run from the repository root. Uses only the isolated culinary-jobs-tests stack.
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repoPath
$resultsPath = Join-Path $repoPath 'artifacts/test-results/jobs-observability'
New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
$dotnetExe = Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet/dotnet.exe'
if (-not (Test-Path $dotnetExe)) { $dotnetExe = (Get-Command dotnet.exe).Source }
& $dotnetExe publish src/CulinaryBlog.API/CulinaryBlog.API.csproj -c Release -o artifacts/jobs-api
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
& cmd.exe /d /c 'docker compose --progress plain -f compose.jobs-tests.yml up -d --build > artifacts\jobs-stack-build.log 2>&1'
if ($LASTEXITCODE -ne 0) { throw 'Docker startup failed; see artifacts/jobs-stack-build.log' }
Add-Type -AssemblyName System.Net.Http
$http = [System.Net.Http.HttpClient]::new()
$http.Timeout = [TimeSpan]::FromSeconds(8)
function Status([string]$path) {
    $response = $http.GetAsync('http://localhost:5059' + $path).GetAwaiter().GetResult()
    try { return [int]$response.StatusCode } finally { $response.Dispose() }
}
function Ready {
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try { if ((Status '/health') -eq 200) { return } } catch { }
        Start-Sleep -Seconds 2
    }
    throw 'Health checks did not recover.'
}
Ready
$env:RUN_JOB_OBS_TESTS = '1'
$env:JOB_OBS_DB = 'Host=127.0.0.1;Port=55433;Database=culinary_jobs_tests;Username=culinary;Password=culinary_jobs_test_password;SSL Mode=Disable;GSS Encryption Mode=Disable'
$env:RUN_MINIO_TESTS = '1'
$env:MINIO_ROOT_USER = 'culinary_jobs'
$env:MINIO_ROOT_PASSWORD = 'culinary_jobs_minio_password'
$env:MINIO_TEST_ENDPOINT = 'localhost:19000'
$env:MINIO_TEST_PUBLIC_URL = 'http://localhost:19000'
& $dotnetExe test CulinaryBlog.slnx --logger trx --results-directory $resultsPath --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Tests failed; inspect the TRX files.' }
function DatabaseSnapshot {
    $sql = 'SELECT (SELECT count(*) FROM culinary."Recipes") AS recipes, (SELECT count(*) FROM culinary."AspNetUsers") AS users, (SELECT count(*) FROM culinary."BackgroundTasks") AS tasks, (SELECT count(*) FROM public."__EFMigrationsHistory") AS migrations;'
    $snapshot = $sql | docker compose -f compose.jobs-tests.yml exec -T postgres psql -U culinary -d culinary_jobs_tests -t -A
    if ($LASTEXITCODE -ne 0) { throw 'Could not read restart snapshot.' }
    return ($snapshot -join '').Trim()
}
$beforeRestart = DatabaseSnapshot
docker compose -f compose.jobs-tests.yml restart api | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'API restart failed.' }
Ready
$afterRestart = DatabaseSnapshot
if ($beforeRestart -ne $afterRestart) { throw 'Restart changed persisted row counts or migration history.' }
[pscustomobject]@{ Before=$beforeRestart; After=$afterRestart; Result='PASS' } | ConvertTo-Json |
    Set-Content -Encoding utf8 artifacts/test-results/jobs-observability/restart.json

$failures = @()
foreach ($service in @('redis', 'minio', 'postgres')) {
    try {
        docker compose -f compose.jobs-tests.yml stop $service | Out-Null
        if ($LASTEXITCODE -ne 0) { throw ('Could not stop ' + $service) }
        Start-Sleep -Seconds 3
        if ((Status '/health/live') -ne 200) { throw 'Liveness must stay healthy.' }
        if ((Status '/health') -ne 503) { throw 'Aggregate health must report dependency failure.' }
        $expectedReady = if ($service -eq 'minio') { 200 } else { 503 }
        if ((Status '/health/ready') -ne $expectedReady) { throw 'Unexpected readiness result.' }
        $failures += [pscustomobject]@{ Service=$service; Liveness=200; Aggregate=503; Readiness=$expectedReady; Result='PASS' }
    } finally {
        docker compose -f compose.jobs-tests.yml start $service | Out-Null
        Ready
    }
}
$failures | ConvertTo-Json | Set-Content -Encoding utf8 artifacts/test-results/jobs-observability/dependency-failures.json
# Request traces and metrics must reach the collector; export may be batched.
Invoke-RestMethod 'http://localhost:5059/api/v1/recipes/search?q=lab&pageSize=2' | Out-Null
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    docker compose -f compose.jobs-tests.yml cp collector:/data/telemetry.json artifacts/test-results/jobs-observability/telemetry.json | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $data = Get-Content -LiteralPath artifacts/test-results/jobs-observability/telemetry.json -Raw
        if ($data.Contains('resourceSpans') -and $data.Contains('resourceMetrics') -and $data.Contains('culinary.http.requests') -and $data.Contains('Npgsql') -and $data.Contains('culinary.recipe.created') -and $data.Contains('culinary.recipe.published') -and $data.Contains('culinary.http.errors')) { break }
    }
    Start-Sleep -Seconds 2
}
if (-not ($data.Contains('resourceSpans') -and $data.Contains('resourceMetrics') -and $data.Contains('culinary.http.requests') -and $data.Contains('Npgsql') -and $data.Contains('culinary.recipe.created') -and $data.Contains('culinary.recipe.published') -and $data.Contains('culinary.http.errors'))) {
    throw 'Collector did not receive expected request/database traces and metrics.'
}
docker compose -f compose.jobs-tests.yml logs --no-color api > artifacts/test-results/jobs-observability/api.log
$logs = Get-Content -LiteralPath artifacts/test-results/jobs-observability/api.log -Raw
foreach ($required in @('CorrelationId','TraceId','ElapsedMs','UserId','RequestType')) {
    if (-not $logs.Contains($required)) { throw ('Missing structured log property: ' + $required) }
}
Write-Output 'PASS: PostgreSQL jobs, MinIO, SMTP, health failure/recovery, structured logging and OTLP export.'
$http.Dispose()

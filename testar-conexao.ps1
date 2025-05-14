Write-Host "Testando conexão com PostgreSQL..." -ForegroundColor Cyan

$connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=pi"
$pgPath = "C:\Program Files\PostgreSQL\17\bin"

Set-Location $PSScriptRoot
$env:PATH = "$pgPath;$env:PATH"

Write-Host "Tentando conectar usando PSQL..." -ForegroundColor Yellow
$env:PGPASSWORD = "pi"

try {
    $result = & "$pgPath\psql.exe" -h localhost -U postgres -d postgres -c "SELECT 1" -t
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Conexão bem sucedida!" -ForegroundColor Green
        Write-Host "Resultado: $result" -ForegroundColor Green
    } else {
        Write-Host "Falha na conexão." -ForegroundColor Red
    }
} catch {
    Write-Host "Erro ao executar psql: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Verificando existência do banco de dados AtestadoMedicoDB..." -ForegroundColor Yellow

try {
    $dbExists = & "$pgPath\psql.exe" -h localhost -U postgres -d postgres -c "SELECT datname FROM pg_database WHERE datname='AtestadoMedicoDB'" -t
    if ($dbExists.Trim() -ne "") {
        Write-Host "O banco de dados AtestadoMedicoDB existe!" -ForegroundColor Green
    } else {
        Write-Host "O banco de dados AtestadoMedicoDB não existe." -ForegroundColor Red
        
        Write-Host "Criando banco de dados AtestadoMedicoDB..." -ForegroundColor Yellow
        & "$pgPath\createdb.exe" -h localhost -U postgres AtestadoMedicoDB
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Banco de dados AtestadoMedicoDB criado com sucesso!" -ForegroundColor Green
        } else {
            Write-Host "Falha ao criar o banco de dados." -ForegroundColor Red
        }
    }
} catch {
    Write-Host "Erro ao verificar banco de dados: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Teste concluído." -ForegroundColor Cyan
Pause 
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "Alterando senha do PostgreSQL na aplicação" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

$PGBIN = "C:\Program Files\PostgreSQL\17\bin"
Write-Host "Usando PostgreSQL em: $PGBIN" -ForegroundColor Yellow

if (-not (Test-Path -Path "$PGBIN\psql.exe")) {
    Write-Host "ERRO: PostgreSQL não encontrado no caminho $PGBIN" -ForegroundColor Red
    Write-Host "Verifique se o PostgreSQL está instalado corretamente." -ForegroundColor Red
    Pause
    exit 1
}

Write-Host ""
$PGPASSWORD = Read-Host -Prompt "Digite a nova senha do usuário postgres"
$env:PGPASSWORD = $PGPASSWORD

Write-Host ""
Write-Host "Testando conexão com PostgreSQL..." -ForegroundColor Yellow
try {
    $result = & "$PGBIN\psql.exe" -h localhost -U postgres -c "SELECT 1" -t
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRO: Não foi possível conectar ao PostgreSQL. Verifique a senha." -ForegroundColor Red
        Pause
        exit 1
    }
    Write-Host "Conexão com PostgreSQL estabelecida com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "ERRO: Falha ao executar psql: $_" -ForegroundColor Red
    Pause
    exit 1
}

Write-Host ""
Write-Host "Atualizando arquivo de configuração appsettings.json..." -ForegroundColor Yellow

# Ler o arquivo appsettings.json
$appSettingsPath = "appsettings.json"
if (-not (Test-Path -Path $appSettingsPath)) {
    Write-Host "ERRO: Arquivo appsettings.json não encontrado!" -ForegroundColor Red
    Pause
    exit 1
}

try {
    $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    
    # Encontrar a string de conexão atual e extrair todas as partes exceto a senha
    $currentConnStr = $appSettings.ConnectionStrings.PostgreSQLConnection
    
    if (-not $currentConnStr) {
        Write-Host "ERRO: String de conexão PostgreSQL não encontrada em appsettings.json" -ForegroundColor Red
        Pause
        exit 1
    }
    
    # Criar nova string de conexão mantendo todos os parâmetros, mas alterando a senha
    $newConnStr = $currentConnStr -replace "Password=[^;]+", "Password=$PGPASSWORD"
    
    # Atualizar o arquivo JSON
    $appSettings.ConnectionStrings.PostgreSQLConnection = $newConnStr
    
    # Converter de volta para JSON e salvar o arquivo
    $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
    
    Write-Host "Arquivo appsettings.json atualizado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Nova string de conexão (com senha ocultada):" -ForegroundColor Yellow
    Write-Host ($newConnStr -replace "Password=[^;]+", "Password=********") -ForegroundColor Yellow
} catch {
    Write-Host "ERRO ao atualizar appsettings.json: $_" -ForegroundColor Red
    Pause
    exit 1
}

Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host "Senha do PostgreSQL atualizada com sucesso!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Agora você pode iniciar a aplicação normalmente com:" -ForegroundColor Yellow
Write-Host "    iniciar-aplicacao.bat" -ForegroundColor Cyan
Write-Host ""
Pause 
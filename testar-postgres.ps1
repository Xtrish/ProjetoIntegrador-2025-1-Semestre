Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "Testando conexão com PostgreSQL" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

$PGBIN = "C:\Program Files\PostgreSQL\17\bin"
Write-Host "Verificando PostgreSQL em: $PGBIN" -ForegroundColor Yellow

if (-not (Test-Path -Path "$PGBIN\psql.exe")) {
    Write-Host "ERRO: PostgreSQL não encontrado no caminho $PGBIN" -ForegroundColor Red
    Write-Host "Verifique se o PostgreSQL está instalado corretamente." -ForegroundColor Red
    
    # Tentar outros caminhos comuns
    $possiblePaths = @(
        "C:\Program Files\PostgreSQL\16\bin",
        "C:\Program Files\PostgreSQL\15\bin",
        "C:\Program Files\PostgreSQL\14\bin",
        "C:\Program Files\PostgreSQL\13\bin",
        "C:\Program Files\PostgreSQL\12\bin",
        "C:\Program Files\PostgreSQL\11\bin",
        "C:\Program Files\PostgreSQL\10\bin"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path -Path "$path\psql.exe") {
            Write-Host "PostgreSQL encontrado em: $path" -ForegroundColor Green
            $PGBIN = $path
            break
        }
    }
    
    if (-not (Test-Path -Path "$PGBIN\psql.exe")) {
        Pause
        exit 1
    }
}

Write-Host ""
$PGPASSWORD = Read-Host -Prompt "Digite a senha do usuário postgres (tente 'pi')"
$env:PGPASSWORD = $PGPASSWORD

Write-Host ""
Write-Host "Tentando conectar ao PostgreSQL..." -ForegroundColor Yellow
$env:PATH = "$PGBIN;$env:PATH"

try {
    $output = & "$PGBIN\psql.exe" -h localhost -U postgres -c "SELECT current_database();" -t
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Conexão bem sucedida com PostgreSQL!" -ForegroundColor Green
        Write-Host "Banco atual: $output" -ForegroundColor Green
        
        # Verifica se o banco AtestadoMedicoDB existe
        $dbExists = & "$PGBIN\psql.exe" -h localhost -U postgres -c "SELECT datname FROM pg_database WHERE datname='AtestadoMedicoDB';" -t
        if ($dbExists.Trim() -eq "") {
            Write-Host "O banco de dados 'AtestadoMedicoDB' não existe." -ForegroundColor Yellow
            $createDb = Read-Host "Deseja criar o banco de dados? (S/N)"
            if ($createDb.ToUpper() -eq "S") {
                & "$PGBIN\createdb.exe" -h localhost -U postgres AtestadoMedicoDB
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Banco de dados 'AtestadoMedicoDB' criado com sucesso!" -ForegroundColor Green
                } else {
                    Write-Host "Falha ao criar o banco de dados." -ForegroundColor Red
                }
            }
        } else {
            Write-Host "O banco de dados 'AtestadoMedicoDB' já existe." -ForegroundColor Green
        }
        
        # Atualizar o arquivo appsettings.json
        Write-Host ""
        Write-Host "Atualizando appsettings.json com a senha correta..." -ForegroundColor Yellow
        $appSettings = Get-Content appsettings.json -Raw
        $appSettings = $appSettings -replace 'Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=[^"]+', "Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=$PGPASSWORD"
        Set-Content -Path appsettings.json -Value $appSettings
        Write-Host "Configuração salva!" -ForegroundColor Green
    } else {
        Write-Host "FALHA ao conectar ao PostgreSQL. Verifique se a senha está correta." -ForegroundColor Red
    }
} catch {
    Write-Host "Erro ao executar comando: $_" -ForegroundColor Red
}

Pause 
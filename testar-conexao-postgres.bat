@echo off
echo ======================================================
echo Testando conexao com PostgreSQL
echo ======================================================
echo.

set PGBIN=C:\Program Files\PostgreSQL\17\bin
echo Usando PostgreSQL em: %PGBIN%

if not exist "%PGBIN%\psql.exe" (
    echo ERRO: PostgreSQL nao encontrado no caminho %PGBIN%
    echo Verifique se o PostgreSQL esta instalado corretamente.
    pause
    exit /b 1
)

echo.
set /p PGPASSWORD="Digite a senha do usuario postgres: "

echo.
echo Tentando conectar ao PostgreSQL...
echo SELECT current_database(); | "%PGBIN%\psql.exe" -h localhost -U postgres -t

if %errorlevel% neq 0 (
    echo FALHA ao conectar ao PostgreSQL. Verifique se a senha esta correta.
    pause
    exit /b 1
) else (
    echo.
    echo Conexao bem sucedida com PostgreSQL!
    echo.
    echo Salvando configuracao para Entity Framework...
    
    echo.
    echo Atualizando appsettings.json...
    powershell -Command "(Get-Content appsettings.json) -replace 'Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=postgres', 'Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=%PGPASSWORD%' | Set-Content appsettings.json"
    echo Configuracao salva!
    
    echo.
    echo Para continuar com a migracao, execute novamente o script postgres-direto.bat
)

pause 
@echo off
echo ======================================================
echo Iniciando Aplicacao de Atestados Medicos
echo ======================================================
echo.

echo Verificando por instancias em execucao...
taskkill /f /im AtestadoMedico.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo Aplicacao anterior encerrada com sucesso.
) else (
    echo Nenhuma instancia anterior encontrada.
)
echo.

echo Pressione CTRL+C para encerrar a aplicacao
echo.

dotnet run --project AtestadoMedico.csproj --launch-profile "http"
echo.
echo A aplicacao foi encerrada ou ocorreu um erro.
echo.
pause 
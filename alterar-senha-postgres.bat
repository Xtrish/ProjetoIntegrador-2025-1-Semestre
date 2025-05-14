@echo off
echo ======================================================
echo Alterando senha do PostgreSQL na aplicacao
echo ======================================================
echo.

powershell.exe -ExecutionPolicy Bypass -File alterar-senha-postgres.ps1

echo.
echo Se ocorreu algum erro, execute o script PowerShell diretamente.
echo powershell.exe -ExecutionPolicy Bypass -File alterar-senha-postgres.ps1
echo.
pause 
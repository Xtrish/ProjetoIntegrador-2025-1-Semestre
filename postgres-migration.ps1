Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "Migrando para PostgreSQL (PowerShell Script)" -ForegroundColor Cyan
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
$PGPASSWORD = Read-Host -Prompt "Digite a senha do usuário postgres"
$env:PGPASSWORD = $PGPASSWORD

Write-Host ""
Write-Host "Adicionando PostgreSQL ao PATH temporariamente..." -ForegroundColor Yellow
$env:PATH = "$PGBIN;$env:PATH"

Write-Host ""
Write-Host "Excluindo banco de dados existente (se houver)..." -ForegroundColor Yellow
& "$PGBIN\dropdb.exe" -h localhost -U postgres AtestadoMedicoDB 2>$null
Write-Host "OK!"

Write-Host ""
Write-Host "Criando banco de dados AtestadoMedicoDB..." -ForegroundColor Yellow
& "$PGBIN\createdb.exe" -h localhost -U postgres AtestadoMedicoDB
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha ao criar o banco de dados." -ForegroundColor Red
    Pause
    exit 1
}
Write-Host "OK!"

Write-Host ""
Write-Host "Atualizando arquivos de configuração..." -ForegroundColor Yellow
Write-Host ""

# Atualizar appsettings.json
Write-Host "Atualizando appsettings.json..." -ForegroundColor Yellow
$appSettings = Get-Content appsettings.json -Raw
$appSettings = $appSettings -replace 'Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=postgres', "Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=$PGPASSWORD"
Set-Content -Path appsettings.json -Value $appSettings
Write-Host "OK!"

# Atualizar Program.cs
Write-Host "Atualizando Program.cs..." -ForegroundColor Yellow
$programCs = Get-Content Program.cs -Raw
$programCs = $programCs -replace 'var usePostgres = false', 'var usePostgres = true'
Set-Content -Path Program.cs -Value $programCs
Write-Host "OK!"

Write-Host ""
Write-Host "Removendo migrações existentes..." -ForegroundColor Yellow
if (Test-Path -Path "Migrations") {
    Remove-Item -Recurse -Force "Migrations"
}
Write-Host "OK!"

Write-Host ""
Write-Host "Criando migração inicial..." -ForegroundColor Yellow
$env:ConnectionStrings__PostgreSQLConnection = "Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=$PGPASSWORD"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add InitialPostgreSQL
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao criar migração." -ForegroundColor Red
    Pause
    exit 1
}
Write-Host "OK!"

Write-Host ""
Write-Host "Aplicando migração ao banco PostgreSQL..." -ForegroundColor Yellow
dotnet ef database update --connection "Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password=$PGPASSWORD"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao aplicar migração." -ForegroundColor Red
    Pause
    exit 1
}
Write-Host "OK!"

Write-Host ""
Write-Host "Iniciando importação de dados de SQLite para PostgreSQL..." -ForegroundColor Yellow

Write-Host "Criando script temporário..." -ForegroundColor Yellow
if (-not (Test-Path -Path "temp")) {
    New-Item -Path "temp" -ItemType Directory | Out-Null
}

$importarCs = @"
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AtestadoMedico.Data;
using AtestadoMedico.Models;

namespace AtestadoMedico.Importacao
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Iniciando importação de dados...");

            // Configurar o contexto do SQLite
            var sqliteOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            sqliteOptionsBuilder.UseSqlite("Data Source=AtestadoMedico.db");
            using var sqliteContext = new ApplicationDbContext(sqliteOptionsBuilder.Options);

            // Configurar o contexto do PostgreSQL
            var pgOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "well";
            var connStr = \$"Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password={password}";
            Console.WriteLine(\$"Usando string de conexão: {connStr}");
            pgOptionsBuilder.UseNpgsql(connStr);
            using var pgContext = new ApplicationDbContext(pgOptionsBuilder.Options);

            try
            {
                // Limpar tabelas do PostgreSQL
                pgContext.Atestados.RemoveRange(pgContext.Atestados);
                pgContext.Usuarios.RemoveRange(pgContext.Usuarios);
                pgContext.SaveChanges();

                Console.WriteLine("Importando usuários...");
                var usuarios = sqliteContext.Usuarios.ToList();
                foreach (var usuario in usuarios)
                {
                    pgContext.Usuarios.Add(new Usuario
                    {
                        Id = usuario.Id,
                        Nome = usuario.Nome,
                        Email = usuario.Email,
                        Senha = usuario.Senha,
                        IsAdmin = usuario.IsAdmin,
                        DataCadastro = DateTime.SpecifyKind(usuario.DataCadastro, DateTimeKind.Utc)
                    });
                }
                pgContext.SaveChanges();
                Console.WriteLine(\$"Importados {usuarios.Count} usuários.");

                Console.WriteLine("Importando atestados...");
                var atestados = sqliteContext.Atestados.ToList();
                foreach (var atestado in atestados)
                {
                    pgContext.Atestados.Add(new Atestado
                    {
                        Id = atestado.Id,
                        UsuarioId = atestado.UsuarioId,
                        DataAtestado = DateTime.SpecifyKind(atestado.DataAtestado, DateTimeKind.Utc),
                        NomeMedico = atestado.NomeMedico,
                        CRM = atestado.CRM,
                        Descricao = atestado.Descricao,
                        DataCadastro = DateTime.SpecifyKind(atestado.DataCadastro, DateTimeKind.Utc),
                        Status = atestado.Status,
                        AtualizadoPor = atestado.AtualizadoPor,
                        DataAtualizacao = atestado.DataAtualizacao.HasValue ? 
                            DateTime.SpecifyKind(atestado.DataAtualizacao.Value, DateTimeKind.Utc) : (DateTime?)null,
                        NomeArquivo = atestado.NomeArquivo,
                        TipoArquivo = atestado.TipoArquivo,
                        CaminhoArquivo = atestado.CaminhoArquivo
                    });
                }
                pgContext.SaveChanges();
                Console.WriteLine(\$"Importados {atestados.Count} atestados.");

                Console.WriteLine("Importação concluída com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(\$"Erro durante a importação: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
"@

Set-Content -Path "temp\Importar.cs" -Value $importarCs

Write-Host ""
Write-Host "Compilando e executando o script de importação..." -ForegroundColor Yellow
$env:PGPASSWORD = $PGPASSWORD
dotnet run --project AtestadoMedico.csproj

Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host "Migração para PostgreSQL concluída!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "A aplicação está configurada para usar PostgreSQL." -ForegroundColor Yellow
Write-Host "Você pode iniciar a aplicação normalmente com:" -ForegroundColor Yellow
Write-Host "    iniciar-aplicacao.bat" -ForegroundColor Cyan
Write-Host ""
Pause 
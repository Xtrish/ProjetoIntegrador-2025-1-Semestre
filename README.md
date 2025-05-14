# Sistema de Gerenciamento de Atestados Médicos

![Banner do Projeto](https://via.placeholder.com/800x200/0073e6/ffffff?text=Sistema+de+Atestados+M%C3%A9dicos)

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-5C2D91)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)](https://www.postgresql.org/download/)

Este sistema permite o gerenciamento completo de atestados médicos, com funcionalidades para funcionários (submissão e acompanhamento) e administradores (aprovação, rejeição e relatórios).

## 📋 Índice

- [Recursos](#-recursos)
- [Requisitos](#-requisitos)
- [Instalação](#-instalação)
- [Configuração](#-configuração)
- [Uso](#-uso)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Licença](#-licença)
- [Contato](#-contato)

## ✨ Recursos

- **Gerenciamento de Usuários**
  - Registro e login de usuários
  - Níveis de acesso (admin/usuário)
  - Perfis personalizados

- **Gerenciamento de Atestados**
  - Upload de arquivos PDF
  - Visualização em tempo real
  - Histórico completo de atestados
  - Status de aprovação

- **Painel Administrativo**
  - Aprovação/rejeição de atestados
  - Gestão de usuários
  - Exportação de relatórios

- **Interface Responsiva**
  - Design moderno e intuitivo
  - Compatível com dispositivos móveis

## 🔧 Requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 17](https://www.postgresql.org/download/)
- Navegador web moderno (Chrome, Firefox, Edge)

## 💻 Instalação

1. Clone este repositório:
   ```bash
   git clone https://github.com/seu-usuario/sistema-atestados-medicos.git
   cd sistema-atestados-medicos
   ```

2. Certifique-se de ter o PostgreSQL instalado e em execução

3. Configure o banco de dados usando o script de migração fornecido:
   ```bash
   .\postgres-migration.ps1
   ```

4. Inicie a aplicação:
   ```bash
   .\iniciar-aplicacao.bat
   ```

## ⚙️ Configuração

### Arquivos essenciais

Os seguintes arquivos são essenciais para o funcionamento e configuração da aplicação:

- `iniciar-aplicacao.bat` - Inicia a aplicação
- `postgres-migration.ps1` - Realiza a migração do banco de dados SQLite para PostgreSQL
- `alterar-senha-postgres.bat` - Altera a senha do PostgreSQL no arquivo de configuração
- `testar-conexao-postgres.bat` - Testa a conexão com o PostgreSQL

### Alterando a senha do PostgreSQL

Se você precisar alterar a senha do PostgreSQL na aplicação:

1. Execute o script `alterar-senha-postgres.bat`
2. Digite a nova senha quando solicitado
3. O script atualizará automaticamente o arquivo `appsettings.json`

## 🚀 Uso

1. Execute o script `iniciar-aplicacao.bat`
2. Acesse a aplicação em: http://localhost:5196
3. Faça login com um dos usuários padrão:
   - **Administrador**: `admin@admin.com` / `admin`

### Fluxo de Trabalho Básico

1. **Funcionários**:
   - Enviam atestados médicos com informações e arquivos anexos
   - Acompanham o status de aprovação

2. **Administradores**:
   - Visualizam todos os atestados pendentes
   - Aprovam ou rejeitam solicitações
   - Gerenciam usuários

## 📁 Estrutura do Projeto

```
sistema-atestados-medicos/
├── Controllers/        # Controladores da API
├── Models/             # Modelos de dados
├── Data/               # Contexto do banco de dados e migrations
├── ViewModels/         # Modelos de visualização
├── wwwroot/            # Arquivos estáticos (HTML, CSS, JavaScript)
├── Migrations/         # Migrações do banco de dados
├── iniciar-aplicacao.bat
├── postgres-migration.ps1
├── alterar-senha-postgres.bat
├── testar-conexao-postgres.bat
├── appsettings.json    # Configurações da aplicação
└── README.md
```
## 🔨 Tecnologias Utilizadas

- **Backend**:
  - C# 10.0
  - ASP.NET Core 9.0
  - Entity Framework Core
  - PostgreSQL 17

- **Frontend**:
  - HTML5
  - CSS3
  - JavaScript
  - Fetch API

- **DevOps**:
  - PowerShell (scripts de automação)
  - Batch scripts (Windows)
  - Git (controle de versão)
'''
## 📜 Licença

Este projeto está licenciado sob a [Licença MIT](LICENSE) - veja o arquivo LICENSE para detalhes.

## 📞 Contato

Se você tiver alguma dúvida ou sugestão, sinta-se à vontade para abrir uma issue ou enviar um pull request.

---

Desenvolvido por [Seu Nome/Equipe] 

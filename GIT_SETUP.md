# Instruções para criação do repositório Git

Este documento fornece os passos necessários para criar um repositório no GitHub e subir o projeto Sistema de Gerenciamento de Atestados Médicos.

## Pré-requisitos

1. Ter uma conta no [GitHub](https://github.com/)
2. Ter o [Git](https://git-scm.com/downloads) instalado em seu computador

## Passos para criar o repositório

### 1. Criar um novo repositório no GitHub

1. Acesse [GitHub](https://github.com/) e faça login em sua conta
2. Clique no botão "+" no canto superior direito e selecione "New repository"
3. Preencha os campos:
   - **Repository name**: sistema-atestados-medicos
   - **Description**: Sistema de Gerenciamento de Atestados Médicos com ASP.NET Core e PostgreSQL
   - **Visibility**: Public (ou Private, se preferir)
   - **Initialize this repository with**: Deixe desmarcado, pois vamos inicializar localmente

4. Clique em "Create repository"

### 2. Inicializar o repositório Git local

Abra um terminal (PowerShell ou CMD) na pasta do projeto e execute os seguintes comandos:

```bash
# Inicializar o repositório Git
git init

# Adicionar todos os arquivos ao stage
git add .

# Verificar o status (opcional)
git status

# Fazer o primeiro commit
git commit -m "Versão inicial do Sistema de Gerenciamento de Atestados Médicos"
```

### 3. Conectar o repositório local ao GitHub

```bash
# Adicionar o repositório remoto (substitua 'seu-usuario' pelo seu nome de usuário do GitHub)
git remote add origin https://github.com/seu-usuario/sistema-atestados-medicos.git

# Enviar o código para o GitHub (branch main)
git push -u origin main
```

Se o seu branch principal for 'master' em vez de 'main', ajuste o comando acima.

### 4. Verificar o repositório no GitHub

1. Acesse seu repositório no GitHub
2. Verifique se todos os arquivos foram enviados corretamente
3. Confirme se o README.md e a licença estão sendo exibidos corretamente

## Atualizações futuras

Para enviar atualizações futuras ao repositório:

```bash
# Verifique as alterações
git status

# Adicione as alterações
git add .

# Faça o commit com uma mensagem descritiva
git commit -m "Descrição das alterações realizadas"

# Envie para o GitHub
git push
```

## Trabalhando em equipe

Se você estiver trabalhando em equipe, é recomendável criar branches para cada funcionalidade:

```bash
# Criar e mudar para um novo branch
git checkout -b nome-da-funcionalidade

# Após concluir o trabalho, adicionar e commitar
git add .
git commit -m "Implementação da funcionalidade X"

# Enviar o branch para o GitHub
git push -u origin nome-da-funcionalidade
```

Em seguida, você pode criar um Pull Request no GitHub para mesclar as alterações ao branch principal.

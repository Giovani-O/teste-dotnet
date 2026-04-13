# Contacts API

API para gerenciamento de contatos com ASP.NET Core.

## Pré-requisitos

- .NET 10.0 SDK
- SQL Server (ou Docker)

## Instalação

1. Restore os pacotes NuGet:
   ```bash
   dotnet restore
   ```

2. Configure as variáveis de ambiente copiando `.env.example` para `.env` e ajustando os valores:
   ```bash
   cp .env.example .env
   ```
3. Execute o container do banco de dados:
   ```bash
   docker compose up -d
   ```

4. Aplique as migrações do banco de dados:
   ```bash
   dotnet ef database update --project Contacts.API
   ```

## Executando o projeto

```bash
dotnet run --project Contacts.API
```

A API estará disponível em `http://localhost:5232`

## Documentação

Após executar o projeto, a documentação do Swagger está disponível em:

**http://localhost:5232/swagger**

## Testes

Execute os testes com:

```bash
dotnet test
```

Para ver a cobertura de código:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

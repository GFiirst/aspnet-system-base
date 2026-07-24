# ASP.NET System Base

Sistema base de autenticação e autorização em ASP.NET Core 10.0, projetado para servir como fundação para outros sistemas. Este projeto fornece uma infraestrutura completa de autenticação JWT, gerenciamento de usuários, roles e permissões.

## 🚀 Características

- **Autenticação JWT**: Sistema de tokens com access token (15 min) e refresh token (30 dias)
- **Gerenciamento de Usuários**: CRUD completo de usuários com hash de senhas (BCrypt)
- **Roles e Permissões**: Sistema granular de autorização baseado em roles e permissões
- **Recuperação de Senha**: Sistema de recuperação via email com token JWT
- **Criptografia de Dados**: Criptografia AES-256 para dados sensíveis
- **Upload de Arquivos**: Suporte a múltiplos formatos (PDF, imagens, documentos, planilhas, texto)
- **Validações**: Validadores de CPF e telefone para uso futuro
- **Audit Logging**: Rastreamento automático de ações no banco de dados
- **CORS Configurável**: Suporte a múltiplas origens para frontend
- **Logging com Serilog**: Logs estruturados em arquivo e console
- **Swagger UI**: Documentação automática da API
- **PostgreSQL**: Banco de dados relacional com Entity Framework Core
- **Middleware de Erros**: Tratamento global de exceções
- **Rate Limiting**: Proteção contra abuso de API

## 🛠️ Tech Stack

- **.NET 10.0**
- **ASP.NET Core Web API**
- **Entity Framework Core 10.0**
- **PostgreSQL** (via Npgsql)
- **JWT Bearer Authentication**
- **Serilog** (Logging)
- **BCrypt.Net** (Hash de senhas)
- **MailKit** (Envio de emails)
- **Swagger/OpenAPI**
- **DotNetEnv** (Variáveis de ambiente)

## 📋 Pré-requisitos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 12+
- (Opcional) Docker para rodar o banco de dados

## 🔧 Instalação e Configuração

### 1. Clone o repositório

```bash
git clone <seu-repositorio>
cd aspnet-system-base
```

### 2. Configure as variáveis de ambiente

Copie o arquivo de exemplo e configure as variáveis:

```bash
cp .env.example .env
```

Edite o arquivo `.env` com suas configurações:

```env
# Database Configuration
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=SEU_BANCO;Username=SEU_USUARIO;Password=SUA_SENHA

# JWT Configuration
Jwt__AccessKey=your-super-secret-access-key-min-32-chars
Jwt__RefreshKey=your-super-secret-refresh-key-min-32-chars
Jwt__ResetPasswordKey=your-super-secret-reset-key-min-32-chars
Jwt__Issuer=AuthApi
Jwt__Audience=AuthApiUsers
Jwt__AccessExpirationMinutes=15
Jwt__ResetPasswordExpirationMinutes=15
Jwt__RefreshExpirationDays=30

# CORS Configuration
Cors__AllowedOrigins=http://localhost:3000,http://localhost:5173

# File Upload Configuration
FILE_PATH=uploads

# Email Configuration (para recuperação de senha)
MAIL_USER=seu-email@gmail.com
MAIL_PASS=sua-senha-app
FRONTEND_URL=http://localhost:3000

# Encryption Configuration (chave deve ter exatamente 32 bytes)
Encryption__Key=MinhaChaveSecretaDe32BytesAES256
```

**Importante**: 
- As chaves JWT devem ter no mínimo 32 caracteres
- A chave de criptografia deve ter exatamente 32 bytes (256 bits)
- Para usar o Gmail, configure uma senha de app em configurações da conta Google

### 3. Configure o banco de dados PostgreSQL

Crie o banco de dados no PostgreSQL:

```sql
CREATE DATABASE SEU_BANCO;
```

### 4. Restaure as dependências

```bash
dotnet restore
```

### 5. Execute as migrations

```bash
dotnet ef database update
```

Isso criará todas as tabelas necessárias e executará os seeders iniciais.

## 🏃 Como Rodar

### Modo Desenvolvimento

```bash
dotnet run
```

A API estará disponível em `https://localhost:5001` ou `http://localhost:5000`

### Swagger UI

Em modo de desenvolvimento, acesse a documentação Swagger em:
```
https://localhost:5001/swagger
```

### Build para Produção

```bash
dotnet build -c Release
dotnet run -c Release
```

## 📁 Estrutura do Projeto

```
aspnet-system-base/
├── Features/
│   ├── Auth/                 # Autenticação (Login, Register, Refresh Token)
│   │   ├── AuthController.cs
│   │   ├── AuthService.cs
│   │   ├── dto/              # DTOs de autenticação
│   │   └── entities/         # Entidades de tokens
│   ├── User/                 # Gerenciamento de usuários
│   │   ├── UserController.cs
│   │   ├── UserService.cs
│   │   └── entities/         # Entidade User
│   ├── Roles/                # Sistema de roles
│   │   ├── enums/            # RolesEnum (admin, manager, agent, user)
│   │   └── entities/         # Entidade Role
│   ├── Permissions/          # Sistema de permissões
│   │   ├── enum/             # PermissionAction (Create, Read, Update, Delete)
│   │   ├── Policies.cs       # Definição de policies
│   │   └── PermissionHandler.cs
│   ├── Relations/           # Relações (User-Role, Role-Permission)
│   ├── Audit/                # Sistema de auditoria
│   │   ├── AuditInterceptor.cs
│   │   └── entities/         # AuditLog
│   ├── Infrastructure/       # Configurações de infraestrutura
│   │   ├── Data/             # DbContext e Migrations
│   │   ├── Jwt/              # Serviço de tokens JWT
│   │   ├── Email/            # Configurações de email
│   │   ├── Encryption/       # Serviço de criptografia AES-256
│   │   └── Validation/       # Validadores (CPF, Telefone)
│   └── Shared/               # Componentes compartilhados
│       ├── Extensions/       # Extension methods
│       ├── Middlewares/      # Global error handling
│       ├── Seeds/            # Seeders iniciais
│       ├── Upload/           # Serviço de upload de arquivos
│       └── Exceptions/       # Exceções customizadas
├── Program.cs                # Entry point e configuração
├── appsettings.json          # Configurações da aplicação
├── .env                      # Variáveis de ambiente (não commitado)
└── .env.example              # Exemplo de variáveis de ambiente
```

## 🔐 Endpoints da API

### Autenticação

#### Registrar Usuário
```http
POST /auth/sign-up
Content-Type: application/json

{
  "name": "Nome do Usuário",
  "email": "usuario@email.com",
  "password": "senha123"
}
```

#### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "usuario@email.com",
  "password": "senha123"
}
```

**Resposta**: Define cookies `access_token` e `refresh_token` (HttpOnly, Secure)

#### Refresh Token
```http
POST /auth/refresh
```

**Resposta**: Novo access token em cookie

#### Logout
```http
POST /auth/logout
```

#### Esqueci Senha
```http
POST /auth/forgot-password
Content-Type: application/json

{
  "email": "usuario@email.com"
}
```

#### Resetar Senha
```http
POST /auth/reset-password
Content-Type: application/json

{
  "token": "jwt-reset-token",
  "newPassword": "novaSenha123"
}
```

### Usuários

#### Criar Usuário (Requer permissão)
```http
POST /user/create
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "Nome do Usuário",
  "email": "usuario@email.com",
  "password": "senha123",
  "role": "user"
}
```

## 👥 Roles e Permissões

### Roles Disponíveis
- `admin` - Acesso total
- `manager` - Gerenciamento de usuários
- `agent` - Acesso limitado
- `user` - Acesso básico

### Permissões
O sistema usa permissões granulares no formato `{Action}{Subject}`:
- `UserCreate` - Criar usuários
- `UserRead` - Ler usuários
- `UserUpdate` - Atualizar usuários
- `UserDelete` - Deletar usuários
- (e outras permissões configuráveis)

### Exemplo de Uso em Controllers

```csharp
[Authorize(Policy = Policies.UserCreate)]
public async Task<IActionResult> CreateUser(CreateUserDto dto)
{
    // Apenas usuários com permissão UserCreate podem acessar
}
```

## 🔄 Como Usar como Base para Outros Sistemas

Este projeto foi desenhado para ser facilmente adaptado como base para novos sistemas:

### 1. Copie o Projeto

```bash
cp -r aspnet-system-base meu-novo-sistema
cd meu-novo-sistema
```

### 2. Renomeie o Projeto

Edite o arquivo `.csproj`:
```xml
<PropertyGroup>
  <RootNamespace>MeuNovoSistema</RootNamespace>
</PropertyGroup>
```

### 3. Adicione Novas Features

Crie novas pastas em `Features/` seguindo o padrão existente:
```
Features/
├── Produtos/          # Nova feature de produtos
│   ├── ProdutosController.cs
│   ├── ProdutosService.cs
│   ├── dto/
│   └── entities/
```

### 4. Configure Novas Permissões

Adicione permissões em `Features/Permissions/Permissions.cs` e configure as policies em `Features/Permissions/Policies.cs`.

### 5. Execute Novas Migrations

```bash
dotnet ef migrations add AddProdutos
dotnet ef database update
```

### 6. Aproveite a Infraestrutura

- **Autenticação**: Já configurada e funcionando
- **Autorização**: Use `[Authorize(Policy = Policies.SuaPolicy)]`
- **Audit**: Ações no banco são automaticamente auditadas
- **Logging**: Logs configurados com Serilog
- **Error Handling**: Middleware global de erros
- **CORS**: Configure as origens no `.env`
- **Upload de Arquivos**: Use `IFileUploadService` para uploads
- **Criptografia**: Use `IEncryptionService` para criptografar dados sensíveis
- **Email**: Use `EmailSettings` para envio de emails
- **Validações**: Use `[CpfValidation]` e `[PhoneValidation]` atributos

## 📝 Logging

Os logs são salvos em `logs/log-{data}.txt` com retenção de 15 dias. O formato inclui timestamp, nível, contexto e mensagem.

## 🔒 Segurança

- Senhas hash com BCrypt (work factor 12)
- Tokens JWT com assinatura HMAC-SHA256
- Tokens de refresh token com hash SHA256
- Cookies HttpOnly e Secure
- Criptografia AES-256 para dados sensíveis
- CORS configurável
- Audit trail de todas as operações
- Rate limiting (5 requisições por minuto por IP)
- Validação de CPF e telefone

## 🧪 Testes

Para adicionar testes, crie um projeto de testes:

```bash
dotnet new xunit -n aspnet-system-base.Tests
dotnet add aspnet-system-base.Tests/aspnet-system-base.Tests.csproj reference aspnet-system-base.csproj
```

## 📦 Dependências Principais

- `Microsoft.AspNetCore.Authentication.JwtBearer` - Autenticação JWT
- `Microsoft.EntityFrameworkCore` - ORM
- `Npgsql.EntityFrameworkCore.PostgreSQL` - Provider PostgreSQL
- `BCrypt.Net-Next` - Hash de senhas
- `Serilog.AspNetCore` - Logging
- `MailKit` - Envio de emails
- `DotNetEnv` - Carregamento de .env
- `Swashbuckle.AspNetCore` - Swagger

## 🤝 Contribuindo

Este é um projeto base. Para contribuir:
1. Siga o padrão de estrutura em `Features/`
2. Use injeção de dependência
3. Implemente DTOs para entrada/saída
4. Adicione validações apropriadas
5. Documente novos endpoints no Swagger

## 📄 Licença

Este projeto serve como base para sistemas internos. Adapte conforme necessário.

## 🆘 Suporte

Para dúvidas ou problemas:
- Verifique os logs em `logs/`
- Confirme as configurações no `.env`
- Valide a conexão com o banco de dados
- Consulte a documentação Swagger em `/swagger`

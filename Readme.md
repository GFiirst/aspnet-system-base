# ASP.NET System Base

API base em ASP.NET Core 10 para projetos que precisam de cadastro, autenticação por JWT, sessões com refresh token, recuperação de senha e infraestrutura inicial de autorização, auditoria, criptografia e upload.

> Estado atual: a API expõe os fluxos de autenticação e o health check. Roles, permissões e upload já fazem parte da infraestrutura, mas ainda não possuem endpoints próprios. `Features/User/UserController.cs` está vazio; portanto, não há CRUD público de usuários nesta versão.

## Funcionalidades

- Cadastro com a role `user` atribuída automaticamente.
- Login com access token e refresh token em cookies HttpOnly.
- Renovação do access token, logout e validação da sessão.
- Recuperação de senha por e-mail via Gmail SMTP.
- Limite de cinco sessões ativas por usuário; a mais antiga é revogada ao exceder o limite.
- Senhas com BCrypt; e-mails, IPs de sessão e IPs de auditoria criptografados com AES-256.
- Refresh tokens armazenados somente como hash SHA-256.
- Auditoria automática de inclusões, alterações e exclusões feitas pelo EF Core.
- Roles e permissões sincronizadas no startup por seeders.
- Rate limiting de 5 requisições por minuto nos endpoints anotados com a policy `Default`.
- Tratamento global de erros, CORS, Serilog, Swagger e health check.
- Serviços reutilizáveis de upload e validação de arquivos, CPF e telefone.

## Tecnologias

- .NET 10, ASP.NET Core Web API e Entity Framework Core 10
- PostgreSQL e Npgsql
- JWT Bearer Authentication, BCrypt.Net, AES-256-CBC e SHA-256
- MailKit, Serilog, Swashbuckle/OpenAPI e DotNetEnv
- Docker e Docker Compose

## Como a autenticação funciona

O login cria dois cookies com `SameSite=Strict`:

| Cookie | Finalidade | Validade padrão |
|---|---|---:|
| `access_token` | Autenticar requisições protegidas | 15 minutos |
| `refresh_token` | Emitir um novo access token | 30 dias |

O handler JWT procura o token no cookie `access_token`; o header `Authorization: Bearer` não é lido pela configuração atual. Clientes web devem enviar credenciais (`credentials: "include"` no `fetch` ou equivalente).

Em desenvolvimento, `CookieSettings:Secure` é `false` para permitir HTTP. Em produção o padrão é `true`, portanto a API deve ser publicada atrás de HTTPS.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL
- Docker com Compose, caso prefira containers

## Configuração local

Crie o arquivo de ambiente:

```bash
cp .env.example .env
```

Preencha as configurações principais:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=system_base;Username=postgres;Password=troque-a-senha

Jwt__AccessKey=uma-chave-de-acesso-longa-e-segura
Jwt__RefreshKey=uma-chave-de-refresh-longa-e-segura
Jwt__ResetPasswordKey=uma-chave-de-reset-longa-e-segura
Jwt__Issuer=AuthApi
Jwt__Audience=AuthApiUsers
Jwt__AccessExpirationMinutes=15
Jwt__ResetPasswordExpirationMinutes=15
Jwt__RefreshExpirationDays=30

Encryption__Key=12345678901234567890123456789012
Cors__AllowedOrigins=http://localhost:3000,http://localhost:5173
APPLY_MIGRATIONS_ON_STARTUP=false

MAIL_USER=
MAIL_PASS=
FRONTEND_URL=http://localhost:3000

ADMIN_NAME=
ADMIN_EMAIL=
ADMIN_PASSWORD=

FILE_PATH=uploads
LOG_PATH=logs
```

Pontos importantes:

- `Encryption__Key` precisa ter exatamente 32 bytes em UTF-8. Trocá-la depois de gravar dados impede a descriptografia dos valores existentes.
- Use chaves JWT longas, aleatórias, diferentes entre si e fora do controle de versão.
- `MAIL_USER` e `MAIL_PASS` autenticam em `smtp.gmail.com:587` com STARTTLS. Para Gmail, use uma senha de app.
- O e-mail usa o link `${FRONTEND_URL}/reset-password?token=...`.
- O admin inicial só é criado quando as três variáveis `ADMIN_*` estão preenchidas.
- Se `Cors__AllowedOrigins` ficar vazio, a API aceita qualquer origem, sem credenciais.

Restaure dependências e aplique as migrations:

```bash
dotnet restore
dotnet tool restore
dotnet ef database update
```

Como alternativa, use `APPLY_MIGRATIONS_ON_STARTUP=true`. Os seeders de roles, permissões e admin sempre rodam no startup e são idempotentes.

## Execução

```bash
dotnet run
```

O perfil HTTP padrão usa `http://localhost:4321`. Para o perfil HTTPS:

```bash
dotnet run --launch-profile https
```

- HTTPS: `https://localhost:7038`
- HTTP alternativo: `http://localhost:5054`
- Swagger em desenvolvimento: `/swagger`
- Health check: `/health`

Build Release:

```bash
dotnet build -c Release
dotnet run -c Release --no-build
```

## Docker Compose

O Compose inicia a API em `http://localhost:5000` e publica o PostgreSQL na porta `5433` do host. Adicione também ao `.env`:

```env
DB_NAME=system_base
DB_USER=postgres
DB_PASSWORD=troque-a-senha
HOST_POSTGRES_PATH=./postgres-data
HOST_UPLOAD_PATH=./uploads
HOST_LOG_PATH=./logs
```

```bash
docker compose up -d --build
docker compose logs -f aspnet-system-base
docker compose down
```

O container da API depende do health check do PostgreSQL e possui seu próprio check em `/health`. O Swagger não é habilitado em produção. Veja outras opções em [DOCKER.md](DOCKER.md).

## Endpoints atuais

| Método | Rota | Autenticação | Rate limit | Descrição |
|---|---|---|---|---|
| `POST` | `/auth/sign-up` | Público | Sim | Cadastra usuário com role `user` |
| `POST` | `/auth/login` | Público | Sim | Autentica e cria uma sessão |
| `POST` | `/auth/refresh` | Cookie de refresh | Sim | Renova o access token |
| `POST` | `/auth/logout` | Público | Sim | Revoga a sessão, se presente, e remove cookies |
| `GET` | `/auth/validate` | Cookie de acesso | Não | Retorna o usuário autenticado |
| `POST` | `/auth/forgot-password` | Público | Sim | Envia o link se a conta existir |
| `POST` | `/auth/reset-password` | Público | Sim | Redefine a senha e revoga sessões |
| `GET` | `/health` | Público | Não | Informa a disponibilidade da API |

### Cadastro

```http
POST /auth/sign-up
Content-Type: application/json

{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "password": "senha-segura"
}
```

O nome aceita até 100 caracteres, o e-mail até 255 e a senha entre 5 e 100. E-mails são normalizados para minúsculas e não podem se repetir.

### Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "maria@example.com",
  "password": "senha-segura"
}
```

Resposta resumida:

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "eyJ...",
  "expiresAt": "2026-08-20T15:00:00Z",
  "userInfo": {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Maria Silva",
    "email": "maria@example.com",
    "createdAt": "2026-08-20T14:45:00Z",
    "roles": ["user"]
  }
}
```

Embora os tokens também apareçam na resposta atual, prefira os cookies HttpOnly no cliente web e não persista tokens em `localStorage`.

### Renovar e validar a sessão

```http
POST /auth/refresh
Cookie: refresh_token=eyJ...
```

A resposta informa `expiresAt`; o novo token é gravado no cookie `access_token`.

```http
GET /auth/validate
Cookie: access_token=eyJ...
```

Retorna `id`, `name`, `email` e `roles` do usuário autenticado.

### Recuperar a senha

```http
POST /auth/forgot-password
Content-Type: application/json

{
  "email": "maria@example.com"
}
```

O endpoint sempre responde com mensagem genérica para não revelar se o e-mail existe.

```http
POST /auth/reset-password
Content-Type: application/json

{
  "token": "token-recebido-no-link",
  "newPassword": "nova-senha-segura"
}
```

A senha precisa ter ao menos 5 caracteres. A alteração revoga todos os refresh tokens ativos do usuário.

### Exemplo com curl

```bash
curl -c cookies.txt -H 'Content-Type: application/json' \
  -d '{"email":"maria@example.com","password":"senha-segura"}' \
  http://localhost:4321/auth/login

curl -b cookies.txt http://localhost:4321/auth/validate

curl -b cookies.txt -c cookies.txt -X POST \
  http://localhost:4321/auth/refresh
```

## Roles e permissões

Os seeders criam `admin`, `manager`, `agent` e `user`. Atualmente:

- `admin`: `user.read`, `user.create`, `user.update`, `user.delete` e `user.manage`, além de bypass no handler.
- `user`: `user.read`.
- `manager` e `agent`: criadas sem permissões associadas.

Uso em novos endpoints:

```csharp
[Authorize(Policy = Policies.UserCreate)]
[HttpPost]
public IActionResult Create()
{
    // Implementação da feature
}
```

Uma fallback policy exige autenticação em todo endpoint novo. Use `[AllowAnonymous]` apenas quando o acesso realmente for público.

## Componentes reutilizáveis

### Upload

`IFileValidator` e `IFileUploadService` estão registrados na injeção de dependência. O limite é 10 MiB, com suporte a PDF, JPG/JPEG, PNG, GIF, WebP, DOC/DOCX, XLS/XLSX e TXT. O serviço gera nomes com UUID, salva, lê e apaga arquivos, e bloqueia caminhos absolutos e path traversal. Não há controller de upload nesta versão.

### Validações

Os atributos `[CpfValidation]` e `[PhoneValidation]` podem ser usados em novos DTOs; ainda não aparecem nos DTOs expostos.

### Auditoria

O interceptor cria registros para entidades adicionadas, alteradas ou removidas, incluindo entidade, ID, ação, valores anteriores/novos, usuário, data e IP. Não há endpoint de consulta dos logs.

### Erros

Erros de domínio seguem este formato:

```json
{
  "statusCode": 409,
  "message": "Email já existe."
}
```

Falhas de Data Annotations retornam HTTP 400 com uma lista de mensagens. Exceções não tratadas retornam HTTP 500 com mensagem genérica.

## Estrutura

```text
Features/
├── Auth/                 # Controller, serviços, DTOs e refresh tokens
├── User/                 # Entidade, DTOs e serviço de cadastro
├── Roles/                # Roles
├── Permissions/          # Policies e handler de autorização
├── Relations/            # Usuário-role e role-permissão
├── Audit/                # Interceptor e logs de auditoria
├── Infrastructure/
│   ├── Data/             # DbContext, mapeamentos e migrations
│   ├── Email/            # Configurações de e-mail
│   ├── Encryption/       # AES-256 e SHA-256
│   ├── Jwt/              # Tokens
│   ├── Scheduling/       # Base para tarefas agendadas
│   └── Validation/       # CPF e telefone
└── Shared/
    ├── Exceptions/       # Exceções HTTP
    ├── Extensions/       # Configuração e startup
    ├── Middlewares/      # Tratamento de erros
    ├── Seeds/            # Roles, permissões e admin
    └── Upload/           # Armazenamento local
```

## Logs

O Serilog escreve no console e em `logs/log-AAAA-MM-DD.txt` (ou `LOG_PATH`), com retenção de 15 arquivos diários. O template inclui exceções em desenvolvimento.

## Evoluindo a base

1. Crie entidade, DTOs, serviço e controller em `Features/`.
2. Registre o serviço em `AddApplicationServices`.
3. Inclua policies em `Policies.AllPolicies` e associe-as em `Policies.RolePermissions`.
4. Use `[Authorize(Policy = ...)]` nos endpoints.
5. Gere e aplique a migration:

```bash
dotnet ef migrations add NomeDaAlteracao
dotnet ef database update
```

6. Adicione XML comments e `SwaggerOperation` para documentar os endpoints no Swagger.

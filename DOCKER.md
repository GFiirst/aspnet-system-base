# Execução com Docker

O projeto possui um `docker-compose.yml` que inicia a API e o PostgreSQL 18. Este é o fluxo recomendado para execução em containers.

## Pré-requisitos

- Docker com o plugin Compose (`docker compose`)
- Arquivo `.env` criado a partir do `.env.example`

## Configuração

Crie o arquivo local:

```bash
cp .env.example .env
```

As principais variáveis usadas pelo Compose são:

```env
# PostgreSQL
DB_NAME=system_base
DB_USER=postgres
DB_PASSWORD=troque-a-senha

# JWT
Jwt__AccessKey=uma-chave-de-acesso-longa-e-segura
Jwt__RefreshKey=uma-chave-de-refresh-longa-e-segura
Jwt__ResetPasswordKey=uma-chave-de-reset-longa-e-segura
Jwt__Issuer=AuthApi
Jwt__Audience=AuthApiUsers
Jwt__AccessExpirationMinutes=15
Jwt__ResetPasswordExpirationMinutes=15
Jwt__RefreshExpirationDays=30

# Aplicação
Encryption__Key=12345678901234567890123456789012
FileEncryption__Kek=<chave-de-32-bytes-em-base64>
Cors__AllowedOrigins=http://localhost:3000,http://localhost:5173
FRONTEND_URL=http://localhost:3000
APPLY_MIGRATIONS_ON_STARTUP=true
FORWARDED_HEADERS_KNOWN_PROXIES=172.18.0.1

# E-mail
MAIL_USER=
MAIL_PASS=

# Admin inicial opcional
ADMIN_NAME=
ADMIN_EMAIL=
ADMIN_PASSWORD=

# Persistência no host
HOST_POSTGRES_PATH=./postgres-data
HOST_UPLOAD_PATH=./uploads
HOST_LOG_PATH=./logs
```

`Encryption__Key` deve possuir exatamente 32 bytes em UTF-8. `FileEncryption__Kek` deve representar 32 bytes em Base64 e pode ser gerada com:

```bash
openssl rand -base64 32
```

As chaves JWT, a chave de dados e as credenciais não devem ser versionadas. Alterar `FileEncryption__Kek` sem recriptografar os arquivos torna o conteúdo existente ilegível.

No container, a conexão com o banco é montada automaticamente pelo Compose usando o hostname `postgres`; a variável `ConnectionStrings__DefaultConnection` do `.env` é usada apenas na execução local fora do Compose.

## Iniciar e parar

```bash
docker compose up -d --build
docker compose ps
docker compose logs -f aspnet-system-base
docker compose down
```

Para acompanhar também o banco:

```bash
docker compose logs -f
```

O Compose publica:

| Serviço | Endereço no host | Endereço entre containers |
|---|---|---|
| API | `http://localhost:5000` | `http://aspnet-system-base:5000` |
| PostgreSQL | `localhost:5433` | `postgres:5432` |

A API é vinculada somente a `127.0.0.1:5000`. Para exposição externa, use um proxy reverso e mantenha HTTPS na borda.

## Migrations e seeders

No Compose, `APPLY_MIGRATIONS_ON_STARTUP` usa `true` como padrão. A API aguarda o health check do PostgreSQL, aplica as migrations e então executa os seeders de roles, permissões e admin.

Para aplicar uma migration manualmente a partir do host, use a string de conexão local apontando para a porta `5433`:

```bash
dotnet ef database update \
  --connection 'Host=localhost;Port=5433;Database=system_base;Username=postgres;Password=troque-a-senha'
```

Normalmente isso não é necessário, pois a aplicação executa a inicialização ao subir.

## Persistência

O Compose utiliza bind mounts configuráveis:

- `${HOST_POSTGRES_PATH:-./postgres-data}` → dados do PostgreSQL;
- `${HOST_UPLOAD_PATH:-./uploads}` → arquivos enviados;
- `${HOST_LOG_PATH:-./logs}` → logs da aplicação.

`docker compose down` remove os containers e a rede, mas preserva esses diretórios. Como são bind mounts, `docker compose down -v` também não apaga os arquivos locais.

## Health checks

- API: `GET /health`, a cada 30 segundos no container.
- PostgreSQL: `pg_isready`, a cada 10 segundos.

```bash
docker inspect --format='{{.State.Health.Status}}' aspnet-system-base
docker inspect --format='{{.State.Health.Status}}' system-base-postgres
```

## Acesso ao PostgreSQL

```bash
docker compose exec postgres psql -U postgres -d system_base
```

Substitua usuário e banco pelos valores de `DB_USER` e `DB_NAME`.

## Scripts auxiliares

Os arquivos `run-db.sh` e `run-app.sh` iniciam containers separadamente e não usam a rede/configuração do Compose. O `run-app.sh` mantém nomes de variáveis legados, como `DB_CONNECTION_STRING`, `JWT_ACCESS_KEY` e `JWT_REFRESH_KEY`; por isso, prefira `docker compose` para obter a configuração completa atual.

O `run-db.sh` publica o PostgreSQL em `localhost:5432`, enquanto o Compose utiliza `localhost:5433`.

## Solução de problemas

### API falha com `relation "roles" does not exist`

As tabelas ainda não foram criadas. Confirme que `APPLY_MIGRATIONS_ON_STARTUP=true` e recrie a API:

```bash
docker compose up -d --build
docker compose logs -f aspnet-system-base
```

### API falha ao validar `FileEncryption:Kek`

Preencha `FileEncryption__Kek` no `.env` com uma chave Base64 que represente exatamente 32 bytes e recrie o container. A validação ocorre no startup mesmo que nenhuma rota de arquivo esteja exposta.

### Porta ocupada

```bash
sudo lsof -i :5000
sudo lsof -i :5433
```

### Ver configuração recebida pelo container

O comando abaixo pode exibir segredos; use apenas em ambiente controlado:

```bash
docker compose exec aspnet-system-base env
```

### Recriar o banco de desenvolvimento

Pare os containers e remova explicitamente o diretório definido em `HOST_POSTGRES_PATH`. Essa operação apaga todos os dados e deve ser usada somente em um banco descartável.

## Produção

- Use um gerenciador de secrets em vez de armazenar credenciais no `.env`.
- Fixe versões das imagens e mantenha .NET/PostgreSQL atualizados.
- Configure backup do diretório de dados do PostgreSQL.
- Use proxy reverso com HTTPS e configure `ForwardedHeaders__KnownProxies` para o endereço real do proxy.
- Defina limites de CPU/memória, monitoramento e alertas.
- Avalie aplicar migrations em uma etapa controlada do deploy em vez do startup.

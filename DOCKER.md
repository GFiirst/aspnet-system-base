# Docker Setup

Este projeto inclui configurações Docker para rodar a aplicação .NET e o banco de dados PostgreSQL separadamente ou juntos via docker-compose.

## Pré-requisitos

- Docker instalado
- Docker Compose instalado (opcional, para orquestração)
- Arquivo `.env` configurado com as variáveis de ambiente

## Variáveis de Ambiente

Crie um arquivo `.env` na raiz do projeto com as seguintes variáveis:

```env
# Database
DB_USER=seu_usuario
DB_PASSWORD=sua_senha
DB_NAME=nome_do_banco
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=nome_do_banco;Username=seu_usuario;Password=sua_senha

# JWT
JWT_ACCESS_KEY=sua_chave_access
JWT_REFRESH_KEY=sua_chave_refresh
JWT_ISSUER=AuthApi
JWT_AUDIENCE=AuthApiUsers
JWT_ACCESS_EXPIRATION_MINUTES=15
JWT_REFRESH_EXPIRATION_DAYS=30

# CORS
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173

# File Upload
FILE_PATH=/app/uploads

# Email
MAIL_USER=seu_email
MAIL_PASS=sua_senha

# Frontend
FRONTEND_URL=http://localhost:3000
```

## Opções de Execução

### 1. Executar Separadamente

#### Banco de Dados PostgreSQL

```bash
./run-db.sh
```

Este script:
- Cria e inicia o container PostgreSQL 18
- Configura volume para persistência de dados
- Exibe a string de conexão

#### Aplicação .NET

```bash
./run-app.sh
```

Este script:
- Builda a imagem Docker da aplicação
- Inicia o container na porta 5000
- Configura volumes para uploads e logs
- Carrega variáveis de ambiente do arquivo `.env`

### 2. Executar com Docker Compose

Para rodar ambos os containers juntos:

```bash
# Iniciar ambos os containers
docker-compose up -d

# Ver logs
docker-compose logs -f

# Parar containers
docker-compose down

# Parar e remover volumes. pfv evite esse
docker-compose down -v
```

## Comandos Úteis

### Verificar status dos containers

```bash
docker ps
```

### Ver logs de um container específico

```bash
# Aplicação
docker logs -f aspnet-system-base

# Banco de dados
docker logs -f system-base-postgres
```

### Parar containers

```bash
# Parar aplicação
docker stop aspnet-system-base

# Parar banco de dados
docker stop system-base-postgres
```

### Remover containers

```bash
# Remover aplicação
docker rm aspnet-system-base

# Remover banco de dados
docker rm system-base-postgres
```

### Acessar o banco de dados

```bash
docker exec -it system-base-postgres psql -U seu_usuario -d nome_do_banco
```

### Reconstruir a imagem da aplicação

```bash
docker build -t aspnet-system-base:latest .
docker stop aspnet-system-base
docker rm aspnet-system-base
./run-app.sh
```

## Volumes

Os seguintes volumes Docker são criados para persistência:

- `postgres_data`: Dados do PostgreSQL
- `uploads`: Arquivos enviados pela aplicação
- `logs`: Logs da aplicação

## Health Checks

- **Aplicação**: Health check configurado em `/health` a cada 30s
- **PostgreSQL**: Health check usando `pg_isready` a cada 10s

## Portas

- **Aplicação**: 5000
- **PostgreSQL**: 5432

## Troubleshooting

### Container não inicia

Verifique se as portas já estão em uso:

```bash
# Verificar portas em uso
sudo lsof -i :5000
sudo lsof -i :5432
```

### Erro de conexão com banco

Certifique-se de que:
1. O container PostgreSQL está rodando
2. A string de conexão está correta no `.env`
3. Os containers estão na mesma rede (se usando docker-compose)

### Permissões negadas nos scripts

```bash
chmod +x run-app.sh run-db.sh
```

### Verificar variáveis de ambiente no container

```bash
docker exec aspnet-system-base env
```

## Produção

Para produção, considere:

1. Usar secrets do Docker Swarm ou Kubernetes para variáveis sensíveis
2. Configurar backup automático do volume `postgres_data`
3. Usar imagem específica de PostgreSQL (ex: `postgres:18.4` em vez de `postgres:18`)
4. Configurar resource limits no docker-compose
5. Usar HTTPS reverso proxy (nginx/traefik)
6. Configurar monitoramento e alertas

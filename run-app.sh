#!/bin/bash

# Script para rodar o container da aplicação .NET separadamente

# Carregar variáveis de ambiente do arquivo .env se existir
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Nome do container e imagem
CONTAINER_NAME="aspnet-system-base"
IMAGE_NAME="aspnet-system-base:latest"

# Verificar se o container já está rodando
if [ "$(docker ps -q -f name=$CONTAINER_NAME)" ]; then
    echo "Container $CONTAINER_NAME já está rodando."
    echo "Para parar: docker stop $CONTAINER_NAME"
    echo "Para reiniciar: docker restart $CONTAINER_NAME"
    exit 0
fi

# Verificar se o container existe mas está parado
if [ "$(docker ps -aq -f name=$CONTAINER_NAME)" ]; then
    echo "Container $CONTAINER_NAME existe mas está parado. Iniciando..."
    docker start $CONTAINER_NAME
    exit 0
fi

# Build da imagem
echo "Building Docker image..."
docker build -t $IMAGE_NAME .

# Verificar variáveis de ambiente obrigatórias
if [ -z "$DB_CONNECTION_STRING" ]; then
    echo "AVISO: DB_CONNECTION_STRING não está definido. A aplicação pode não funcionar corretamente."
fi

if [ -z "$JWT_ACCESS_KEY" ]; then
    echo "AVISO: JWT_ACCESS_KEY não está definido. A autenticação pode não funcionar."
fi

# Criar volumes se não existirem
docker volume create uploads 2>/dev/null
docker volume create logs 2>/dev/null

# Rodar o container
echo "Starting container $CONTAINER_NAME..."
docker run -d \
    --name $CONTAINER_NAME \
    -p 5000:5000 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ASPNETCORE_URLS=http://+:5000 \
    -e ConnectionStrings__DefaultConnection="${DB_CONNECTION_STRING}" \
    -e Jwt__AccessKey="${JWT_ACCESS_KEY}" \
    -e Jwt__RefreshKey="${JWT_REFRESH_KEY}" \
    -e Jwt__Issuer="${JWT_ISSUER:-AuthApi}" \
    -e Jwt__Audience="${JWT_AUDIENCE:-AuthApiUsers}" \
    -e Jwt__AccessExpirationMinutes="${JWT_ACCESS_EXPIRATION_MINUTES:-15}" \
    -e Jwt__RefreshExpirationDays="${JWT_REFRESH_EXPIRATION_DAYS:-30}" \
    -e Cors__AllowedOrigins="${CORS_ALLOWED_ORIGINS}" \
    -e FILE_PATH="${FILE_PATH:-/app/uploads}" \
    -e MAIL_USER="${MAIL_USER}" \
    -e MAIL_PASS="${MAIL_PASS}" \
    -e FRONTEND_URL="${FRONTEND_URL}" \
    -v uploads:/app/uploads \
    -v logs:/app/logs \
    --restart unless-stopped \
    $IMAGE_NAME

echo "Container $CONTAINER_NAME iniciado com sucesso!"
echo "A aplicação está disponível em: http://localhost:5000"
echo "Para ver os logs: docker logs -f $CONTAINER_NAME"
echo "Para parar: docker stop $CONTAINER_NAME"

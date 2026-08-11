#!/bin/bash

# Script para rodar o container PostgreSQL separadamente

# Carregar variáveis de ambiente do arquivo .env se existir
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Nome do container e imagem
CONTAINER_NAME="system-base-postgres"
IMAGE_NAME="postgres:18"

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

# Verificar variáveis de ambiente obrigatórias
if [ -z "$DB_USER" ]; then
    echo "ERRO: DB_USER não está definido. Defina no arquivo .env"
    exit 1
fi

if [ -z "$DB_PASSWORD" ]; then
    echo "ERRO: DB_PASSWORD não está definido. Defina no arquivo .env"
    exit 1
fi

if [ -z "$DB_NAME" ]; then
    echo "ERRO: DB_NAME não está definido. Defina no arquivo .env"
    exit 1
fi

# Criar volume se não existir
docker volume create postgres_data 2>/dev/null

# Rodar o container
echo "Starting container $CONTAINER_NAME..."
docker run -d \
    --name $CONTAINER_NAME \
    -p 5432:5432 \
    -e POSTGRES_USER="${DB_USER}" \
    -e POSTGRES_PASSWORD="${DB_PASSWORD}" \
    -e POSTGRES_DB="${DB_NAME}" \
    -v postgres_data:/var/lib/postgresql/data \
    --restart unless-stopped \
    $IMAGE_NAME

echo "Container $CONTAINER_NAME iniciado com sucesso!"
echo "PostgreSQL está disponível em: localhost:5432"
echo "Database: ${DB_NAME}"
echo "User: ${DB_USER}"
echo "Para ver os logs: docker logs -f $CONTAINER_NAME"
echo "Para parar: docker stop $CONTAINER_NAME"
echo ""
echo "String de conexão:"
echo "Host=localhost;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

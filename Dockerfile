# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Set Docker build flag to skip husky
ARG DOCKER_BUILD=true
ENV DOCKER_BUILD=$DOCKER_BUILD

# Copy csproj and restore dependencies
COPY ["aspnet-system-base.csproj", "./"]
RUN dotnet restore "aspnet-system-base.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "aspnet-system-base.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install necessary dependencies for PostgreSQL
RUN apt-get update && apt-get install -y \
    libpq-dev \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create uploads directory
RUN mkdir -p /app/uploads /app/logs

# Copy published app from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Set environment to production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "aspnet-system-base.dll"]

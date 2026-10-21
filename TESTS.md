# Testes automatizados do ASP.NET System Base

A suíte usa xUnit no projeto [`Tests/AspnetSystemBase.Tests.csproj`](Tests/AspnetSystemBase.Tests.csproj) e combina testes unitários com testes de integração HTTP sobre a aplicação e o PostgreSQL reais.

## Pré-requisitos

- .NET 10 SDK;
- Docker Engine em execução e acessível pelo usuário para os testes de integração;
- dependências restauradas pelo `dotnet restore`.

O PostgreSQL local e o `docker-compose.yml` da aplicação não são usados pela suíte. Não é necessário criar `.env`, banco ou usuário de testes.

## Executar

Toda a suíte:

```bash
dotnet test Tests/AspnetSystemBase.Tests.csproj
```

Saída detalhada por caso:

```bash
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --logger "console;verbosity=normal"
```

Depois de um build `Release` já validado:

```bash
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --configuration Release \
  --no-restore \
  --no-build
```

Gerar resultado TRX:

```bash
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory TestResults
```

Coletar cobertura com o Coverlet já configurado:

```bash
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults
```

## Filtros úteis

O filtro considera o namespace e o nome totalmente qualificado:

```bash
# Testes unitários, sem aplicação ou banco
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Unit"

# Autenticação
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Auth"

# Upload, validação e criptografia de arquivos
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Upload"

# Auditoria e sanitização de dados sensíveis
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Audit"

# Registro do OpenTelemetry
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Observability"

# Um método específico
dotnet test Tests/AspnetSystemBase.Tests.csproj \
  --filter "FullyQualifiedName~Validador_deve_rejeitar_pdf_sem_assinatura_real"
```

## Arquitetura da suíte

### Testes unitários

Validam componentes isolados sem iniciar a aplicação ou o banco. A cobertura atual inclui:

- envelope encryption AES-256-GCM, aleatoriedade e detecção de adulteração;
- persistência criptografada e leitura descriptografada;
- validação conjunta de extensão, MIME e assinatura real de arquivos;
- estrutura mínima de documentos Open XML;
- rejeição de conteúdo binário disfarçado de texto;
- proteção contra path traversal no armazenamento;
- sanitização recursiva de senhas, tokens, segredos, credenciais, hashes e valores criptografados em JSON;
- registro condicional de traces e métricas e validação do protocolo OTLP dos logs.

### Testes de integração

Exercitam a API por HTTP com `WebApplicationFactory<Program>`, incluindo middlewares, autenticação por cookies, controllers, serviços, EF Core, migrations, PostgreSQL e criptografia.

A infraestrutura compartilhada fica em `Tests/Infrastructure/`:

| Componente | Responsabilidade |
|---|---|
| `ApiFactory` | Inicia a API em `Testing`, cria o PostgreSQL descartável e injeta configurações de teste |
| `IntegrationTestBase` | Fornece o `HttpClient` e restaura o estado antes de cada teste |
| `AuthTestHelper` | Cria usuários, autentica clientes e executa operações auxiliares no banco |

## Isolamento

- Cada classe de integração recebe sua própria `ApiFactory` e seu próprio PostgreSQL criado pelo Testcontainers.
- As migrations e os seeders são executados na inicialização da aplicação de teste.
- Antes de cada caso, o Respawn limpa os dados e preserva migrations, roles, permissões e vínculos role–permissão.
- Uploads e logs usam um diretório temporário exclusivo da factory.
- O diretório de uploads é recriado antes de cada teste e removido ao descartar a factory.
- A configuração é injetada em memória; segredos reais e o `.env` local não são lidos em `Testing`.

Como o rate limit permanece em memória durante a vida da `ApiFactory`, evite concentrar mais de dez chamadas ao mesmo endpoint limitado dentro de uma única classe de integração.

## Cobertura funcional

| Área | O que é verificado |
|---|---|
| Health | Inicialização e resposta do health check |
| Autenticação | Login, cookies, refresh, logout, recuperação de senha, sanitização dos logs do Serilog, revogação, limite de sessões e rate limit |
| Arquivos | Validação do conteúdo, criptografia em repouso, leitura, adulteração e path traversal |
| Auditoria | Sanitização isolada e persistência de logs sem dados sensíveis |
| Observabilidade | Ativação condicional de traces, métricas e logs OTLP e rejeição de protocolo inválido |

## VS Code

Com a extensão C# Dev Kit instalada, os casos aparecem no painel **Testing** e podem ser executados ou depurados individualmente. Se o projeto não for descoberto:

```bash
dotnet restore Tests/AspnetSystemBase.Tests.csproj
dotnet test Tests/AspnetSystemBase.Tests.csproj --list-tests
```

Depois, execute **Developer: Reload Window** no VS Code.

## CI/CD

O job `tests` em [`.github/workflows/main.yml`](.github/workflows/main.yml) restaura, compila em `Release`, executa a suíte e publica o resultado TRX por 14 dias. Pull requests executam somente a validação; em pushes para `main`, a preparação e o deploy dependem do sucesso dos testes.

O runner hospedado do GitHub já possui Docker, então o Testcontainers inicia o PostgreSQL sem declarar um service container no workflow.

## Solução de problemas

### Erro ao conectar no Docker

Verifique o daemon e as permissões do socket:

```bash
docker info
docker ps
```

No Linux, o usuário precisa acessar `/var/run/docker.sock`.

### Primeira execução mais lenta

O Testcontainers pode precisar baixar `postgres:18`. Execuções posteriores reutilizam a imagem local, mas continuam criando bancos descartáveis.

### Estado vazando entre testes

Novos testes de integração devem herdar de `IntegrationTestBase`. Uma factory criada manualmente sem `ResetDatabaseAsync` pode manter dados e arquivos entre os casos.

### Teste passa sozinho e falha com a classe inteira

Confira primeiro o rate limit, particionado por IP, método e rota e preservado durante a vida da `ApiFactory`. Verifique também valores únicos criados pelo cenário.

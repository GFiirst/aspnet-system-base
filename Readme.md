**Iniciar Variaveis de ambiente**

dotnet user-secrets init

**Listar Variaveis de ambiente**

dotnet user-secrets list

**Setar Variaveis de ambiente**

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=aspnet_crud_estudos;Username=postgres;Password=suasenha"

**Gerar migration**

dotnet ef migrations add InitialCreatedotnet

**Rodar migration**

dotnet ef database update

**Instalar JWT**
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

**Colocar senha no secrets** A SENHA TEM Q SER LONGA SE NAO DA ERRO - O HS256 EGIXE 32 BITS DE SENHA
dotnet user-secrets set "Jwt:AccessKey" "SENHA_SUPER_EXTRA_FORTE_LONGA_DE_MAIS"
dotnet user-secrets set "Jwt:RefreshKey" "SENHA_SUPER_MEGA_FORTE_LONGA_LONGA_DE_MAIS"

**Adicionar URL Cors**
dotnet user-secrets set "Cors:AllowedOrigins" "https://seusite.com"
**Iniciar Variaveis de ambiente**

dotnet user-secrets init

**Listar Variaveis de ambiente**

dotnet user-secrets list

**Setar Variaveis de ambiente**

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=aspnet_crud_estudos;Username=postgres;Password=suasenha"

**Gerar migration**

dotnet ef migrations add InitialCreatedotnet tool restore

**Rodar migration**

dotnet ef database update

**Instalar JWT**
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

**Colocar senha no secrets**
dotnet user-secrets set "JWT:SECRET" "SENHA_SUPER_FORTE"
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddApplicationServices();

builder.Services.AddApiValidation();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(Policies.ConfigurePolicies);
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

var app = builder.Build();

await app.SeedDatabaseAsync();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
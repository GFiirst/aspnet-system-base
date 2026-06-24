var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddApplicationServices();

builder.Services.AddApiValidation();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
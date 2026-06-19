using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
);

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState
            .Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();

        return new BadRequestObjectResult(firstError);
    };
});

var app = builder.Build();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
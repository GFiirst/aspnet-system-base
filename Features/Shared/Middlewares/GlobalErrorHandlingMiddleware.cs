using Serilog;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            Log.Warning(ex, "Exceção da aplicação: {Message}", ex.Message);

            await WriteErrorAsync(
                context,
                ex.StatusCode,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro não tratado: {Message}", ex.Message);

            await WriteErrorAsync(
                context,
                500,
                "Erro interno do servidor"
            );
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message
    )
    {
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            new
            {
                statusCode,
                message
            }
        );
    }

}

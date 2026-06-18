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
            await WriteErrorAsync(
                context,
                ex.StatusCode,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

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
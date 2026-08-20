public abstract class BaseScheduledService : BackgroundService
{
    private readonly ILogger _logger;

    protected BaseScheduledService(ILogger logger)
    {
        _logger = logger;
    }

    protected virtual bool ExecuteOnStartup => true;
    protected virtual TimeOnly ExecutionTimeUtc => new(4, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço agendado {ServiceName} iniciado", GetType().Name);

        if (ExecuteOnStartup)
        {
            await ExecuteSafelyAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetNextExecutionUtc() - DateTime.UtcNow;
            _logger.LogInformation(
                "Próxima execução de {ServiceName} em {Delay}",
                GetType().Name,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await ExecuteSafelyAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Serviço agendado {ServiceName} finalizado", GetType().Name);
    }

    protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);

    private async Task ExecuteSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ExecuteTaskAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar o serviço agendado {ServiceName}", GetType().Name);
        }
    }

    private DateTime GetNextExecutionUtc()
    {
        var now = DateTime.UtcNow;
        var nextExecution = now.Date.Add(ExecutionTimeUtc.ToTimeSpan());
        return nextExecution <= now ? nextExecution.AddDays(1) : nextExecution;
    }
}

namespace AspnetSystemBase.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<ApiFactory>, IAsyncLifetime
{
    protected IntegrationTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected ApiFactory Factory { get; }

    protected HttpClient Client { get; }

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}

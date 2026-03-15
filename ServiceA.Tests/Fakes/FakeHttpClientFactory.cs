namespace ServiceA.Tests.Fakes;

/// <summary>
/// Substitui IHttpClientFactory no DI de testes.
/// Retorna HttpClients que usam o FakeServiceBMessageHandler,
/// evitando chamadas reais de rede ao ServiceB.
/// </summary>
public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler);
}

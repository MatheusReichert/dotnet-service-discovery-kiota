using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Shared.Infrastructure;

/// <summary>
/// Classe base para factories que combinam Descoberta Automática + Kiota Client
/// </summary>
public abstract class KiotaClientFactoryBase<TClient> where TClient : class
{
    protected readonly IKubernetesServiceDiscovery K8sDiscovery;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IConfiguration Configuration;

    private string? _cachedBaseUrl;

    protected KiotaClientFactoryBase(
        IKubernetesServiceDiscovery k8sDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        K8sDiscovery = k8sDiscovery;
        HttpClientFactory = httpClientFactory;
        Configuration = configuration;
    }

    /// <summary>
    /// Label do tipo de API para descoberta (ex: "products-api")
    /// </summary>
    protected abstract string ApiType { get; }

    /// <summary>
    /// Chave de configuração para fallback (ex: "Services:ServiceB:Url")
    /// </summary>
    protected abstract string ConfigurationKey { get; }

    /// <summary>
    /// URL padrão de fallback (ex: "http://serviceb")
    /// </summary>
    protected abstract string DefaultUrl { get; }

    /// <summary>
    /// Cria a instância do cliente Kiota com o adapter configurado
    /// </summary>
    protected abstract TClient CreateClient(HttpClientRequestAdapter adapter);

    /// <summary>
    /// Cria cliente Kiota com URL descoberta automaticamente.
    /// A URL é resolvida apenas na primeira chamada e reutilizada nas demais
    /// (a factory deve ser registrada como Singleton no DI).
    /// </summary>
    public async Task<TClient> CreateClientAsync()
    {
        // 1. Resolve URL apenas uma vez — sem consulta ao K8s por request
        _cachedBaseUrl ??= await ResolveBaseUrlAsync();

        // 2. Criar HttpClient configurado (fresh por chamada — evita problemas de DNS/pool)
        var httpClient = HttpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_cachedBaseUrl);

        // 3. Configurar Kiota adapter
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

        // 4. Retornar cliente Kiota type-safe
        return CreateClient(adapter);
    }

    private async Task<string> ResolveBaseUrlAsync()
    {
        // Fallback hierárquico: K8s → Config → Default
        var discoveredUrl = await K8sDiscovery.DiscoverServiceUrlAsync(ApiType);
        return discoveredUrl
            ?? Configuration[ConfigurationKey]
            ?? DefaultUrl;
    }
}

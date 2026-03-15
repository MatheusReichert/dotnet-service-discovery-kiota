using Shared.Infrastructure;

namespace ServiceA.Tests.Fakes;

/// <summary>
/// Simula ambiente fora do Kubernetes — DiscoverServiceUrlAsync retorna null,
/// fazendo o código cair no fallback de configuração.
/// </summary>
public class FakeKubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    public Task<string?> DiscoverServiceUrlAsync(string apiType) =>
        Task.FromResult<string?>(null);

    public Task<Dictionary<string, string>> DiscoverAllServicesAsync() =>
        Task.FromResult(new Dictionary<string, string>());
}

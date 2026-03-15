using System.Net;

namespace ServiceA.Tests;

/// <summary>
/// Testes de integração para os endpoints que chamam o ServiceB.
///
/// Como funciona o mock:
///   - IKubernetesServiceDiscovery → retorna null (sem K8s)
///   - IHttpClientFactory → retorna HttpClient com FakeServiceBMessageHandler
///   - FakeServiceBMessageHandler → devolve [Laptop, Mouse] para qualquer request
///
/// Isso testa que ServiceA processa corretamente a resposta do ServiceB,
/// sem depender de rede real ou do ServiceB estar rodando.
/// </summary>
public class CrossServiceTests : IClassFixture<ServiceAWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CrossServiceTests(ServiceAWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---------- /api/users/with-products/{id} (HTTP manual) ----------

    [Fact]
    public async Task GetUserWithProducts_ReturnsOk_WithProductsFromServiceB()
    {
        var response = await _client.GetAsync("/api/users/with-products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Laptop", body);
    }

    [Fact]
    public async Task GetUserWithProducts_ResponseContains_DiscoveredUrl()
    {
        var response = await _client.GetAsync("/api/users/with-products/1");
        var body = await response.Content.ReadAsStringAsync();

        // Fallback URL esperada quando K8s retorna null e config não está definida
        Assert.Contains("http://serviceb", body);
    }

    // ---------- /api/users/with-products-typed/{id} (Kiota type-safe) ----------

    [Fact]
    public async Task GetUserWithProductsTyped_ReturnsOk_WithProductsFromServiceB()
    {
        var response = await _client.GetAsync("/api/users/with-products-typed/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Laptop", body);
    }

    [Fact]
    public async Task GetUserWithProductsTyped_ResponseContains_KiotaMetadata()
    {
        var response = await _client.GetAsync("/api/users/with-products-typed/1");
        var body = await response.Content.ReadAsStringAsync();

        // Mensagem definida no endpoint para identificar o caminho Kiota
        Assert.Contains("Kiota", body);
    }

    // ---------- /api/services/catalog ----------

    [Fact]
    public async Task GetServiceCatalog_ReturnsOk_WithEmptyCatalog_WhenNotInKubernetes()
    {
        var response = await _client.GetAsync("/api/services/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("catalog", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"count\":0", body.Replace(" ", ""));
    }
}

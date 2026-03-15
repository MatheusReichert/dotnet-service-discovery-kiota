using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceA.Tests.Fakes;
using Shared.Infrastructure;

namespace ServiceA.Tests;

/// <summary>
/// Factory compartilhada entre os testes de integração do ServiceA.
///
/// O que esta factory faz:
///   1. Substitui IKubernetesServiceDiscovery por um fake que retorna null
///      (simula "não estou no cluster K8s").
///   2. Substitui IHttpClientFactory por um fake que devolve clientes com
///      FakeServiceBMessageHandler, interceptando toda chamada HTTP ao ServiceB.
///
/// Com isso, os testes rodam sem Kubernetes, sem ServiceB real e sem rede.
/// </summary>
public class ServiceAWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Substitui K8s discovery — retorna null (sem cluster)
            services.RemoveAll<IKubernetesServiceDiscovery>();
            services.AddSingleton<IKubernetesServiceDiscovery, FakeKubernetesServiceDiscovery>();

            // Substitui IHttpClientFactory — intercepta chamadas ao ServiceB
            var fakeHandler = new FakeServiceBMessageHandler();
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(fakeHandler));
        });
    }
}

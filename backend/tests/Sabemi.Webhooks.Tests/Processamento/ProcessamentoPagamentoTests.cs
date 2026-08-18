using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Tests.Infraestrutura;

namespace Sabemi.Webhooks.Tests.Processamento;

public sealed class ProcessamentoPagamentoTests(SabemiWebApplicationFactory factory)
    : IClassFixture<SabemiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Deve_responder_rapido_e_processar_em_background_apos_o_delay_simulado()
    {
        var idTransacao = Guid.NewGuid().ToString();
        var idContrato = Guid.NewGuid().ToString();
        var payload = PayloadTestHelper.CriarPayloadValido(idTransacao: idTransacao, idContrato: idContrato, status: "Sucesso");

        var cronometroEnvio = Stopwatch.StartNew();
        var respostaEnvio = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload));
        cronometroEnvio.Stop();

        Assert.Equal(HttpStatusCode.Accepted, respostaEnvio.StatusCode);
        // O endpoint responde antes do "processamento pesado" (2s) rodar em background.
        // Margem de 1,8s (não 1s) para absorver o cold-start da 1ª requisição (JIT, pool de conexões)
        // sem tornar o teste instável, mantendo a prova de que não se espera pelo processamento completo.
        Assert.True(cronometroEnvio.Elapsed < TimeSpan.FromSeconds(1.8), $"Resposta demorou {cronometroEnvio.Elapsed}, deveria ser bem mais rápida que os ~2s do processamento em background.");

        var pagamentoLogoAposEnvio = await BuscarPagamentoPorIdTransacaoAsync(idTransacao);
        Assert.NotEqual("Processado", pagamentoLogoAposEnvio?.StatusProcessamento);

        var pagamentoFinal = await AguardarStatusAsync(idTransacao, "Processado", TimeSpan.FromSeconds(10));

        Assert.NotNull(pagamentoFinal);
        Assert.Equal("Processado", pagamentoFinal!.StatusProcessamento);
        Assert.Equal("Sucesso", pagamentoFinal.StatusPagamentoBanco);
        Assert.NotNull(pagamentoFinal.ProcessadoEm);
    }

    private async Task<PagamentoResumoResponse?> BuscarPagamentoPorIdTransacaoAsync(string idTransacao)
    {
        var resultado = await _client.GetFromJsonAsync<PagamentosPaginadosResponse>("/api/pagamentos?tamanhoPagina=200");
        return resultado?.Itens.FirstOrDefault(p => p.IdTransacao == idTransacao);
    }

    private async Task<PagamentoResumoResponse?> AguardarStatusAsync(string idTransacao, string statusEsperado, TimeSpan tempoLimite)
    {
        var cronometro = Stopwatch.StartNew();
        while (cronometro.Elapsed < tempoLimite)
        {
            var pagamento = await BuscarPagamentoPorIdTransacaoAsync(idTransacao);
            if (pagamento?.StatusProcessamento == statusEsperado)
            {
                return pagamento;
            }

            await Task.Delay(250);
        }

        return await BuscarPagamentoPorIdTransacaoAsync(idTransacao);
    }
}

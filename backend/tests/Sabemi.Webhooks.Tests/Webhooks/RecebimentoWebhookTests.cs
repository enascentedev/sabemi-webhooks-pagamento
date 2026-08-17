using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Tests.Infraestrutura;

namespace Sabemi.Webhooks.Tests.Webhooks;

public sealed class RecebimentoWebhookTests(SabemiWebApplicationFactory factory)
    : IClassFixture<SabemiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Deve_retornar_401_quando_api_key_invalida()
    {
        var payload = PayloadTestHelper.CriarPayloadValido();
        var requisicao = AssinaturaTestHelper.CriarRequisicaoWebhook(payload, apiKey: "chave-errada");

        var resposta = await _client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Deve_retornar_401_quando_assinatura_invalida()
    {
        var payload = PayloadTestHelper.CriarPayloadValido();
        var requisicao = AssinaturaTestHelper.CriarRequisicaoWebhook(payload, assinatura: "assinatura-incorreta");

        var resposta = await _client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Deve_aceitar_payload_valido_e_retornar_202()
    {
        var payload = PayloadTestHelper.CriarPayloadValido();
        var requisicao = AssinaturaTestHelper.CriarRequisicaoWebhook(payload);

        var resposta = await _client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<WebhookPagamentoResponse>();
        Assert.NotNull(corpo);
        Assert.Equal("Recebido", corpo!.Situacao);
    }

    [Fact]
    public async Task Deve_marcar_como_invalido_quando_payload_falha_validacao()
    {
        var payloadInvalido = JsonSerializer.Serialize(new { id_transacao = Guid.NewGuid().ToString() });
        var requisicao = AssinaturaTestHelper.CriarRequisicaoWebhook(payloadInvalido);

        var resposta = await _client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<WebhookPagamentoResponse>();
        Assert.NotNull(corpo);
        Assert.Equal("Invalido", corpo!.Situacao);
    }

    [Fact]
    public async Task Deve_detectar_duplicidade_para_mesmo_id_transacao()
    {
        var payload = PayloadTestHelper.CriarPayloadValido();

        var primeiraResposta = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload));
        var segundaResposta = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload));

        Assert.Equal(HttpStatusCode.Accepted, primeiraResposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segundaResposta.StatusCode);

        var corpoSegunda = await segundaResposta.Content.ReadFromJsonAsync<WebhookPagamentoResponse>();
        Assert.Equal("Duplicado", corpoSegunda!.Situacao);
    }

    [Fact]
    public async Task Deve_processar_apenas_uma_vez_quando_requisicoes_concorrentes_com_mesmo_id_transacao()
    {
        var payload = PayloadTestHelper.CriarPayloadValido();

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload))));

        var aceitas = respostas.Count(r => r.StatusCode == HttpStatusCode.Accepted);
        var duplicadas = respostas.Count(r => r.StatusCode == HttpStatusCode.OK);

        Assert.Equal(1, aceitas);
        Assert.Equal(4, duplicadas);
    }
}

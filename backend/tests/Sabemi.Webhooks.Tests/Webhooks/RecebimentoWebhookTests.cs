using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Tests.Infraestrutura;

namespace Sabemi.Webhooks.Tests.Webhooks;

[Collection(ApiCollection.Nome)]
public sealed class RecebimentoWebhookTests(SabemiWebApplicationFactory factory)
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

    // A coluna data_pagamento é timestamptz e o Npgsql rejeita DateTime que não seja Kind=Utc.
    // Sem a normalização na borda, os dois últimos formatos derrubariam o SaveChanges e o evento
    // não chegaria nem a ser registrado no log bruto.
    [Theory]
    [InlineData("2026-08-17T12:00:00Z")]
    [InlineData("2026-08-17T12:00:00")]
    [InlineData("2026-08-17T12:00:00-03:00")]
    public async Task Deve_aceitar_data_pagamento_em_qualquer_formato_de_fuso(string dataPagamento)
    {
        var payload = PayloadTestHelper.CriarPayloadValido(dataPagamento: dataPagamento);

        var resposta = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload));

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
    }

    [Fact]
    public async Task Deve_converter_data_pagamento_com_offset_para_o_instante_em_utc()
    {
        var idTransacao = Guid.NewGuid().ToString();
        var payload = PayloadTestHelper.CriarPayloadValido(
            idTransacao: idTransacao,
            dataPagamento: "2026-08-17T12:00:00-03:00");

        var resposta = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payload));
        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);

        var listagem = await _client.GetFromJsonAsync<PagamentosPaginadosResponse>("/api/pagamentos?tamanhoPagina=200");
        var pagamento = listagem?.Itens.FirstOrDefault(p => p.IdTransacao == idTransacao);

        Assert.NotNull(pagamento);
        // 12:00 em UTC-3 é o mesmo instante que 15:00Z: prova que houve conversão, e não apenas
        // troca do Kind do DateTime.
        Assert.Equal(
            new DateTime(2026, 8, 17, 15, 0, 0, DateTimeKind.Utc),
            pagamento!.DataPagamento!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task Deve_identificar_o_campo_quando_o_valor_nao_converte()
    {
        var payloadComValorTextual = """
            {"id_transacao":"tx-formato","id_contrato":"c-formato","valor":"abc","data_pagamento":"2026-08-17T12:00:00Z","status":"Sucesso"}
            """;

        var resposta = await _client.SendAsync(AssinaturaTestHelper.CriarRequisicaoWebhook(payloadComValorTextual));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<WebhookPagamentoResponse>();
        Assert.Contains("valor", corpo!.Mensagem, StringComparison.OrdinalIgnoreCase);
    }
}

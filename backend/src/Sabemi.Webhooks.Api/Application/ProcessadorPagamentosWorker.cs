using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Api.Domain;
using Sabemi.Webhooks.Api.Infrastructure;

namespace Sabemi.Webhooks.Api.Application;

/// <summary>
/// Consome a fila em background e aplica a regra de negócio (simulada como pesada,
/// ~2s) fora do ciclo de requisição, atualizando o evento bruto e o status do contrato.
/// Cria um escopo de DI por item processado, já que o worker é singleton e o
/// DbContext é scoped.
/// </summary>
public sealed class ProcessadorPagamentosWorker(
    IServiceProvider servicos,
    IFilaProcessamentoPagamentos fila,
    ILogger<ProcessadorPagamentosWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DuracaoSimuladaDoProcessamento = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid eventoId;
            try
            {
                eventoId = await fila.AguardarProximoAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessarAsync(eventoId, stoppingToken);
        }
    }

    private async Task ProcessarAsync(Guid eventoId, CancellationToken stoppingToken)
    {
        using var escopo = servicos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        var evento = await db.EventosWebhookBrutos.FirstOrDefaultAsync(e => e.Id == eventoId, stoppingToken);
        if (evento is null)
        {
            logger.LogWarning("Evento {EventoId} não encontrado ao tentar processar.", eventoId);
            return;
        }

        evento.MarcarComoProcessando();
        await db.SaveChangesAsync(stoppingToken);

        try
        {
            await Task.Delay(DuracaoSimuladaDoProcessamento, stoppingToken);

            var situacao = string.Equals(evento.StatusPagamentoBanco, "Sucesso", StringComparison.OrdinalIgnoreCase)
                ? SituacaoContrato.Sucesso
                : SituacaoContrato.Erro;

            var contrato = await db.StatusContratos.FirstOrDefaultAsync(c => c.IdContrato == evento.IdContrato, stoppingToken);
            if (contrato is null)
            {
                contrato = StatusContrato.CriarComPagamento(
                    evento.IdContrato!, evento.Valor!.Value, evento.IdTransacao!, evento.DataPagamento!.Value, situacao);
                db.StatusContratos.Add(contrato);
            }
            else
            {
                contrato.RegistrarPagamento(evento.Valor!.Value, evento.IdTransacao!, evento.DataPagamento!.Value, situacao);
            }

            evento.MarcarComoProcessado();
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar evento {EventoId}.", eventoId);
            evento.MarcarComoFalha(ex.Message);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}

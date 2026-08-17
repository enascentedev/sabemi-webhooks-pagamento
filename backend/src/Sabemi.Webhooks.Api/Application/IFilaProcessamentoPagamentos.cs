namespace Sabemi.Webhooks.Api.Application;

/// <summary>
/// Fila em memória que desacopla o recebimento do webhook (rápido) do
/// processamento pesado da regra de negócio (executado em background).
/// </summary>
public interface IFilaProcessamentoPagamentos
{
    ValueTask EnfileirarAsync(Guid eventoId, CancellationToken cancellationToken = default);

    ValueTask<Guid> AguardarProximoAsync(CancellationToken cancellationToken);
}

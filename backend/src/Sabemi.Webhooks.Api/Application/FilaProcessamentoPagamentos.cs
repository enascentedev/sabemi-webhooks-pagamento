using System.Threading.Channels;

namespace Sabemi.Webhooks.Api.Application;

/// <summary>
/// Implementação com Channel limitado: WriteAsync aguarda (backpressure) se a
/// fila estiver cheia, em vez de descartar eventos ou crescer sem limite.
/// </summary>
public sealed class FilaProcessamentoPagamentos : IFilaProcessamentoPagamentos
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(capacity: 500)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    });

    public ValueTask EnfileirarAsync(Guid eventoId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(eventoId, cancellationToken);

    public ValueTask<Guid> AguardarProximoAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);
}

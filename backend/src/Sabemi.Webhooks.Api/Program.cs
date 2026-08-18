using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Api.Application;
using Sabemi.Webhooks.Api.Domain;
using Sabemi.Webhooks.Api.Endpoints;
using Sabemi.Webhooks.Api.Infrastructure;
using Sabemi.Webhooks.Api.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.")));

builder.Services.AddSingleton<IFilaProcessamentoPagamentos, FilaProcessamentoPagamentos>();
builder.Services.AddScoped<IProcessamentoWebhookService, ProcessamentoWebhookService>();
builder.Services.AddScoped<IConsultaPagamentosService, ConsultaPagamentosService>();
builder.Services.AddHostedService<ProcessadorPagamentosWorker>();
builder.Services.AddTransient<AssinaturaWebhookFilter>();

var origensPermitidas = builder.Configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy => policy
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("Dashboard");

app.MapWebhookEndpoints();
app.MapPagamentoEndpoints();
// Verifica o banco: uma API que responde "ok" sem conseguir persistir daria falso positivo para
// quem monitora o serviço, já que todo o fluxo do webhook depende do Postgres.
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ok" })
            : Results.Json(new { status = "degradado" }, statusCode: StatusCodes.Status503ServiceUnavailable))
    .WithName("HealthCheck")
    .WithTags("Infraestrutura")
    .WithSummary("Indica se a API e o banco de dados estão respondendo");

// EF.IsDesignTime evita que a migração automática e a recuperação de eventos
// pendentes rodem durante `dotnet ef migrations add`/`database update`.
if (!EF.IsDesignTime)
{
    await AplicarMigracoesAsync(app);
    await RecuperarEventosPendentesAsync(app);
}

app.Run();

static async Task AplicarMigracoesAsync(WebApplication app)
{
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

static async Task RecuperarEventosPendentesAsync(WebApplication app)
{
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
    var fila = escopo.ServiceProvider.GetRequiredService<IFilaProcessamentoPagamentos>();

    var idsPendentes = await db.EventosWebhookBrutos
        .Where(e => e.StatusProcessamento == StatusProcessamento.Pendente || e.StatusProcessamento == StatusProcessamento.Processando)
        .Select(e => e.Id)
        .ToListAsync();

    foreach (var id in idsPendentes)
    {
        await fila.EnfileirarAsync(id);
    }
}

// Necessário para que o WebApplicationFactory<Program> dos testes de integração
// consiga referenciar esta classe de entrada (top-level statements geram "Program" implícito).
public partial class Program;

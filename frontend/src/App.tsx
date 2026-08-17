import { useState } from "react"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { MetricasCards } from "@/components/dashboard/MetricasCards"
import { FiltrosPagamentos } from "@/components/dashboard/FiltrosPagamentos"
import { TabelaPagamentos } from "@/components/dashboard/TabelaPagamentos"
import { DetalheEventoDialog } from "@/components/dashboard/DetalheEventoDialog"
import { usePagamentos } from "@/hooks/usePagamentos"
import { useMetricas } from "@/hooks/useMetricas"
import { AlertTriangleIcon } from "lucide-react"
import type { FiltrosPagamentos as FiltrosPagamentosState, PagamentoResumo } from "@/types/pagamento"

const FILTROS_INICIAIS: FiltrosPagamentosState = {
  status: "Todos",
  idContrato: "",
  pagina: 1,
}

function App() {
  const [filtros, setFiltros] = useState<FiltrosPagamentosState>(FILTROS_INICIAIS)
  const [eventoSelecionado, setEventoSelecionado] = useState<PagamentoResumo | null>(null)

  const { data: metricas, isLoading: carregandoMetricas } = useMetricas()
  const { data: pagamentos, isLoading: carregandoPagamentos } = usePagamentos(filtros)

  const totalPaginas = pagamentos ? Math.max(1, Math.ceil(pagamentos.total / pagamentos.tamanhoPagina)) : 1

  return (
    <div className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 p-6">
      <header>
        <h1 className="font-heading text-2xl font-semibold">Pagamentos — Webhooks Sabemi</h1>
        <p className="text-sm text-muted-foreground">
          Notificações de pagamento recebidas do banco parceiro, atualizadas automaticamente a cada 5 segundos.
        </p>
      </header>

      <MetricasCards metricas={metricas} carregando={carregandoMetricas} />

      {!carregandoMetricas && (metricas?.falhas ?? 0) > 0 && (
        <Alert variant="destructive">
          <AlertTriangleIcon />
          <AlertTitle>Existem eventos com falha</AlertTitle>
          <AlertDescription>
            {metricas!.falhas} evento(s) falharam na validação ou no processamento. Use o filtro
            "Falha de validação" ou clique em uma linha destacada na tabela para ver os detalhes.
          </AlertDescription>
        </Alert>
      )}

      <section className="flex flex-col gap-4">
        <FiltrosPagamentos filtros={filtros} aoAlterar={setFiltros} />

        <TabelaPagamentos
          itens={pagamentos?.itens ?? []}
          carregando={carregandoPagamentos}
          aoSelecionar={setEventoSelecionado}
        />

        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            Página {filtros.pagina} de {totalPaginas} · {pagamentos?.total ?? 0} evento(s)
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={filtros.pagina <= 1}
              onClick={() => setFiltros((atual) => ({ ...atual, pagina: atual.pagina - 1 }))}
            >
              Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={filtros.pagina >= totalPaginas}
              onClick={() => setFiltros((atual) => ({ ...atual, pagina: atual.pagina + 1 }))}
            >
              Próxima
            </Button>
          </div>
        </div>
      </section>

      <DetalheEventoDialog evento={eventoSelecionado} aoFechar={() => setEventoSelecionado(null)} />
    </div>
  )
}

export default App

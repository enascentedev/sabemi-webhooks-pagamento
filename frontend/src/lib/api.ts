import type { FiltrosPagamentos, Metricas, PagamentosPaginados } from "@/types/pagamento"

const TAMANHO_PAGINA = 20

async function tratarResposta<T>(resposta: Response): Promise<T> {
  if (!resposta.ok) {
    throw new Error(`Requisição falhou com status ${resposta.status}`)
  }
  return (await resposta.json()) as T
}

export async function buscarPagamentos(filtros: FiltrosPagamentos): Promise<PagamentosPaginados> {
  const params = new URLSearchParams()
  if (filtros.status !== "Todos") {
    params.set("status", filtros.status)
  }
  if (filtros.idContrato.trim()) {
    params.set("idContrato", filtros.idContrato.trim())
  }
  params.set("pagina", String(filtros.pagina))
  params.set("tamanhoPagina", String(TAMANHO_PAGINA))

  const resposta = await fetch(`/api/pagamentos?${params.toString()}`)
  return tratarResposta<PagamentosPaginados>(resposta)
}

export async function buscarMetricas(): Promise<Metricas> {
  const resposta = await fetch("/api/metricas")
  return tratarResposta<Metricas>(resposta)
}

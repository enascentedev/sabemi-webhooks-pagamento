import { keepPreviousData, useQuery } from "@tanstack/react-query"
import { buscarPagamentos } from "@/lib/api"
import type { FiltrosPagamentos } from "@/types/pagamento"

const INTERVALO_ATUALIZACAO_MS = 5000

export function usePagamentos(filtros: FiltrosPagamentos) {
  return useQuery({
    queryKey: ["pagamentos", filtros],
    queryFn: () => buscarPagamentos(filtros),
    placeholderData: keepPreviousData,
    refetchInterval: INTERVALO_ATUALIZACAO_MS,
  })
}

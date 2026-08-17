import { useQuery } from "@tanstack/react-query"
import { buscarMetricas } from "@/lib/api"

const INTERVALO_ATUALIZACAO_MS = 5000

export function useMetricas() {
  return useQuery({
    queryKey: ["metricas"],
    queryFn: buscarMetricas,
    refetchInterval: INTERVALO_ATUALIZACAO_MS,
  })
}

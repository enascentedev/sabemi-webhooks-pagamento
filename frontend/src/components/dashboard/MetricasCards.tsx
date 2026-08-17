import { Card, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"
import type { Metricas } from "@/types/pagamento"

interface MetricasCardsProps {
  metricas: Metricas | undefined
  carregando: boolean
}

export function MetricasCards({ metricas, carregando }: MetricasCardsProps) {
  const itens = [
    { label: "Total recebido", valor: metricas?.total },
    { label: "Processados", valor: metricas?.processados },
    { label: "Pendentes / processando", valor: (metricas?.pendentes ?? 0) + (metricas?.processando ?? 0) },
    { label: "Falhas", valor: metricas?.falhas, destaque: true },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {itens.map((item) => (
        <Card key={item.label}>
          <CardHeader>
            <CardDescription>{item.label}</CardDescription>
            {carregando ? (
              <Skeleton className="h-8 w-16" />
            ) : (
              <CardTitle
                className={cn("text-3xl", item.destaque && (item.valor ?? 0) > 0 && "text-destructive")}
              >
                {item.valor ?? 0}
              </CardTitle>
            )}
          </CardHeader>
        </Card>
      ))}
    </div>
  )
}

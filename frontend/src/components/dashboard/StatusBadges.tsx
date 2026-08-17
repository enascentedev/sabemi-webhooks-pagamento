import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import type { StatusPagamentoBanco, StatusProcessamento } from "@/types/pagamento"

const ESTILOS_PROCESSAMENTO: Record<Exclude<StatusProcessamento, "Falha">, string> = {
  Pendente: "border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-400",
  Processando: "border-blue-500/30 bg-blue-500/10 text-blue-700 dark:text-blue-400 animate-pulse",
  Processado: "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400",
}

export function StatusProcessamentoBadge({ status }: { status: StatusProcessamento }) {
  if (status === "Falha") {
    return <Badge variant="destructive">Falha</Badge>
  }

  return (
    <Badge variant="outline" className={cn(ESTILOS_PROCESSAMENTO[status])}>
      {status}
    </Badge>
  )
}

export function StatusPagamentoBadge({ status }: { status: StatusPagamentoBanco | null }) {
  if (!status) {
    return <span className="text-muted-foreground">—</span>
  }

  if (status === "Sucesso") {
    return (
      <Badge variant="outline" className="border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400">
        Sucesso
      </Badge>
    )
  }

  return <Badge variant="destructive">Erro</Badge>
}

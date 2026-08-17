import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { StatusPagamentoBadge, StatusProcessamentoBadge } from "@/components/dashboard/StatusBadges"
import { AlertTriangleIcon } from "lucide-react"
import type { PagamentoResumo } from "@/types/pagamento"

interface DetalheEventoDialogProps {
  evento: PagamentoResumo | null
  aoFechar: () => void
}

function formatarJson(payload: string): string {
  try {
    return JSON.stringify(JSON.parse(payload), null, 2)
  } catch {
    return payload
  }
}

export function DetalheEventoDialog({ evento, aoFechar }: DetalheEventoDialogProps) {
  return (
    <Dialog open={evento !== null} onOpenChange={(aberto) => !aberto && aoFechar()}>
      <DialogContent className="sm:max-w-lg">
        {evento && (
          <>
            <DialogHeader>
              <DialogTitle>Evento {evento.idTransacao ?? "(sem id_transacao)"}</DialogTitle>
              <DialogDescription>
                Recebido em {new Date(evento.recebidoEm).toLocaleString("pt-BR")}
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-wrap items-center gap-2">
              <StatusProcessamentoBadge status={evento.statusProcessamento} />
              <StatusPagamentoBadge status={evento.statusPagamentoBanco} />
            </div>

            {evento.erroMensagem && (
              <Alert variant="destructive">
                <AlertTriangleIcon />
                <AlertTitle>Falha no processamento</AlertTitle>
                <AlertDescription>{evento.erroMensagem}</AlertDescription>
              </Alert>
            )}

            <div>
              <p className="mb-1 text-xs font-medium text-muted-foreground">Payload bruto recebido</p>
              <pre className="max-h-64 overflow-auto rounded-lg bg-muted p-3 text-xs">
                {formatarJson(evento.payloadBruto ?? "")}
              </pre>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}

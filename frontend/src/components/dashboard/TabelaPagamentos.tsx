import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Skeleton } from "@/components/ui/skeleton"
import { StatusPagamentoBadge, StatusProcessamentoBadge } from "@/components/dashboard/StatusBadges"
import { cn } from "@/lib/utils"
import type { PagamentoResumo } from "@/types/pagamento"

interface TabelaPagamentosProps {
  itens: PagamentoResumo[]
  carregando: boolean
  aoSelecionar: (evento: PagamentoResumo) => void
}

function formatarValor(valor: number | null): string {
  if (valor === null) return "—"
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function formatarData(data: string | null): string {
  if (!data) return "—"
  return new Date(data).toLocaleString("pt-BR")
}

export function TabelaPagamentos({ itens, carregando, aoSelecionar }: TabelaPagamentosProps) {
  return (
    <div className="rounded-xl border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>ID Transação</TableHead>
            <TableHead>ID Contrato</TableHead>
            <TableHead>Valor</TableHead>
            <TableHead>Data pagamento</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Processamento</TableHead>
            <TableHead>Recebido em</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {carregando && itens.length === 0 &&
            Array.from({ length: 5 }).map((_, indice) => (
              <TableRow key={indice}>
                {Array.from({ length: 7 }).map((__, coluna) => (
                  <TableCell key={coluna}>
                    <Skeleton className="h-4 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))}

          {!carregando && itens.length === 0 && (
            <TableRow>
              <TableCell colSpan={7} className="py-8 text-center text-muted-foreground">
                Nenhum pagamento encontrado para os filtros selecionados.
              </TableCell>
            </TableRow>
          )}

          {itens.map((evento) => (
            <TableRow
              key={evento.id}
              onClick={() => aoSelecionar(evento)}
              className={cn(
                "cursor-pointer",
                evento.statusProcessamento === "Falha" && "bg-destructive/5 hover:bg-destructive/10"
              )}
            >
              <TableCell className="font-mono text-xs">{evento.idTransacao ?? "—"}</TableCell>
              <TableCell className="font-mono text-xs">{evento.idContrato ?? "—"}</TableCell>
              <TableCell>{formatarValor(evento.valor)}</TableCell>
              <TableCell>{formatarData(evento.dataPagamento)}</TableCell>
              <TableCell>
                <StatusPagamentoBadge status={evento.statusPagamentoBanco} />
              </TableCell>
              <TableCell>
                <StatusProcessamentoBadge status={evento.statusProcessamento} />
              </TableCell>
              <TableCell className="text-muted-foreground">{formatarData(evento.recebidoEm)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

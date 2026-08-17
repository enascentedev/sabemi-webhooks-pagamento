import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { FiltrosPagamentos as FiltrosPagamentosState } from "@/types/pagamento"

interface FiltrosPagamentosProps {
  filtros: FiltrosPagamentosState
  aoAlterar: (filtros: FiltrosPagamentosState) => void
}

const OPCOES_STATUS: { valor: FiltrosPagamentosState["status"]; rotulo: string }[] = [
  { valor: "Todos", rotulo: "Todos os status" },
  { valor: "Sucesso", rotulo: "Sucesso" },
  { valor: "Erro", rotulo: "Erro" },
  { valor: "Falha", rotulo: "Falha de validação" },
]

export function FiltrosPagamentos({ filtros, aoAlterar }: FiltrosPagamentosProps) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <Select
        value={filtros.status}
        onValueChange={(valor) =>
          aoAlterar({ ...filtros, status: valor as FiltrosPagamentosState["status"], pagina: 1 })
        }
      >
        <SelectTrigger className="w-full sm:w-48">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          {OPCOES_STATUS.map((opcao) => (
            <SelectItem key={opcao.valor} value={opcao.valor}>
              {opcao.rotulo}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Input
        placeholder="Filtrar por ID do contrato"
        value={filtros.idContrato}
        onChange={(evento) => aoAlterar({ ...filtros, idContrato: evento.target.value, pagina: 1 })}
        className="sm:max-w-xs"
      />
    </div>
  )
}

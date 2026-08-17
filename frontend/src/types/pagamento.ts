export type StatusProcessamento = "Pendente" | "Processando" | "Processado" | "Falha"

export type StatusPagamentoBanco = "Sucesso" | "Erro"

export interface PagamentoResumo {
  id: string
  idTransacao: string | null
  idContrato: string | null
  valor: number | null
  dataPagamento: string | null
  statusPagamentoBanco: StatusPagamentoBanco | null
  statusProcessamento: StatusProcessamento
  erroMensagem: string | null
  payloadBruto: string
  recebidoEm: string
  processadoEm: string | null
}

export interface PagamentosPaginados {
  itens: PagamentoResumo[]
  pagina: number
  tamanhoPagina: number
  total: number
}

export interface Metricas {
  total: number
  pendentes: number
  processando: number
  processados: number
  falhas: number
}

export interface FiltrosPagamentos {
  status: "Todos" | StatusPagamentoBanco | "Falha"
  idContrato: string
  pagina: number
}

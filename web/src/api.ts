export type PaisRef = { codigo: string; nome: string }
export type CategoriaRef = { codigo: string; nome: string }
export type Imagem = { url: string; credito: string }

export type EquipamentoResumo = {
  slug: string
  nome: string
  variante: string | null
  pais: PaisRef
  forca: string
  categoria: CategoriaRef
  anoEntradaServico: number | null
  status: string
  confiabilidade: string
  descricaoCurta: string | null
  imagem: Imagem | null
}

export type ListaPaginada<T> = {
  itens: T[]
  pagina: number
  tamanhoPagina: number
  total: number
  totalPaginas: number
}

export type Convertido = { valor: number; unidade: string }
export type Medida = {
  chave: string
  rotulo: string
  valor: number
  unidade: string
  convertidos: Convertido[]
}
export type Campo = {
  chave: string
  rotulo: string
  valor: string | null
  itens: string[] | null
}
export type Fonte = {
  slug: string
  tipo: string
  titulo: string
  autor: string | null
  ano: number | null
  url: string | null
  provisoria: boolean
  referencia: string | null
}

export type ParticipacaoNaFicha = {
  evento: { slug: string; nome: string; tipo: string }
  dataInicio: string
  dataFim: string | null
  pais: PaisRef
  papel: string
  quantidadeAprox: number | null
  observacao: string | null
}

export type EquipamentoFicha = {
  slug: string
  nome: string
  variante: string | null
  pais: PaisRef
  forca: string
  categoria: CategoriaRef
  fabricante: string | null
  anoPrimeiroUso: number | null
  anoEntradaServico: number | null
  unidadesProduzidas: number | null
  status: string
  descricaoCurta: string | null
  curiosidade: string | null
  imagem: Imagem | null
  confiabilidade: string
  somenteFontesProvisorias: boolean
  operadores: PaisRef[]
  especificacoes: { medidas: Medida[]; campos: Campo[] }
  fontes: Fonte[]
  eventos: ParticipacaoNaFicha[]
}

export type Filtros = {
  forcas: PaisRef[]
  categorias: { codigo: string; nome: string; forca: string | null }[]
  paises: PaisRef[]
  status: string[]
  paisesEventos: PaisRef[]
  tiposEvento: string[]
}

export type EventoRef = { slug: string; nome: string; tipo: string }

export type EventoResumo = {
  slug: string
  nome: string
  tipo: string
  dataInicio: string
  dataFim: string | null
  local: string | null
  confiabilidade: string
  eventoPai: EventoRef | null
}

export type NoEvento = {
  slug: string
  nome: string
  tipo: string
  dataInicio: string
  dataFim: string | null
  subeventos: NoEvento[]
}

export type Lado = { lado: string | null; paises: PaisRef[] }

export type EquipamentoNoEvento = {
  slug: string
  nome: string
  categoria: CategoriaRef
  papel: string
  quantidadeAprox: number | null
  observacao: string | null
}

export type PaisGrupo = {
  pais: PaisRef
  forcas: { forca: string; equipamentos: EquipamentoNoEvento[] }[]
}

export type EventoDetalhe = {
  slug: string
  nome: string
  tipo: string
  dataInicio: string
  dataFim: string | null
  local: string | null
  descricao: string | null
  resultado: string | null
  confiabilidade: string
  somenteFontesProvisorias: boolean
  eventoPai: EventoRef | null
  beligerantes: Lado[]
  subeventos: NoEvento[]
  participacoes: PaisGrupo[]
  fontes: Fonte[]
}

/** Erro de HTTP com o status, para a interface distinguir "não encontrado" de falha. */
export class ErroHttp extends Error {
  status: number
  constructor(status: number, mensagem: string) {
    super(mensagem)
    this.status = status
  }
}

async function obter<T>(url: string): Promise<T> {
  const resposta = await fetch(url)
  if (!resposta.ok) throw new ErroHttp(resposta.status, `Falha ao consultar ${url}`)
  return resposta.json() as Promise<T>
}

export type ParametrosLista = {
  forca?: string
  categoria?: string
  pais?: string
  status?: string
  de?: string
  ate?: string
  pagina?: string
  tamanho?: string
}

export function buscarEquipamentos(p: ParametrosLista): Promise<ListaPaginada<EquipamentoResumo>> {
  const q = new URLSearchParams()
  for (const [chave, valor] of Object.entries(p)) {
    if (valor) q.set(chave, valor)
  }
  const texto = q.toString()
  return obter(`/api/equipamentos${texto ? `?${texto}` : ''}`)
}

export function buscarFicha(slug: string): Promise<EquipamentoFicha> {
  return obter(`/api/equipamentos/${encodeURIComponent(slug)}`)
}

export function buscarFiltros(): Promise<Filtros> {
  return obter('/api/filtros')
}

export type ParametrosEventos = {
  tipo?: string
  pais?: string
  forca?: string
  tamanho?: string
  de?: string
  ate?: string
  pagina?: string
}

export function buscarEventos(p: ParametrosEventos): Promise<ListaPaginada<EventoResumo>> {
  const q = new URLSearchParams()
  for (const [chave, valor] of Object.entries(p)) {
    if (valor) q.set(chave, valor)
  }
  const texto = q.toString()
  return obter(`/api/eventos${texto ? `?${texto}` : ''}`)
}

export function buscarEvento(slug: string): Promise<EventoDetalhe> {
  return obter(`/api/eventos/${encodeURIComponent(slug)}`)
}

export type ResultadoBusca = {
  termo: string
  total: number
  equipamentos: EquipamentoResumo[]
  eventos: EventoResumo[]
}

export function buscarTexto(termo: string): Promise<ResultadoBusca> {
  return obter(`/api/busca?q=${encodeURIComponent(termo)}`)
}

export type ParametrosLinhaDoTempo = { forca?: string; pais?: string }

export function buscarLinhaDoTempo(p: ParametrosLinhaDoTempo): Promise<{ raizes: NoEvento[] }> {
  const q = new URLSearchParams()
  for (const [chave, valor] of Object.entries(p)) {
    if (valor) q.set(chave, valor)
  }
  const texto = q.toString()
  return obter(`/api/linha-do-tempo${texto ? `?${texto}` : ''}`)
}

const FORCAS: Record<string, string> = {
  MARINHA: 'Marinha',
  AEREA: 'Força Aérea',
  EXERCITO: 'Exército',
}

const STATUS: Record<string, string> = {
  ATIVO: 'Ativo',
  APOSENTADO: 'Aposentado',
  PROTOTIPO: 'Protótipo',
}

const CONFIABILIDADE: Record<string, string> = {
  CONFIRMADO: 'Confirmado',
  ESTIMADO: 'Estimado',
  CONTESTADO: 'Contestado',
}

const TIPOS_FONTE: Record<string, string> = {
  LIVRO: 'Livro',
  ARTIGO: 'Artigo',
  SITE: 'Site',
  ARQUIVO: 'Arquivo',
}

const TIPOS_EVENTO: Record<string, string> = {
  GUERRA: 'Guerra',
  CAMPANHA: 'Campanha',
  BATALHA: 'Batalha',
}

export const rotuloTipoEvento = (c: string) => TIPOS_EVENTO[c] ?? c

/** "2022-02-24" -> "24/02/2022" (sem passar por Date, para não sofrer com fuso horário). */
export function formatarData(iso: string): string {
  const [ano, mes, dia] = iso.split('-')
  return ano && mes && dia ? `${dia}/${mes}/${ano}` : iso
}

/** Período de um evento; sem data de fim, o evento está em andamento. */
export function formatarPeriodo(inicio: string, fim: string | null): string {
  if (fim === null) return `${formatarData(inicio)} até o presente`
  if (fim === inicio) return formatarData(inicio)
  return `${formatarData(inicio)} a ${formatarData(fim)}`
}

export const rotuloForca = (c: string) => FORCAS[c] ?? c
export const rotuloStatus = (c: string) => STATUS[c] ?? c
export const rotuloConfiabilidade = (c: string) => CONFIABILIDADE[c] ?? c
export const rotuloTipoFonte = (c: string) => TIPOS_FONTE[c] ?? c

const numero = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 })
export const formatarNumero = (n: number) => numero.format(n)

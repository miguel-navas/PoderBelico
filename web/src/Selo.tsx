import { Link } from 'react-router-dom'
import type { Fonte } from './api'
import { rotuloTipoFonte } from './rotulos'

const ROTULO_ALERTA: Record<string, string> = {
  ESTIMADO: 'Dados estimados',
  CONTESTADO: 'Dados contestados',
}

/** Selo de confiabilidade: dado confirmado não leva alerta; os demais apontam para a metodologia. */
export function Selo({ confiabilidade }: { confiabilidade: string }) {
  const classe = `selo selo-${confiabilidade.toLowerCase()}`
  if (confiabilidade === 'CONFIRMADO') return <span className={classe}>Confirmado</span>
  return (
    <Link to="/metodologia" className={classe}>
      {ROTULO_ALERTA[confiabilidade] ?? confiabilidade}
    </Link>
  )
}

export function ListaFontes({ fontes }: { fontes: Fonte[] }) {
  return (
    <>
      <h3>Fontes</h3>
      <ul>
        {fontes.map((f) => (
          <li key={f.slug}>
            {f.url ? (
              <a href={f.url} rel="noreferrer">
                {f.titulo}
              </a>
            ) : (
              f.titulo
            )}
            {' — '}
            {rotuloTipoFonte(f.tipo)}
            {f.autor && `, ${f.autor}`}
            {f.ano && `, ${f.ano}`}
            {f.referencia && `, ${f.referencia}`}
            {f.provisoria && <em> (fonte provisória)</em>}
          </li>
        ))}
      </ul>
    </>
  )
}

import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { buscarEventos, buscarFiltros } from '../api'
import type { ParametrosEventos } from '../api'
import { LinkF } from '../navegacao'
import { formatarPeriodo, rotuloForca, rotuloTipoEvento } from '../rotulos'

const CHAVES = ['tipo', 'forca', 'pais', 'de', 'ate', 'pagina'] as const

export default function Eventos() {
  const [busca, setBusca] = useSearchParams()

  const parametros: ParametrosEventos = {}
  for (const chave of CHAVES) {
    const valor = busca.get(chave)
    if (valor) parametros[chave] = valor
  }
  const temFiltro = CHAVES.some((c) => c !== 'pagina' && busca.get(c))

  const filtros = useQuery({ queryKey: ['filtros'], queryFn: buscarFiltros })
  const lista = useQuery({
    queryKey: ['eventos', parametros],
    queryFn: () => buscarEventos(parametros),
  })

  function alterar(chave: string, valor: string) {
    const novo = new URLSearchParams(busca)
    if (valor) novo.set(chave, valor)
    else novo.delete(chave)
    if (chave !== 'pagina') novo.delete('pagina')
    setBusca(novo)
  }

  return (
    <section>
      <h2>Eventos</h2>

      <form className="filtros" onSubmit={(e) => e.preventDefault()} aria-label="Filtros">
        <label>
          Tipo
          <select value={parametros.tipo ?? ''} onChange={(e) => alterar('tipo', e.target.value)}>
            <option value="">Todos</option>
            {filtros.data?.tiposEvento.map((t) => (
              <option key={t} value={t}>
                {rotuloTipoEvento(t)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Força
          <select value={parametros.forca ?? ''} onChange={(e) => alterar('forca', e.target.value)}>
            <option value="">Todas</option>
            {filtros.data?.forcas.map((f) => (
              <option key={f.codigo} value={f.codigo}>
                {rotuloForca(f.codigo)}
              </option>
            ))}
          </select>
        </label>
        <label>
          País participante
          <select value={parametros.pais ?? ''} onChange={(e) => alterar('pais', e.target.value)}>
            <option value="">Todos</option>
            {filtros.data?.paisesEventos.map((p) => (
              <option key={p.codigo} value={p.codigo}>
                {p.nome}
              </option>
            ))}
          </select>
        </label>
        <label>
          De (ano)
          <input
            type="number"
            inputMode="numeric"
            placeholder="ano"
            value={parametros.de ?? ''}
            onChange={(e) => alterar('de', e.target.value)}
          />
        </label>
        <label>
          Até (ano)
          <input
            type="number"
            inputMode="numeric"
            placeholder="ano"
            value={parametros.ate ?? ''}
            onChange={(e) => alterar('ate', e.target.value)}
          />
        </label>
      </form>

      {lista.isPending && <p>Carregando eventos...</p>}
      {lista.isError && <p role="alert">Não foi possível carregar os eventos.</p>}

      {lista.data && lista.data.itens.length === 0 && (
        <div className="vazio">
          <p>Nenhum evento encontrado.</p>
          {temFiltro && (
            <button type="button" onClick={() => setBusca(new URLSearchParams())}>
              Limpar filtros
            </button>
          )}
        </div>
      )}

      {lista.data && lista.data.itens.length > 0 && (
        <>
          <p className="contagem">
            {lista.data.total} {lista.data.total === 1 ? 'evento' : 'eventos'}
          </p>
          <ul className="cartoes">
            {lista.data.itens.map((e) => (
              <li key={e.slug}>
                <LinkF to={`/eventos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                </LinkF>
                <div className="meta">
                  {rotuloTipoEvento(e.tipo)} · {formatarPeriodo(e.dataInicio, e.dataFim)}
                </div>
                {e.eventoPai && (
                  <div className="meta">
                    Dentro de <Link to={`/eventos/${e.eventoPai.slug}`}>{e.eventoPai.nome}</Link>
                  </div>
                )}
              </li>
            ))}
          </ul>

          {lista.data.totalPaginas > 1 && (
            <nav className="paginacao" aria-label="Paginação">
              <button
                type="button"
                disabled={lista.data.pagina <= 1}
                onClick={() => alterar('pagina', String(lista.data.pagina - 1))}
              >
                Anterior
              </button>
              <span>
                Página {lista.data.pagina} de {lista.data.totalPaginas}
              </span>
              <button
                type="button"
                disabled={lista.data.pagina >= lista.data.totalPaginas}
                onClick={() => alterar('pagina', String(lista.data.pagina + 1))}
              >
                Próxima
              </button>
            </nav>
          )}
        </>
      )}
    </section>
  )
}

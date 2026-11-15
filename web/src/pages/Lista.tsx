import { ImagemCartao } from '../ImagemCartao'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { buscarEquipamentos, buscarFiltros } from '../api'
import type { ParametrosLista } from '../api'
import { LinkF } from '../navegacao'
import { rotuloConfiabilidade, rotuloForca, rotuloStatus } from '../rotulos'

const CHAVES = ['forca', 'subtipo', 'pais', 'status', 'de', 'ate', 'pagina'] as const

export default function Lista() {
  const [busca, setBusca] = useSearchParams()

  const parametros: ParametrosLista = {}
  for (const chave of CHAVES) {
    const valor = busca.get(chave)
    if (valor) parametros[chave] = valor
  }
  const abertoInicial = typeof window.matchMedia !== 'function' || !window.matchMedia('(max-width: 600px)').matches
  const nAtivos = CHAVES.filter((c) => c !== 'pagina' && busca.get(c)).length
  const temFiltro = CHAVES.some((c) => c !== 'pagina' && busca.get(c))

  const filtros = useQuery({ queryKey: ['filtros'], queryFn: buscarFiltros })
  const lista = useQuery({
    queryKey: ['equipamentos', parametros],
    queryFn: () => buscarEquipamentos(parametros),
  })

  // A mudança de qualquer filtro volta para a primeira página.
  function alterar(chave: string, valor: string) {
    const novo = new URLSearchParams(busca)
    if (valor) novo.set(chave, valor)
    else novo.delete(chave)
    if (chave !== 'pagina') novo.delete('pagina')
    if (chave === 'forca') novo.delete('subtipo')
    setBusca(novo)
  }

  const subtipos = (filtros.data?.subtipos ?? []).filter(
    (c) => !parametros.forca || c.forca === parametros.forca,
  )
  const IMAGEM_FORCA: Record<string, string> = {
    AEREA: 'caca',
    MARINHA: 'porta_avioes',
    EXERCITO: 'tanque_de_batalha',
  }
  const imagemBanner = parametros.subtipo
    ? `/img/categorias/${parametros.subtipo}.jpg`
    : parametros.forca && IMAGEM_FORCA[parametros.forca]
      ? `/img/categorias/${IMAGEM_FORCA[parametros.forca]}.jpg`
      : '/img/fundo-moderno.png'
  const subtipoAtual = filtros.data?.subtipos?.find((c) => c.codigo === parametros.subtipo)

  return (
    <section>
      <h2>Equipamentos</h2>

      <div
        className="banner-categoria"
        style={{ backgroundImage: `linear-gradient(90deg, #080b0ecc, #080b0e33), url(${imagemBanner})` }}
      >
        <h3>{subtipoAtual?.nome ?? (parametros.forca ? rotuloForca(parametros.forca) : 'Todos os equipamentos')}</h3>
      </div>

      <details className="painel-filtros" open={abertoInicial}>
        <summary>Filtros{nAtivos > 0 ? ` (${nAtivos} ativos)` : ''}</summary>
      <form className="filtros" onSubmit={(e) => e.preventDefault()} aria-label="Filtros">
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
          Categoria
          <select value={parametros.subtipo ?? ''} onChange={(e) => alterar('subtipo', e.target.value)}>
            <option value="">Todas</option>
            {subtipos.map((c) => (
              <option key={c.codigo} value={c.codigo}>
                {c.nome}
              </option>
            ))}
          </select>
        </label>
        <label>
          País de origem
          <select value={parametros.pais ?? ''} onChange={(e) => alterar('pais', e.target.value)}>
            <option value="">Todos</option>
            {filtros.data?.paises.map((p) => (
              <option key={p.codigo} value={p.codigo}>
                {p.nome}
              </option>
            ))}
          </select>
        </label>
        <label>
          Status
          <select value={parametros.status ?? ''} onChange={(e) => alterar('status', e.target.value)}>
            <option value="">Todos</option>
            {filtros.data?.status.map((s) => (
              <option key={s} value={s}>
                {rotuloStatus(s)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Em serviço desde
          <input
            type="number"
            inputMode="numeric"
            placeholder="ano"
            value={parametros.de ?? ''}
            onChange={(e) => alterar('de', e.target.value)}
          />
        </label>
        <label>
          Em serviço até
          <input
            type="number"
            inputMode="numeric"
            placeholder="ano"
            value={parametros.ate ?? ''}
            onChange={(e) => alterar('ate', e.target.value)}
          />
        </label>
      </form>
      </details>

      {lista.isPending && <p>Carregando equipamentos...</p>}
      {lista.isError && <p role="alert">Não foi possível carregar os equipamentos.</p>}

      {lista.data && lista.data.itens.length === 0 && (
        <div className="vazio">
          <p>Nenhum equipamento encontrado.</p>
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
            {lista.data.total} {lista.data.total === 1 ? 'equipamento' : 'equipamentos'}
          </p>
          <ul className="cartoes">
            {lista.data.itens.map((e) => (
              <li key={e.slug} className={`forca-${e.forca.toLowerCase()}`}>
                <ImagemCartao e={e} />
                <LinkF to={`/equipamentos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                  {e.variante && <span> {e.variante}</span>}
                </LinkF>
                <div className="meta">
                  {e.pais.nome} · {rotuloForca(e.forca)} · {e.subtipo?.nome ?? e.categoria.nome}
                  {e.anoEntradaServico !== null && <> · em serviço desde {e.anoEntradaServico}</>}
                </div>
                <div className="meta">
                  {rotuloStatus(e.status)} · {rotuloConfiabilidade(e.confiabilidade)}
                </div>
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

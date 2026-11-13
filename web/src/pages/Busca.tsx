import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { buscarTexto } from '../api'
import { formatarPeriodo, rotuloForca, rotuloTipoEvento } from '../rotulos'

export default function Busca() {
  const [params] = useSearchParams()
  const termo = (params.get('q') ?? '').trim()

  const resultado = useQuery({
    queryKey: ['busca', termo],
    queryFn: () => buscarTexto(termo),
    enabled: termo.length > 0,
    retry: false,
  })

  return (
    <section>
      <h2>Busca</h2>

      {termo.length === 0 && <p>Digite um nome ou parte da descrição no campo de busca acima.</p>}

      {termo.length > 0 && resultado.isPending && <p>Buscando...</p>}
      {resultado.isError && (
        <p role="alert">Não foi possível buscar. Use letras ou números e tente de novo.</p>
      )}

      {resultado.data && resultado.data.total === 0 && (
        <div className="vazio">
          <p>Nenhum resultado para “{resultado.data.termo}”.</p>
          <p>
            Experimente navegar por <Link to="/equipamentos">equipamentos</Link> ou pela{' '}
            <Link to="/eventos">linha do tempo de eventos</Link>.
          </p>
        </div>
      )}

      {resultado.data && resultado.data.equipamentos.length > 0 && (
        <section aria-labelledby="busca-equipamentos">
          <h3 id="busca-equipamentos">Equipamentos ({resultado.data.equipamentos.length})</h3>
          <ul className="cartoes">
            {resultado.data.equipamentos.map((e) => (
              <li key={e.slug}>
                <Link to={`/equipamentos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                  {e.variante ? ` ${e.variante}` : ''}
                </Link>
                <div className="meta">
                  {e.pais.nome} · {rotuloForca(e.forca)} · {e.categoria.nome}
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}

      {resultado.data && resultado.data.eventos.length > 0 && (
        <section aria-labelledby="busca-eventos">
          <h3 id="busca-eventos">Eventos ({resultado.data.eventos.length})</h3>
          <ul className="cartoes">
            {resultado.data.eventos.map((e) => (
              <li key={e.slug}>
                <Link to={`/eventos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                </Link>
                <div className="meta">
                  {rotuloTipoEvento(e.tipo)} · {formatarPeriodo(e.dataInicio, e.dataFim)}
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}
    </section>
  )
}

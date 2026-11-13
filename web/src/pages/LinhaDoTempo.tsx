import { useQuery } from '@tanstack/react-query'
import { buscarFiltros, buscarLinhaDoTempo } from '../api'
import type { NoEvento } from '../api'
import { LinkF, SeletorForca } from '../navegacao'
import { useForca } from '../useForca'
import { formatarPeriodo, rotuloTipoEvento } from '../rotulos'

function Arvore({ nos }: { nos: NoEvento[] }) {
  return (
    <ul>
      {nos.map((n) => (
        <li key={n.slug}>
          <LinkF to={`/eventos/${n.slug}`}>{n.nome}</LinkF>{' '}
          <span className="meta">
            ({rotuloTipoEvento(n.tipo)}, {formatarPeriodo(n.dataInicio, n.dataFim)})
          </span>
          {n.subeventos.length > 0 && <Arvore nos={n.subeventos} />}
        </li>
      ))}
    </ul>
  )
}

export default function LinhaDoTempo() {
  const forca = useForca()
  const filtros = useQuery({ queryKey: ['filtros'], queryFn: buscarFiltros })
  const linha = useQuery({
    queryKey: ['linha-do-tempo', forca],
    queryFn: () => buscarLinhaDoTempo({ forca }),
  })

  return (
    <section>
      <h2>Linha do tempo</h2>
      <p>De 1914 até o presente, agrupada por guerra e em ordem de data de início.</p>

      <form className="filtros" onSubmit={(e) => e.preventDefault()} aria-label="Filtros">
        <SeletorForca forcas={filtros.data?.forcas} />
      </form>

      {linha.isPending && <p>Carregando a linha do tempo...</p>}
      {linha.isError && <p role="alert">Não foi possível carregar a linha do tempo.</p>}

      {linha.data && linha.data.raizes.length === 0 && <p>Nenhum evento encontrado.</p>}

      {linha.data && linha.data.raizes.length > 0 && (
        <ol className="linha-do-tempo">
          {linha.data.raizes.map((r) => (
            <li key={r.slug}>
              <h3>
                <LinkF to={`/eventos/${r.slug}`}>{r.nome}</LinkF>
              </h3>
              <div className="meta">
                {rotuloTipoEvento(r.tipo)} · {formatarPeriodo(r.dataInicio, r.dataFim)}
              </div>
              {r.subeventos.length > 0 && <Arvore nos={r.subeventos} />}
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

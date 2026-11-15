import { ImagemCartao } from '../ImagemCartao'
import { useQuery } from '@tanstack/react-query'
import { buscarEquipamentos, buscarEventos, buscarFiltros } from '../api'
import { LinkF, SeletorForca } from '../navegacao'
import { useForca } from '../useForca'
import { formatarPeriodo, rotuloForca, rotuloTipoEvento } from '../rotulos'

function SecaoForca({ codigo, pais }: { codigo: string; pais: string }) {
  const equipamentos = useQuery({
    queryKey: ['pais-equipamentos', pais, codigo],
    queryFn: () => buscarEquipamentos({ pais, forca: codigo, tamanho: '100' }),
  })
  const eventos = useQuery({
    queryKey: ['pais-eventos', pais, codigo],
    queryFn: () => buscarEventos({ pais, forca: codigo, tamanho: '100' }),
  })

  const nenhum =
    equipamentos.data && eventos.data && equipamentos.data.itens.length === 0 && eventos.data.itens.length === 0

  return (
    <section aria-labelledby={`forca-${codigo}`}>
      <h3 id={`forca-${codigo}`}>{rotuloForca(codigo)}</h3>

      {(equipamentos.isPending || eventos.isPending) && <p>Carregando...</p>}
      {(equipamentos.isError || eventos.isError) && <p role="alert">Não foi possível carregar esta seção.</p>}
      {nenhum && <p>Nada cadastrado para esta força.</p>}

      {equipamentos.data && equipamentos.data.itens.length > 0 && (
        <>
          <h4>Equipamentos ({equipamentos.data.total})</h4>
          <ul className="cartoes">
            {equipamentos.data.itens.map((e) => (
              <li key={e.slug} className={`forca-${e.forca.toLowerCase()}`}>
                <ImagemCartao e={e} />
                <LinkF to={`/equipamentos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                  {e.variante ? ` ${e.variante}` : ''}
                </LinkF>
                <div className="meta">{e.categoria.nome}</div>
              </li>
            ))}
          </ul>
        </>
      )}

      {eventos.data && eventos.data.itens.length > 0 && (
        <>
          <h4>Eventos ({eventos.data.total})</h4>
          <ul className="cartoes">
            {eventos.data.itens.map((e) => (
              <li key={e.slug}>
                <LinkF to={`/eventos/${e.slug}`}>
                  <strong>{e.nome}</strong>
                </LinkF>
                <div className="meta">
                  {rotuloTipoEvento(e.tipo)} · {formatarPeriodo(e.dataInicio, e.dataFim)}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  )
}

export default function Pais({ codigo }: { codigo: string }) {
  const iso = codigo.toUpperCase()
  const forca = useForca()
  const filtros = useQuery({ queryKey: ['filtros'], queryFn: buscarFiltros })

  const nome = [...(filtros.data?.paises ?? []), ...(filtros.data?.paisesEventos ?? [])].find(
    (p) => p.codigo === iso,
  )?.nome
  const forcas = filtros.data?.forcas ?? []
  const mostrar = forca ? forcas.filter((f) => f.codigo === forca) : forcas

  if (filtros.isError) return <p role="alert">Não foi possível carregar o país.</p>
  if (filtros.data && !nome) {
    return (
      <section>
        <h2>País não encontrado</h2>
        <p>
          Não há dados para “{codigo}”. Veja a <LinkF to="/paises">lista de países</LinkF> ou use a{' '}
          <LinkF to="/busca">busca</LinkF>.
        </p>
      </section>
    )
  }

  return (
    <section>
      <h2>{nome ?? iso}</h2>

      <form className="filtros" onSubmit={(e) => e.preventDefault()} aria-label="Filtros">
        <SeletorForca forcas={forcas} />
      </form>

      {filtros.isPending && <p>Carregando...</p>}
      {mostrar.map((f) => (
        <SecaoForca key={f.codigo} codigo={f.codigo} pais={iso} />
      ))}
    </section>
  )
}

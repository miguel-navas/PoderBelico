import { useQuery } from '@tanstack/react-query'
import { buscarEquipamentos, buscarFiltros } from '../api'
import { LinkF } from '../navegacao'

function CartaoPais({ codigo, nome }: { codigo: string; nome: string }) {
  const capa = useQuery({
    queryKey: ['pais-capa', codigo],
    queryFn: () => buscarEquipamentos({ pais: codigo, tamanho: '20' }),
    staleTime: 5 * 60 * 1000,
  })
  const itens = capa.data?.itens ?? []
  const e = itens.find((i) => i.imagem) ?? null
  const url = e?.imagem?.url ?? (itens[0]?.subtipo ? `/img/categorias/${itens[0].subtipo.codigo}.jpg` : null)
  return (
    <li>
      <div className="cartao-img">
        {url ? (
          <>
            <img src={url} alt="" loading="lazy" decoding="async" />
            <span className="credito">{e?.imagem ? e.imagem.credito : 'Ilustração da categoria · IA'}</span>
          </>
        ) : (
          <div className="inicial" aria-hidden="true">{nome.charAt(0)}</div>
        )}
      </div>
      <LinkF to={`/paises/${codigo}`}>
        <strong>{nome}</strong>
      </LinkF>
    </li>
  )
}

export default function Paises() {
  const filtros = useQuery({ queryKey: ['filtros'], queryFn: buscarFiltros })

  // Países com equipamento ou com participação em eventos, sem repetir.
  const todos = new Map<string, string>()
  for (const p of [...(filtros.data?.paises ?? []), ...(filtros.data?.paisesEventos ?? [])]) {
    todos.set(p.codigo, p.nome)
  }
  const lista = [...todos].sort((a, b) => a[1].localeCompare(b[1], 'pt-BR'))

  return (
    <section>
      <h2>Países</h2>
      {filtros.isPending && <p>Carregando países...</p>}
      {filtros.isError && <p role="alert">Não foi possível carregar os países.</p>}
      <ul className="cartoes">
        {lista.map(([codigo, nome]) => (
          <CartaoPais key={codigo} codigo={codigo} nome={nome} />
        ))}
      </ul>
    </section>
  )
}

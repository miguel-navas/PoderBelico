import { useQuery } from '@tanstack/react-query'
import { buscarFiltros } from '../api'
import { LinkF } from '../navegacao'

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
          <li key={codigo}>
            <LinkF to={`/paises/${codigo}`}>
              <strong>{nome}</strong>
            </LinkF>
          </li>
        ))}
      </ul>
    </section>
  )
}

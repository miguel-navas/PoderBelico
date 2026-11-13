import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'

type Saude = { status: string; banco: boolean }

async function buscarSaude(): Promise<Saude> {
  const resposta = await fetch('/api/saude')
  if (!resposta.ok) throw new Error('Falha ao consultar a API')
  return resposta.json()
}

export default function Inicio() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['saude'],
    queryFn: buscarSaude,
  })

  return (
    <section>
      <p>Equipamentos militares, da 1ª Guerra Mundial aos dias de hoje.</p>
      <p>
        <Link to="/equipamentos">Ver equipamentos</Link> · <Link to="/linha-do-tempo">Linha do tempo</Link> ·{' '}
        <Link to="/paises">Países</Link>
      </p>
      {isPending && <p>Consultando a API...</p>}
      {isError && <p role="alert">API indisponível</p>}
      {data && <p>API {data.status}</p>}
    </section>
  )
}

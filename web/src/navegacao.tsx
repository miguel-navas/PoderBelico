import { Link, useSearchParams } from 'react-router-dom'
import type { LinkProps } from 'react-router-dom'
import { useForca } from './useForca'
import { rotuloForca } from './rotulos'

/** Link interno que leva o filtro de força da página atual para o destino. */
export function LinkF({ to, ...resto }: Omit<LinkProps, 'to'> & { to: string }) {
  const forca = useForca()
  const destino = forca && !to.includes('?') ? `${to}?forca=${encodeURIComponent(forca)}` : to
  return <Link to={destino} {...resto} />
}

type Opcao = { codigo: string }

/** Seletor de força que grava o filtro na URL e mantém os demais parâmetros. */
export function SeletorForca({ forcas }: { forcas: Opcao[] | undefined }) {
  const [params, setParams] = useSearchParams()
  return (
    <label>
      Força
      <select
        value={params.get('forca') ?? ''}
        onChange={(e) => {
          const novo = new URLSearchParams(params)
          if (e.target.value) novo.set('forca', e.target.value)
          else novo.delete('forca')
          setParams(novo)
        }}
      >
        <option value="">Todas</option>
        {forcas?.map((f) => (
          <option key={f.codigo} value={f.codigo}>
            {rotuloForca(f.codigo)}
          </option>
        ))}
      </select>
    </label>
  )
}

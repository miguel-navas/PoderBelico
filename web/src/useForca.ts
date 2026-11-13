import { useSearchParams } from 'react-router-dom'

/** Força escolhida, lida da URL (?forca=MARINHA). Vazio = todas. */
export function useForca(): string {
  const [params] = useSearchParams()
  return params.get('forca') ?? ''
}

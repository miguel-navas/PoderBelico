import { fireEvent, render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'

function resposta(corpo: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: async () => corpo }
}

function abrir(rota: string) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={[rota]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

const midway = {
  slug: 'batalha-de-midway', nome: 'Batalha de Midway', tipo: 'BATALHA',
  dataInicio: '1942-06-04', dataFim: '1942-06-07', local: null, confiabilidade: 'ESTIMADO', eventoPai: null,
}
const sbd = {
  slug: 'sbd-dauntless', nome: 'SBD Dauntless', variante: null, pais: { codigo: 'USA', nome: 'Estados Unidos' },
  forca: 'AEREA', categoria: { codigo: 'AERONAVE', nome: 'Aeronave' }, anoEntradaServico: 1940,
  status: 'APOSENTADO', confiabilidade: 'ESTIMADO', descricaoCurta: null, imagem: null,
}

test('resultados aparecem agrupados por tipo com links', async () => {
  const f = vi.fn(async () => resposta({ termo: 'midway', total: 2, equipamentos: [sbd], eventos: [midway] }))
  vi.stubGlobal('fetch', f)

  abrir('/busca?q=midway')

  expect(await screen.findByRole('heading', { name: /Equipamentos \(1\)/ })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: /Eventos \(1\)/ })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: /SBD Dauntless/ })).toHaveAttribute('href', '/equipamentos/sbd-dauntless')
  expect(screen.getByRole('link', { name: 'Batalha de Midway' })).toHaveAttribute('href', '/eventos/batalha-de-midway')
  expect(f).toHaveBeenCalledWith('/api/busca?q=midway')
})

test('sem resultados mostra mensagem e caminhos alternativos', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => resposta({ termo: 'xyz', total: 0, equipamentos: [], eventos: [] })))

  abrir('/busca?q=xyz')

  expect(await screen.findByText(/Nenhum resultado/)).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'equipamentos' })).toHaveAttribute('href', '/equipamentos')
  expect(screen.getByRole('link', { name: /linha do tempo/ })).toHaveAttribute('href', '/eventos')
})

test('termo inválido (400) mostra aviso', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => resposta({ erro: 'x' }, 400)))

  abrir('/busca?q=***')

  expect(await screen.findByRole('alert')).toHaveTextContent(/Não foi possível buscar/)
})

test('o campo do cabeçalho leva à página de busca', async () => {
  const f = vi.fn(async () => resposta({ termo: 'jutlandia', total: 0, equipamentos: [], eventos: [] }))
  vi.stubGlobal('fetch', f)
  abrir('/')

  const campo = screen.getByRole('searchbox', { name: 'Buscar' })
  fireEvent.change(campo, { target: { value: 'jutlândia' } })
  fireEvent.submit(campo.closest('form')!)

  expect(await screen.findByRole('heading', { name: 'Busca' })).toBeInTheDocument()
  expect(f).toHaveBeenCalledWith('/api/busca?q=jutl%C3%A2ndia')
})

test('sem termo não consulta a API', () => {
  const f = vi.fn()
  vi.stubGlobal('fetch', f)

  abrir('/busca')

  expect(screen.getByText(/Digite um nome/)).toBeInTheDocument()
  expect(f).not.toHaveBeenCalled()
})

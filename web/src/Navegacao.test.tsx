import { fireEvent, render, screen, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'

function resposta(corpo: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: async () => corpo }
}

function simularApi(regras: [string, unknown, number?][]) {
  const f = vi.fn(async (url: string) => {
    const regra = regras.find(([prefixo]) => url.startsWith(prefixo))
    return regra ? resposta(regra[1], regra[2] ?? 200) : resposta({}, 404)
  })
  vi.stubGlobal('fetch', f)
  return f
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

const filtros = {
  forcas: [
    { codigo: 'MARINHA', nome: 'Marinha' },
    { codigo: 'AEREA', nome: 'Força Aérea' },
    { codigo: 'EXERCITO', nome: 'Exército' },
  ],
  categorias: [],
  paises: [{ codigo: 'RUS', nome: 'Rússia' }],
  status: [],
  paisesEventos: [{ codigo: 'USA', nome: 'Estados Unidos' }],
  tiposEvento: [],
}

const lista = (itens: unknown[]) => ({ itens, pagina: 1, tamanhoPagina: 100, total: itens.length, totalPaginas: 1 })

const guerra = {
  slug: 'segunda-guerra-mundial', nome: 'Segunda Guerra Mundial', tipo: 'GUERRA',
  dataInicio: '1939-09-01', dataFim: '1945-09-02',
  subeventos: [
    {
      slug: 'batalha-de-midway', nome: 'Batalha de Midway', tipo: 'BATALHA',
      dataInicio: '1942-06-04', dataFim: '1942-06-07', subeventos: [],
    },
  ],
}

test('linha do tempo mostra as guerras com seus subeventos e links', async () => {
  simularApi([
    ['/api/filtros', filtros],
    ['/api/linha-do-tempo', { raizes: [guerra] }],
  ])

  abrir('/linha-do-tempo')

  expect(await screen.findByRole('heading', { name: 'Segunda Guerra Mundial' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Batalha de Midway' })).toHaveAttribute('href', '/eventos/batalha-de-midway')
})

test('filtro de força na linha do tempo vai para a URL e é levado ao abrir um país', async () => {
  const f = simularApi([
    ['/api/filtros', filtros],
    ['/api/linha-do-tempo', { raizes: [guerra] }],
    ['/api/equipamentos', lista([])],
    ['/api/eventos', lista([])],
  ])

  abrir('/linha-do-tempo')
  await screen.findByRole('heading', { name: 'Segunda Guerra Mundial' })
  fireEvent.change(await screen.findByLabelText('Força'), { target: { value: 'MARINHA' } })

  // a consulta usa o filtro
  await vi.waitFor(() => expect(f).toHaveBeenCalledWith('/api/linha-do-tempo?forca=MARINHA'))
  // e os links de navegação carregam o filtro
  expect(screen.getByRole('link', { name: 'Países' })).toHaveAttribute('href', '/paises?forca=MARINHA')
  expect(await screen.findByRole('link', { name: 'Batalha de Midway' })).toHaveAttribute(
    'href',
    '/eventos/batalha-de-midway?forca=MARINHA',
  )

  fireEvent.click(screen.getByRole('link', { name: 'Países' }))
  fireEvent.click(await screen.findByRole('link', { name: 'Rússia' }))

  expect(await screen.findByRole('heading', { name: 'Rússia' })).toBeInTheDocument()
  expect(screen.getByLabelText('Força')).toHaveValue('MARINHA')
  expect(screen.getByRole('heading', { name: 'Marinha' })).toBeInTheDocument()
  expect(screen.queryByRole('heading', { name: 'Exército' })).not.toBeInTheDocument()
})

test('página do país tem uma seção por força com equipamentos e eventos', async () => {
  simularApi([
    ['/api/filtros', filtros],
    [
      '/api/equipamentos?pais=RUS&forca=EXERCITO',
      lista([{
        slug: 't-90', nome: 'T-90', variante: null, pais: { codigo: 'RUS', nome: 'Rússia' }, forca: 'EXERCITO',
        categoria: { codigo: 'TANQUE', nome: 'Tanque' }, anoEntradaServico: 1992, status: 'ATIVO',
        confiabilidade: 'ESTIMADO', descricaoCurta: null, imagem: null,
      }]),
    ],
    ['/api/equipamentos', lista([])],
    [
      '/api/eventos?pais=RUS&forca=EXERCITO',
      lista([{
        slug: 'guerra-x', nome: 'Guerra X', tipo: 'GUERRA', dataInicio: '2022-02-24', dataFim: null,
        local: null, confiabilidade: 'ESTIMADO', eventoPai: null,
      }]),
    ],
    ['/api/eventos', lista([])],
  ])

  abrir('/paises/RUS')

  expect(await screen.findByRole('heading', { name: 'Rússia' })).toBeInTheDocument()
  const exercito = (await screen.findByRole('heading', { name: 'Exército' })).closest('section')!
  expect(await within(exercito).findByRole('link', { name: /T-90/ })).toHaveAttribute('href', '/equipamentos/t-90')
  expect(within(exercito).getByRole('link', { name: 'Guerra X' })).toHaveAttribute('href', '/eventos/guerra-x')
  const marinha = screen.getByRole('heading', { name: 'Marinha' }).closest('section')!
  expect(await within(marinha).findByText(/Nada cadastrado/)).toBeInTheDocument()
})

test('país desconhecido mostra mensagem com caminhos alternativos', async () => {
  simularApi([['/api/filtros', filtros]])

  abrir('/paises/ZZZ')

  expect(await screen.findByRole('heading', { name: 'País não encontrado' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'lista de países' })).toHaveAttribute('href', '/paises')
})

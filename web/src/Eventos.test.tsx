import { render, screen, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'
import { formatarPeriodo } from './rotulos'

function resposta(corpo: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: async () => corpo }
}

function simularApi(regras: [string, unknown, number?][]) {
  const fetchSimulado = vi.fn(async (url: string) => {
    const regra = regras.find(([prefixo]) => url.startsWith(prefixo))
    return regra ? resposta(regra[1], regra[2] ?? 200) : resposta({}, 404)
  })
  vi.stubGlobal('fetch', fetchSimulado)
  return fetchSimulado
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
  forcas: [],
  categorias: [],
  paises: [],
  status: [],
  paisesEventos: [
    { codigo: 'USA', nome: 'Estados Unidos' },
    { codigo: 'JPN', nome: 'Japão' },
  ],
  tiposEvento: ['GUERRA', 'CAMPANHA', 'BATALHA'],
}

const guerra = {
  slug: 'segunda-guerra-mundial',
  nome: 'Segunda Guerra Mundial',
  tipo: 'GUERRA',
  dataInicio: '1939-09-01',
  dataFim: '1945-09-02',
  local: 'Mundial',
  confiabilidade: 'ESTIMADO',
  eventoPai: null,
}

const midway = {
  slug: 'batalha-de-midway',
  nome: 'Batalha de Midway',
  tipo: 'BATALHA',
  dataInicio: '1942-06-04',
  dataFim: '1942-06-07',
  local: null,
  confiabilidade: 'ESTIMADO',
  eventoPai: { slug: 'segunda-guerra-mundial', nome: 'Segunda Guerra Mundial', tipo: 'GUERRA' },
}

const detalheGuerra = {
  ...guerra,
  descricao: 'Conflito global.',
  resultado: 'Vitória dos Aliados',
  somenteFontesProvisorias: true,
  beligerantes: [
    { lado: 'Aliados', paises: [{ codigo: 'USA', nome: 'Estados Unidos' }] },
    { lado: 'Eixo', paises: [{ codigo: 'JPN', nome: 'Japão' }] },
  ],
  subeventos: [
    {
      slug: 'batalha-de-midway',
      nome: 'Batalha de Midway',
      tipo: 'BATALHA',
      dataInicio: '1942-06-04',
      dataFim: '1942-06-07',
      subeventos: [],
    },
  ],
  participacoes: [
    {
      pais: { codigo: 'USA', nome: 'Estados Unidos' },
      forcas: [
        {
          forca: 'AEREA',
          equipamentos: [
            {
              slug: 'sbd-dauntless',
              nome: 'SBD Dauntless',
              categoria: { codigo: 'AERONAVE', nome: 'Aeronave' },
              papel: 'Bombardeiro de mergulho',
              quantidadeAprox: null,
              observacao: null,
            },
          ],
        },
      ],
    },
  ],
  fontes: [
    {
      slug: 'f',
      tipo: 'SITE',
      titulo: 'Fonte provisória',
      autor: null,
      ano: null,
      url: null,
      provisoria: true,
      referencia: null,
    },
  ],
}

test('formata o período, incluindo evento em andamento', () => {
  expect(formatarPeriodo('1939-09-01', '1945-09-02')).toBe('01/09/1939 a 02/09/1945')
  expect(formatarPeriodo('2022-02-24', null)).toBe('24/02/2022 até o presente')
  expect(formatarPeriodo('1942-06-04', '1942-06-04')).toBe('04/06/1942')
})

test('lista eventos com período e evento pai', async () => {
  simularApi([
    ['/api/filtros', filtros],
    ['/api/eventos', { itens: [guerra, midway], pagina: 1, tamanhoPagina: 24, total: 2, totalPaginas: 1 }],
  ])

  abrir('/eventos')

  const link = await screen.findByRole('link', { name: 'Batalha de Midway' })
  expect(link).toHaveAttribute('href', '/eventos/batalha-de-midway')
  expect(screen.getByText(/Guerra · 01\/09\/1939 a 02\/09\/1945/)).toBeInTheDocument()
  const paiLinks = screen.getAllByRole('link', { name: 'Segunda Guerra Mundial' })
  expect(paiLinks.some((l) => l.getAttribute('href') === '/eventos/segunda-guerra-mundial')).toBe(true)
})

test('envia os filtros da URL para a API de eventos', async () => {
  const api = simularApi([
    ['/api/filtros', filtros],
    ['/api/eventos', { itens: [guerra], pagina: 1, tamanhoPagina: 24, total: 1, totalPaginas: 1 }],
  ])

  abrir('/eventos?de=1939&ate=1945&tipo=GUERRA')

  await screen.findByRole('link', { name: 'Segunda Guerra Mundial' })
  expect(api).toHaveBeenCalledWith('/api/eventos?tipo=GUERRA&de=1939&ate=1945')
})

test('lista vazia de eventos mostra mensagem e limpar filtros', async () => {
  simularApi([
    ['/api/filtros', filtros],
    ['/api/eventos', { itens: [], pagina: 1, tamanhoPagina: 24, total: 0, totalPaginas: 0 }],
  ])

  abrir('/eventos?tipo=BATALHA')

  expect(await screen.findByText('Nenhum evento encontrado.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Limpar filtros' })).toBeInTheDocument()
})

test('página do evento mostra beligerantes por lado, subeventos e equipamentos por país', async () => {
  simularApi([['/api/eventos/segunda-guerra-mundial', detalheGuerra]])

  abrir('/eventos/segunda-guerra-mundial')

  expect(await screen.findByRole('heading', { name: 'Segunda Guerra Mundial' })).toBeInTheDocument()
  expect(screen.getByText('Aliados')).toBeInTheDocument()
  expect(screen.getByText('Eixo')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Batalha de Midway' })).toHaveAttribute(
    'href',
    '/eventos/batalha-de-midway',
  )
  const equipamento = screen.getByRole('link', { name: 'SBD Dauntless' })
  expect(equipamento).toHaveAttribute('href', '/equipamentos/sbd-dauntless')
  expect(equipamento.closest('li')).toHaveTextContent('Bombardeiro de mergulho')
  expect(screen.getByRole('heading', { name: 'Estados Unidos' })).toBeInTheDocument()
  expect(screen.getByText('Força Aérea')).toBeInTheDocument()
  expect(screen.getByText(/fonte provisória/)).toBeInTheDocument()
})

test('batalha mostra o link para o evento pai', async () => {
  simularApi([
    [
      '/api/eventos/batalha-de-midway',
      {
        ...detalheGuerra,
        ...midway,
        beligerantes: [],
        subeventos: [],
        participacoes: [],
        fontes: [],
      },
    ],
  ])

  abrir('/eventos/batalha-de-midway')

  const cabecalho = (await screen.findByRole('heading', { name: 'Batalha de Midway' })).closest('article')!
  const pai = within(cabecalho).getAllByRole('link', { name: 'Segunda Guerra Mundial' })[0]
  expect(pai).toHaveAttribute('href', '/eventos/segunda-guerra-mundial')
  expect(screen.getByText('Nenhum equipamento cadastrado para este evento.')).toBeInTheDocument()
})

test('evento inexistente mostra não encontrado', async () => {
  simularApi([['/api/eventos/nao-existe', { erro: 'x' }, 404]])

  abrir('/eventos/nao-existe')

  expect(await screen.findByRole('heading', { name: 'Evento não encontrado' })).toBeInTheDocument()
})

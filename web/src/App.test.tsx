import { fireEvent, render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test, vi } from 'vitest'
import App from './App'

function resposta(corpo: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: async () => corpo }
}

const filtros = {
  forcas: [
    { codigo: 'MARINHA', nome: 'Marinha' },
    { codigo: 'AEREA', nome: 'Força Aérea' },
    { codigo: 'EXERCITO', nome: 'Exército' },
  ],
  categorias: [
    { codigo: 'NAVIO', nome: 'Navio', forca: 'MARINHA' },
    { codigo: 'AERONAVE', nome: 'Aeronave', forca: 'AEREA' },
    { codigo: 'TANQUE', nome: 'Tanque', forca: 'EXERCITO' },
  ],
  paises: [
    { codigo: 'RUS', nome: 'Rússia' },
    { codigo: 'USA', nome: 'Estados Unidos' },
  ],
  status: ['ATIVO', 'APOSENTADO', 'PROTOTIPO'],
}

const tanque = {
  slug: 't-14-armata',
  nome: 'T-14 Armata',
  variante: null,
  pais: { codigo: 'RUS', nome: 'Rússia' },
  forca: 'EXERCITO',
  categoria: { codigo: 'TANQUE', nome: 'Tanque' },
  anoEntradaServico: 2015,
  status: 'ATIVO',
  confiabilidade: 'ESTIMADO',
  descricaoCurta: null,
  imagem: null,
}

const ficha = {
  ...tanque,
  fabricante: null,
  anoPrimeiroUso: null,
  unidadesProduzidas: null,
  curiosidade: null,
  somenteFontesProvisorias: true,
  operadores: [{ codigo: 'RUS', nome: 'Rússia' }],
  especificacoes: {
    medidas: [
      {
        chave: 'velocidade_max_kmh',
        rotulo: 'Velocidade máxima',
        valor: 75,
        unidade: 'km/h',
        convertidos: [{ valor: 46.6, unidade: 'mph' }],
      },
    ],
    campos: [{ chave: 'torre_tripulada', rotulo: 'Torre tripulada', valor: 'Não', itens: null }],
  },
  eventos: [
    {
      evento: { slug: 'guerra-do-golfo', nome: 'Guerra do Golfo', tipo: 'GUERRA' },
      dataInicio: '1990-08-02',
      dataFim: '1991-02-28',
      pais: { codigo: 'USA', nome: 'Estados Unidos' },
      papel: 'ataque ao solo e apoio aéreo aproximado',
      quantidadeAprox: null,
      observacao: null,
    },
  ],
  fontes: [
    {
      slug: 'acervo',
      tipo: 'SITE',
      titulo: 'Acervo do autor',
      autor: null,
      ano: null,
      url: null,
      provisoria: true,
      referencia: null,
    },
  ],
}

/** fetch simulado: responde por prefixo de URL (a primeira regra que casar vence). */
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

test('início mostra o título e os cards das três forças', () => {
  simularApi([])

  abrir('/')

  expect(screen.getByRole('heading', { name: 'Poder Bélico' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: /EXÉRCITO/ })).toHaveAttribute('href', '/equipamentos?forca=EXERCITO')
  expect(screen.getByRole('link', { name: /AERONÁUTICA/ })).toHaveAttribute('href', '/equipamentos?forca=AEREA')
  expect(screen.getByRole('link', { name: /MARINHA/ })).toHaveAttribute('href', '/equipamentos?forca=MARINHA')
})

test('lista equipamentos com os dados principais', async () => {
  simularApi([
    ['/api/filtros', filtros],
    ['/api/equipamentos', { itens: [tanque], pagina: 1, tamanhoPagina: 24, total: 1, totalPaginas: 1 }],
  ])

  abrir('/equipamentos')

  const link = await screen.findByRole('link', { name: /T-14 Armata/ })
  expect(link).toHaveAttribute('href', '/equipamentos/t-14-armata')
  expect(screen.getByText(/Rússia · Exército · Tanque/)).toBeInTheDocument()
})

test('envia os filtros da URL para a API', async () => {
  const api = simularApi([
    ['/api/filtros', filtros],
    ['/api/equipamentos', { itens: [tanque], pagina: 1, tamanhoPagina: 24, total: 1, totalPaginas: 1 }],
  ])

  abrir('/equipamentos?forca=EXERCITO&pais=RUS')

  await screen.findByRole('link', { name: /T-14 Armata/ })
  expect(api).toHaveBeenCalledWith('/api/equipamentos?forca=EXERCITO&pais=RUS')
})

test('lista vazia mostra mensagem e permite limpar os filtros', async () => {
  simularApi([
    ['/api/filtros', filtros],
    ['/api/equipamentos', { itens: [], pagina: 1, tamanhoPagina: 24, total: 0, totalPaginas: 0 }],
  ])

  abrir('/equipamentos?forca=MARINHA')

  expect(await screen.findByText('Nenhum equipamento encontrado.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Limpar filtros' })).toBeInTheDocument()
})

test('ficha mostra a medida convertida, o selo e a fonte provisória', async () => {
  simularApi([['/api/equipamentos/t-14-armata', ficha]])

  abrir('/equipamentos/t-14-armata')

  expect(await screen.findByRole('heading', { name: 'T-14 Armata' })).toBeInTheDocument()
  expect(screen.getByText('75 km/h (46,6 mph)')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Dados estimados' })).toHaveAttribute('href', '/metodologia')
  expect(screen.getByText(/fonte provisória/)).toBeInTheDocument()
  expect(screen.getByRole('img', { name: 'Imagem indisponível' })).toBeInTheDocument()
})

test('ficha lista os eventos com papel e link', async () => {
  simularApi([['/api/equipamentos/t-14-armata', ficha]])

  abrir('/equipamentos/t-14-armata')

  const link = await screen.findByRole('link', { name: 'Guerra do Golfo' })
  expect(link).toHaveAttribute('href', '/eventos/guerra-do-golfo')
  expect(link.closest('li')).toHaveTextContent('ataque ao solo e apoio aéreo aproximado')
  expect(link.closest('li')).toHaveTextContent('02/08/1990 a 28/02/1991')
})

test('ficha sem eventos mostra a mensagem de lista vazia', async () => {
  simularApi([['/api/equipamentos/t-14-armata', { ...ficha, eventos: [] }]])

  abrir('/equipamentos/t-14-armata')

  expect(
    await screen.findByText('Nenhum evento cadastrado para este equipamento.'),
  ).toBeInTheDocument()
})

test('slug inexistente mostra não encontrado com link para a busca', async () => {
  simularApi([['/api/equipamentos/nao-existe', { erro: 'x' }, 404]])

  abrir('/equipamentos/nao-existe')

  expect(await screen.findByRole('heading', { name: 'Equipamento não encontrado' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Buscar equipamentos' })).toHaveAttribute('href', '/busca')
})

test('rodapé leva à página de metodologia', async () => {
  simularApi([])

  abrir('/busca')
  fireEvent.click(screen.getByRole('link', { name: /Metodologia/ }))

  expect(await screen.findByRole('heading', { name: 'Metodologia' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Níveis de confiabilidade' })).toBeInTheDocument()
})

test('ficha confirmada não mostra selo de alerta', async () => {
  simularApi([['/api/equipamentos/t-14-armata', { ...ficha, confiabilidade: 'CONFIRMADO' }]])

  abrir('/equipamentos/t-14-armata')

  expect(await screen.findByRole('heading', { name: 'T-14 Armata' })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: /Dados (estimados|contestados)/ })).not.toBeInTheDocument()
  expect(screen.getByText('Confirmado')).toBeInTheDocument()
})

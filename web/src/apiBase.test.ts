import { afterEach, expect, it, vi } from 'vitest'
import { buscarFiltros, urlPublica } from './api'

afterEach(() => { vi.unstubAllEnvs(); vi.unstubAllGlobals() })

it('consulta a API dentro do subdiretório publicado', async () => {
  vi.stubEnv('BASE_URL', '/poderbelico/')
  const request = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ forcas: [] }) })
  vi.stubGlobal('fetch', request)
  await buscarFiltros()
  expect(request).toHaveBeenCalledWith('/poderbelico/api/filtros')
})

it('urlPublica prefixa caminhos publicos com a base do site', () => {
  vi.stubEnv('BASE_URL', '/poderbelico/')
  expect(urlPublica('/img/a.jpg')).toBe('/poderbelico/img/a.jpg')
  expect(urlPublica('https://x.com/a.jpg')).toBe('https://x.com/a.jpg')
})

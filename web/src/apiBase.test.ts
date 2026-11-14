import { afterEach, expect, it, vi } from 'vitest'
import { buscarFiltros } from './api'

afterEach(() => { vi.unstubAllEnvs(); vi.unstubAllGlobals() })

it('consulta a API dentro do subdiretório publicado', async () => {
  vi.stubEnv('BASE_URL', '/poderbelico/')
  const request = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ forcas: [] }) })
  vi.stubGlobal('fetch', request)
  await buscarFiltros()
  expect(request).toHaveBeenCalledWith('/poderbelico/api/filtros')
})

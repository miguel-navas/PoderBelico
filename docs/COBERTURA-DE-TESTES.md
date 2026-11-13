# Cobertura de testes por capability

Teste .NET: `dotnet test` na raiz. Teste da interface: `cd web; npx vitest run`.

| Capability | API / importador (`tests/PoderBelico.Api.Tests`) | Interface (`web/src`) |
|---|---|---|
| importacao-de-dados | `ImportadorTests`, `ImportadorEventosTests`, `SementeTests`, `EspecificacoesValidadorTests` | — |
| catalogo-equipamentos | `EquipamentosApiTests`, `ConversoesTests` | `App.test.tsx` (lista, ficha, conversão, 404) |
| catalogo-eventos | `EventosApiTests` (lista, filtros, árvore, em andamento) | `Eventos.test.tsx` |
| participacao-equipamento-evento | `ImportadorEventosTests` (integridade), `EventosApiTests`, `EquipamentosApiTests` | `App.test.tsx` (eventos na ficha), `Eventos.test.tsx` (equipamentos no evento) |
| fontes-e-confiabilidade | `ImportadorTests` (fonte obrigatória, rebaixamento) | `App.test.tsx` (selo, fontes, metodologia) |
| navegacao-e-busca | `BuscaApiTests`, `EventosApiTests` (força e linha do tempo) | `Busca.test.tsx`, `Navegacao.test.tsx` |

## Telas pequenas e acessibilidade (9.3)

Verificado em Chrome com tela de 360 px, usando dados com textos muito longos: nenhuma das 10 páginas
(início, equipamentos, ficha, eventos, evento, linha do tempo, países, país, busca, metodologia) passou
da largura da tela. O axe-core não achou violações nelas (tema claro).

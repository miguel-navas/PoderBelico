# Design: Layout e identidade visual

## Decisões
1. **Tema escuro único**, herdado da página inicial. Sem tema claro no MVP.
2. **CSS puro com variáveis** (`:root`), sem biblioteca de UI. O projeto já usa
   `index.css`, `catalogo.css` e `inicio.css`; os tokens ficam em `tokens.css`
   e as demais folhas passam a consumi-los.
3. **Lista em cartões com foto** (não tabela): as imagens são o maior ativo do
   acervo. Uma tabela compacta pode virar visualização alternativa depois.
4. **Sem mudança de API**: `EquipamentoResumo.imagem` já traz `url` e `credito`.
5. **Componentes de layout** novos em `web/src/layout/`: `Topo`, `Rodape`,
   `Pagina` (envolve conteúdo com largura máxima e título).
6. **Estado sem imagem**: o cartão usa um painel com a silhueta da categoria
   (ícone) em vez de imagem quebrada.

## Tokens (valores iniciais)
| Token | Valor | Uso |
|---|---|---|
| `--fundo` | `#080b0e` | fundo da página |
| `--superficie` | `#11161b` | cartões, cabeçalho |
| `--superficie-2` | `#171e25` | campos, hover |
| `--borda` | `#2a343d` | contornos |
| `--texto` | `#f4f6f8` | texto principal |
| `--texto-suave` | `#aab4be` | texto secundário (≥ 4,5:1 no fundo) |
| `--destaque` | `#eee3bd` | foco, links ativos, selo |
| `--marinha` / `--aerea` / `--exercito` | `#4aa3df` / `#8fb8de` / `#9bb37a` | marcador de força |
| `--raio` | `8px` | cantos |
| Espaçamento | escala 4/8/12/16/24/32/48 | margens e vãos |
| Fonte de título | Stencil, Impact, sans-serif | h1/h2, igual à inicial |
| Fonte de texto | system-ui, sans-serif | corpo |

## Breakpoints
- Celular: até 600 px. Tablet: 601–1023 px. Desktop: 1024 px ou mais.
- Grade de cartões: 1 coluna (celular), 2 (tablet), 3 (desktop), 4 (≥ 1500 px).
- Largura máxima do conteúdo: 1200 px.

## Riscos
- Os testes procuram elementos por papel e texto: manter `role="search"`,
  `aria-label="Principal"`, `aria-label="Filtros"`, `aria-label="Paginação"`
  e os textos de erro/vazio atuais.
- Imagens pesam 16–25 MB no total: usar `loading="lazy"` e `decoding="async"`
  e dimensões fixas para evitar salto de layout.

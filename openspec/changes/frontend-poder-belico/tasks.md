# Tarefas

## 1. Base visual
- [x] 1.1 Criar `tokens.css` com as variáveis do design.md e importá-lo antes das demais folhas
- [x] 1.2 Reescrever `index.css` e `catalogo.css` usando os tokens (remover cores fixas)
- [x] 1.3 Estilos globais: foco visível, `prefers-reduced-motion`, tipografia dos títulos

## 2. Layout compartilhado
- [x] 2.1 Cabeçalho (marca, navegação com item ativo, busca) e rodapé, em `App.tsx` + `catalogo.css` (sem componentes separados)
- [x] 2.2 Versão celular do menu e da busca
- [x] 2.3 Aplicar em `App.tsx` mantendo `role="search"` e `aria-label="Principal"`

## 3. Lista de equipamentos
- [x] 3.1 Cartão com imagem, metadados e selo; fallback por categoria
- [x] 3.2 Grade responsiva (1/2/3/4 colunas)
- [x] 3.3 Painel de filtros (recolhível no celular), contagem e "Limpar filtros"
- [x] 3.4 Paginação estilizada
- [x] 3.5 Estados de carregamento, vazio e erro no estilo do sistema

## 4. Ficha técnica
- [x] 4.1 Imagem em destaque com crédito
- [x] 4.2 Especificações em tabela de duas colunas e fontes em lista
- [x] 4.3 Selo de confiabilidade e aviso de dados estimados

## 4b. Categorias e imagens
- [x] 4b.1 API: filtro `subtipo`, lista de `subtipos` em `/api/filtros` e `subtipo` no resumo (sem migration)
- [x] 4b.2 Front: filtro "Categoria" por subtipo, banner e imagem de categoria nos cartões sem foto
- [x] 4b.3 Imagens por categoria em `web/public/img/categorias/` e imagens dos 4 navios
- [ ] 4b.4 Imagens próprias para categorias ainda sem ficha (fragata, artilharia, lança-mísseis; lança-foguetes já feito)
- [x] 4b.5 Banner da lista também com Categoria = Todas (imagem por força; fundo genérico sem filtro)
- [x] 4b.6 Página de país exibe imagem nos cards (componente ImagemCartao compartilhado)
- [x] 4b.7 Subtipo lanca_foguetes: fichas HIMARS, M270, ASTROS II, BM-21 Grad, BM-13 Katyusha

## 5. Demais páginas
- [ ] 5.1 Eventos, Evento, Linha do tempo no mesmo padrão
- [ ] 5.2 Países, País e Busca no mesmo padrão
- [ ] 5.3 Metodologia legível (largura de leitura ~70 caracteres)

## 6. Verificação
- [ ] 6.1 `npx vitest run` continua verde; ajustar apenas testes presos a marcação antiga
- [ ] 6.2 Conferir 360, 768 e 1280 px sem rolagem horizontal
- [ ] 6.3 Conferir contraste AA e navegação por teclado

# Change: Layout e identidade visual do front-end

## Por quê
A página inicial já segue o mockup (fundo escuro, título em imagem, cartões por
força), mas o restante do site ainda está sem estilo: o cabeçalho é um título
com links soltos, os filtros usam controles nativos, a lista é texto puro e a
ficha não mostra a imagem do equipamento. O acervo já tem imagens (59 aeronaves,
46 blindados) que não aparecem na navegação.

## O que muda
- Criar um sistema visual único (tokens de cor, tipografia, espaçamento) aplicado
  a todas as páginas, derivado da página inicial.
- Novo cabeçalho e rodapé compartilhados, com navegação e busca.
- Lista de equipamentos em grade de cartões com foto, filtros estilizados e
  paginação visível.
- Ficha técnica com imagem em destaque, crédito da imagem e especificações em
  tabela legível.
- Páginas de eventos, linha do tempo, países e busca no mesmo padrão.
- Responsividade (celular, tablet, desktop) e acessibilidade (AA).

## Capabilities
- layout-e-identidade-visual (nova)
- lista-e-ficha-visuais (nova)

## Impacto
Somente `web/`. Nenhuma mudança de API, banco ou importador. Os testes
existentes (Vitest) continuam valendo: papéis, rótulos acessíveis e textos
verificados pelos testes são mantidos.

## Fora do escopo
Tema claro, animações elaboradas, mapas, comparador, painel de administração,
versão em inglês, galeria com várias imagens por equipamento.

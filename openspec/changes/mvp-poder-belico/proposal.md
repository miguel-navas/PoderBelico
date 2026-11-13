# Change: MVP do Poder Bélico

## Por quê
Não existe um acervo em português que una, em um só lugar, a ficha técnica dos
equipamentos militares (marinha, força aérea, exército) e o relato dos eventos
em que foram usados, da 1ª Guerra Mundial até hoje.

## O que muda
- Criar o catálogo de equipamentos com ficha técnica por categoria.
- Criar o catálogo de eventos (guerra, campanha, batalha) com hierarquia.
- Ligar equipamentos e eventos por participação.
- Criar navegação por país, evento e linha do tempo, com busca e filtro por força.
- Exigir fontes e indicar a confiabilidade dos dados.
- Importar os dados por script idempotente a partir de JSON/CSV.

## Capabilities (todas novas)
- catalogo-equipamentos
- catalogo-eventos
- participacao-equipamento-evento
- navegacao-e-busca
- fontes-e-confiabilidade
- importacao-de-dados

## Impacto
Projeto novo. Stack: SQLite, .NET 10, React + TypeScript. Detalhes em
`design.md`.

## Fora do escopo
Comparador, mapas animados, painel de administração, versão em inglês.

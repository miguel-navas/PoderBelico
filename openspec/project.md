# Poder Bélico

## Finalidade
Site de consulta sobre os equipamentos militares usados por marinhas, forças
aéreas e exércitos de cada país, da 1ª Guerra Mundial (1914) até hoje. Cada
equipamento tem uma ficha técnica própria e está ligado aos eventos (batalhas,
campanhas, guerras) em que foi utilizado.

## Público
Estudantes, entusiastas de história militar, jornalistas e pesquisadores.

## Idioma
Português (Brasil). Inglês fica para uma fase posterior.

## Stack
- Banco: SQLite (arquivo único), migrations via EF Core
- Backend: .NET 10, ASP.NET Core Minimal API, EF Core
- Frontend: React + TypeScript (Vite), React Router, TanStack Query
- Busca: SQLite FTS5
- Testes: xUnit (backend), Vitest (frontend)
- Deploy: único, a API serve também o build estático do React

## Domínio
País, Força (marinha, aérea, exército), Categoria, Equipamento, Evento,
Participação (liga equipamento, evento e país), Fonte.

## Convenções
- Nomes de tabelas e campos em português, sem acentos.
- Valores numéricos guardados em unidade padrão (km/h, kg, m, km, t) e
  convertidos apenas na exibição (mph, nós, pés, milhas náuticas).
- Cada dado é um campo próprio (ex.: motor e potência são campos diferentes).
- Tipos double/int para especificações; nunca decimal (SQLite grava como texto).
- Datas em ISO 8601 (texto). Anos como inteiro.
- Todo equipamento e todo evento cita ao menos uma fonte.
- Dado estimado ou contestado recebe indicador de confiabilidade.
- Requisitos das specs usam SHALL/MUST e cenários WHEN/THEN (formato OpenSpec).

## Escopo do MVP
5 países e 5 a 8 eventos; navegação por país, evento e linha do tempo; filtro
por força em todas as páginas; ficha de equipamento e página de evento ligadas
entre si; busca por nome.

## Fora do escopo (por ora)
Comparador de equipamentos, mapas animados, painel de administração, versão em
inglês, contas de usuário.

# Tarefas

## 1. Base do projeto
- [ ] 1.1 Criar solução .NET 10 (API) e projeto React + TypeScript (Vite)
- [ ] 1.2 Configurar EF Core com SQLite e a primeira migration (DDL do design.md)
- [ ] 1.3 Configurar a API para servir o build estático do React
- [ ] 1.4 Configurar xUnit e Vitest

## 2. Dados de referência
- [ ] 2.1 Carregar forças e categorias (HasData) e países (semente: USA, RUS, GBR, DEU, JPN, SUN)
- [ ] 2.2 Esquemas JSON de especificações por categoria (tanque, aeronave, navio) e validador

## 3. Importação (importacao-de-dados)
- [ ] 3.1 Implementar o importador idempotente (JSON/CSV)
- [ ] 3.2 Validar esquema por categoria e exigir fontes
- [ ] 3.3 Gerar relatório de erros e avisos
- [ ] 3.4 Importar as fichas de exemplo (T-14 Armata, A-10 Thunderbolt II)

## 4. Catálogo de equipamentos
- [ ] 4.1 Endpoints de lista (com filtros) e de ficha
- [ ] 4.2 Conversão de unidades na exibição
- [ ] 4.3 Páginas React: lista e ficha técnica

## 5. Catálogo de eventos
- [ ] 5.1 Endpoints de lista e de evento (com hierarquia)
- [ ] 5.2 Páginas React: lista e página do evento

## 6. Participação
- [ ] 6.1 Cadastro e consulta de participações
- [ ] 6.2 Exibir eventos na ficha e equipamentos na página do evento

## 7. Navegação e busca
- [x] 7.1 Navegação por país e linha do tempo
- [x] 7.2 Filtro por força persistente na URL
- [x] 7.3 Índice FTS5 e endpoint de busca
- [x] 7.4 Tela de resultados da busca

## 8. Fontes e confiabilidade
- [x] 8.1 Exibir fontes e selo de confiabilidade nas páginas
- [x] 8.2 Página de metodologia

## 9. Qualidade
- [x] 9.1 Testes de API para cada capability
- [x] 9.2 Testes de interface para ficha, evento e busca
- [x] 9.3 Revisão de acessibilidade e responsividade (mobile)

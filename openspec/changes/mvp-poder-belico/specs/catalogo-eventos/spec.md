## ADDED Requirements

### Requirement: Cadastro de eventos com hierarquia
O sistema SHALL representar eventos dos tipos guerra, campanha e batalha, e
SHALL permitir que um evento pertença a um evento pai (batalha dentro de
campanha, campanha dentro de guerra).

#### Scenario: Batalha dentro de guerra
- **WHEN** o usuário abre a página da Batalha da Jutlândia
- **THEN** o sistema mostra a Primeira Guerra Mundial como evento pai, com link para ela

#### Scenario: Guerra com subeventos
- **WHEN** o usuário abre a página da Segunda Guerra Mundial
- **THEN** o sistema lista as campanhas e batalhas cadastradas dentro dela, em ordem cronológica

### Requirement: Página do evento
O sistema SHALL exibir para cada evento nome, tipo, período, local, descrição,
resultado, países beligerantes com seus lados, e as fontes.

#### Scenario: Beligerantes por lado
- **WHEN** o usuário abre a página de um evento com dois lados
- **THEN** o sistema agrupa os países por lado (por exemplo, Aliados e Eixo)

### Requirement: Listagem de eventos
O sistema SHALL listar eventos em ordem cronológica, com filtros por período,
tipo e país participante.

#### Scenario: Filtrar por período
- **WHEN** o usuário filtra eventos entre 1939 e 1945
- **THEN** a lista mostra apenas eventos cujo período intersecta esse intervalo

### Requirement: Datas parciais e em andamento
O sistema SHALL aceitar evento sem data de fim (em andamento) e exibir "em
andamento" no lugar da data final.

#### Scenario: Evento em andamento
- **WHEN** um evento não tem data de fim
- **THEN** o período aparece como "data de início até o presente"

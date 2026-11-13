## ADDED Requirements

### Requirement: Registro de participação
O sistema SHALL registrar a participação de um equipamento em um evento,
informando o país pelo qual foi usado, o papel desempenhado, a quantidade
aproximada (opcional) e observações (opcional).

#### Scenario: Participação única por país
- **WHEN** é cadastrada uma segunda participação do mesmo equipamento, no mesmo evento, pelo mesmo país
- **THEN** o sistema rejeita o registro duplicado

#### Scenario: Papel obrigatório
- **WHEN** uma participação é cadastrada sem papel
- **THEN** o sistema rejeita o registro e informa o campo faltante

### Requirement: Eventos na ficha do equipamento
O sistema SHALL listar, na ficha do equipamento, os eventos em que ele
participou, com o papel desempenhado e link para o evento.

#### Scenario: A-10 na Guerra do Golfo
- **WHEN** o usuário abre a ficha do A-10 Thunderbolt II
- **THEN** a seção de eventos lista a Guerra do Golfo com o papel "ataque ao solo e apoio aéreo aproximado"

### Requirement: Equipamentos na página do evento
O sistema SHALL listar, na página do evento, os equipamentos que participaram,
agrupados por país e por força.

#### Scenario: Agrupamento por país e força
- **WHEN** o usuário abre a página de um evento com participações de vários países
- **THEN** os equipamentos aparecem agrupados por país e, dentro de cada país, por força

### Requirement: Integridade da participação
O sistema MUST garantir que o país da participação seja beligerante do evento.

#### Scenario: País que não é beligerante
- **WHEN** uma participação é importada com país que não consta nos beligerantes do evento
- **THEN** a importação rejeita o registro e o inclui no relatório de erros

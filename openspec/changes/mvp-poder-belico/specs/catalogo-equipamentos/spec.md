## ADDED Requirements

### Requirement: Ficha técnica do equipamento
O sistema SHALL exibir, para cada equipamento, uma ficha com os campos comuns
(nome, variante, país de origem, força, categoria, fabricante, ano de primeiro
uso, ano de entrada em serviço, unidades produzidas, operadores, status,
descrição curta e curiosidade) e os campos específicos da sua categoria.

#### Scenario: Ficha de aeronave
- **WHEN** o usuário abre a página do A-10 Thunderbolt II
- **THEN** o sistema mostra fabricante, primeiro voo, envergadura, peso máximo de decolagem, velocidade máxima, alcance, teto, armamento e blindagem
- **AND** mostra unidades produzidas e operadores

#### Scenario: Ficha de tanque
- **WHEN** o usuário abre a página do T-14 Armata
- **THEN** o sistema mostra motor, potência, velocidade máxima, armamento principal, peso, tripulação, alcance e se a torre é tripulada

### Requirement: Campos de especificação separados e em unidade padrão
O sistema MUST armazenar cada especificação em campo próprio e em unidade
padrão (km/h, kg, m, t, km), e SHALL converter para outras unidades apenas na
exibição.

#### Scenario: Conversão na exibição
- **WHEN** a ficha de uma aeronave é exibida
- **THEN** a velocidade máxima aparece em km/h e também em mph
- **AND** o valor armazenado permanece em km/h

#### Scenario: Velocidade de navio
- **WHEN** a ficha de um navio é exibida
- **THEN** a velocidade máxima aparece em nós, convertida do valor em km/h

### Requirement: Listagem com filtros
O sistema SHALL listar equipamentos com filtros combináveis por país, força,
categoria, período de entrada em serviço e status, com paginação.

#### Scenario: Filtrar por força e país
- **WHEN** o usuário filtra por força "Exército" e país "Rússia"
- **THEN** a lista mostra apenas equipamentos do exército cujo país de origem é a Rússia

#### Scenario: Sem resultados
- **WHEN** os filtros não correspondem a nenhum equipamento
- **THEN** o sistema mostra uma mensagem de lista vazia e oferece limpar os filtros

### Requirement: Endereço próprio por equipamento
O sistema SHALL atribuir a cada equipamento um slug único e estável usado na URL.

#### Scenario: Equipamento inexistente
- **WHEN** o usuário abre a URL de um slug que não existe
- **THEN** o sistema responde com página de não encontrado (404) e link para a busca

### Requirement: Imagem com crédito
O sistema SHALL exibir o crédito e a licença da imagem de cada equipamento que
possua imagem, e MUST NOT exibir imagem sem crédito registrado.

#### Scenario: Imagem sem crédito
- **WHEN** um equipamento tem imagem mas não tem crédito registrado
- **THEN** o sistema não exibe a imagem e mostra um espaço reservado

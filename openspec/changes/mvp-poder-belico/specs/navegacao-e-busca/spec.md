## ADDED Requirements

### Requirement: Navegação por país
O sistema SHALL oferecer uma página por país com seus equipamentos e os eventos
de que participou, separados por força.

#### Scenario: Página do país
- **WHEN** o usuário abre a página da Rússia
- **THEN** o sistema mostra abas ou seções para marinha, força aérea e exército
- **AND** lista os equipamentos e eventos relacionados a cada uma

### Requirement: Linha do tempo
O sistema SHALL oferecer uma linha do tempo de 1914 até o presente, com os
eventos em ordem cronológica e link para a página de cada um.

#### Scenario: Navegar na linha do tempo
- **WHEN** o usuário abre a linha do tempo
- **THEN** os eventos aparecem agrupados por guerra e ordenados por data de início

### Requirement: Filtro por força em todas as páginas
O sistema SHALL permitir filtrar o conteúdo por força (marinha, aérea,
exército) e MUST refletir o filtro na URL, para que possa ser compartilhado e
mantido ao navegar.

#### Scenario: Filtro persistente
- **WHEN** o usuário seleciona a força "Marinha" na linha do tempo e abre um país
- **THEN** a página do país abre já filtrada por "Marinha"
- **AND** a URL contém o parâmetro do filtro

### Requirement: Busca textual
O sistema SHALL oferecer busca por nome e descrição de equipamentos e eventos,
ignorando maiúsculas, minúsculas e acentos, com resultados agrupados por tipo.

#### Scenario: Busca sem acento
- **WHEN** o usuário busca "jutlandia"
- **THEN** o resultado inclui a Batalha da Jutlândia

#### Scenario: Busca sem resultados
- **WHEN** a busca não encontra nada
- **THEN** o sistema mostra mensagem de nenhum resultado e sugere navegar por país ou linha do tempo

### Requirement: Uso em telas pequenas
O sistema SHALL ser utilizável em telas de celular, com fichas legíveis sem
rolagem horizontal.

#### Scenario: Ficha no celular
- **WHEN** o usuário abre uma ficha técnica em tela de 360 px de largura
- **THEN** os campos aparecem em uma coluna, sem corte de texto ou de etiquetas

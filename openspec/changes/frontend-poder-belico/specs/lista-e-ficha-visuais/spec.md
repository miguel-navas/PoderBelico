## ADDED Requirements

### Requirement: Lista de equipamentos em cartões
O sistema SHALL listar equipamentos em grade de cartões, cada um com imagem,
nome (e variante), país, força, categoria, ano de entrada em serviço, status e
selo de confiabilidade, com o cartão inteiro clicável para a ficha.

#### Scenario: Grade responsiva
- **WHEN** a largura é de 1280 px
- **THEN** a lista mostra 3 cartões por linha

#### Scenario: Equipamento sem imagem
- **WHEN** o equipamento não tem imagem
- **THEN** o cartão mostra um painel com o ícone da categoria, sem imagem quebrada

#### Scenario: Imagem gerada por IA
- **WHEN** o cartão exibe uma imagem com crédito "Imagem gerada por IA"
- **THEN** o texto do crédito é visível no cartão ou no texto alternativo da imagem

### Requirement: Filtros estilizados e visíveis
O sistema SHALL apresentar os filtros (força, categoria, país, status, ano) em
um painel acima da lista, com rótulos visíveis, contagem de resultados e botão
para limpar filtros, mantendo o estado na URL.

#### Scenario: Filtro compartilhável
- **WHEN** o usuário escolhe a força "Aérea" e copia a URL
- **THEN** quem abrir a URL vê a lista já filtrada

#### Scenario: Filtros no celular
- **WHEN** a largura é de até 600 px
- **THEN** os filtros ficam em um painel recolhível, fechado por padrão, com o número de filtros ativos

### Requirement: Ficha técnica com imagem em destaque
O sistema SHALL mostrar na ficha uma imagem em destaque no topo com seu
crédito, seguida de resumo, especificações em tabela de duas colunas e fontes.

#### Scenario: Ficha com imagem
- **WHEN** o usuário abre a ficha do F-14 Tomcat
- **THEN** a imagem aparece no topo com a legenda "Imagem gerada por IA"
- **AND** as especificações aparecem em tabela com rótulo e valor

#### Scenario: Selo de confiabilidade
- **WHEN** a ficha tem confiabilidade ESTIMADO
- **THEN** o selo aparece perto do título, com texto (não só cor)

### Requirement: Paginação visível
O sistema SHALL exibir a paginação ao fim da lista com botões Anterior e
Próxima, a página atual e o total, desabilitando o botão sem destino.

#### Scenario: Primeira página
- **WHEN** o usuário está na página 1
- **THEN** o botão "Anterior" está desabilitado

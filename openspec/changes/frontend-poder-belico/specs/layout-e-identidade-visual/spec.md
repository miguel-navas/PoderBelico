## ADDED Requirements

### Requirement: Sistema visual único
O sistema SHALL aplicar a todas as páginas, exceto a inicial que mantém seu
layout próprio, o mesmo tema escuro, a mesma tipografia e a mesma escala de
espaçamento, definidos como variáveis CSS.

#### Scenario: Páginas consistentes
- **WHEN** o usuário navega da lista para a ficha, eventos e países
- **THEN** cabeçalho, cores, fontes e espaçamentos são os mesmos em todas

### Requirement: Cabeçalho e rodapé compartilhados
O sistema SHALL exibir em todas as páginas internas um cabeçalho com a marca
"Poder Bélico" (link para a inicial), a navegação principal e o campo de busca,
e um rodapé com o link para a metodologia e fontes.

#### Scenario: Item de menu ativo
- **WHEN** o usuário está em Equipamentos
- **THEN** o item "Equipamentos" aparece destacado e marcado com `aria-current="page"`

#### Scenario: Menu no celular
- **WHEN** a largura da tela é de até 600 px
- **THEN** a navegação quebra em linhas ou vira um botão de menu sem rolagem horizontal
- **AND** o campo de busca continua acessível

### Requirement: Responsividade
O sistema SHALL se adaptar a celular, tablet e desktop sem rolagem horizontal
da página e sem sobreposição de elementos.

#### Scenario: Celular
- **WHEN** a largura é de 360 px
- **THEN** o conteúdo ocupa uma coluna e os alvos de toque medem ao menos 44 px

### Requirement: Acessibilidade
O sistema SHALL manter contraste mínimo AA (4,5:1 para texto), foco visível em
todos os controles, navegação completa por teclado e respeitar
`prefers-reduced-motion`.

#### Scenario: Foco visível
- **WHEN** o usuário navega por Tab
- **THEN** o elemento focado mostra contorno na cor de destaque

### Requirement: Estados de carregamento, vazio e erro
O sistema SHALL mostrar, de forma consistente em todas as páginas de dados,
estados de carregamento, lista vazia (com ação para limpar filtros) e erro.

#### Scenario: Erro de API
- **WHEN** a API não responde
- **THEN** a página mostra a mensagem de erro com `role="alert"` no estilo do sistema visual

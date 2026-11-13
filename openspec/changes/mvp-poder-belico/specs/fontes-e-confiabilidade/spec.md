## ADDED Requirements

### Requirement: Fonte obrigatória
O sistema MUST exigir ao menos uma fonte para cada equipamento e cada evento,
e SHALL exibir as fontes na página correspondente.

#### Scenario: Equipamento sem fonte
- **WHEN** um equipamento é importado sem nenhuma fonte
- **THEN** a importação rejeita o registro e o inclui no relatório de erros

#### Scenario: Exibição das fontes
- **WHEN** o usuário abre a ficha de um equipamento
- **THEN** o sistema lista as fontes com título, autor, ano e link (quando houver)

### Requirement: Indicador de confiabilidade
O sistema SHALL classificar equipamentos e eventos como confirmado, estimado ou
contestado, e SHALL exibir um selo visível quando o dado não for confirmado.

#### Scenario: Equipamento com dados estimados
- **WHEN** o usuário abre a ficha de um equipamento marcado como "estimado"
- **THEN** o sistema mostra um selo "dados estimados" no topo da ficha
- **AND** o selo tem link para a página de metodologia

#### Scenario: Equipamento confirmado
- **WHEN** o equipamento é marcado como "confirmado"
- **THEN** nenhum selo de alerta é exibido

### Requirement: Página de metodologia
O sistema SHALL ter uma página que explica como os dados são coletados, o
significado de cada nível de confiabilidade, as unidades usadas e como enviar
correções.

#### Scenario: Acesso à metodologia
- **WHEN** o usuário clica no selo de confiabilidade ou no rodapé
- **THEN** a página de metodologia é aberta

### Requirement: Fonte provisória
O sistema SHALL permitir marcar uma fonte como provisória (origem não
verificada, como uma conversa com assistente de IA ou uma anotação própria),
SHALL exibir o aviso "fonte a verificar" ao lado dela, e MUST NOT aceitar
apenas fontes provisórias como base para a confiabilidade "confirmado".

#### Scenario: Só fonte provisória, declarada como confirmada
- **WHEN** uma ficha declarada "confirmado" é importada com apenas fontes provisórias
- **THEN** o sistema grava a ficha como "estimado"
- **AND** o relatório de importação traz um aviso sobre o rebaixamento

#### Scenario: Fonte provisória e fonte definitiva
- **WHEN** uma ficha "confirmado" tem uma fonte provisória e uma fonte definitiva
- **THEN** a confiabilidade "confirmado" é mantida

#### Scenario: Exibição do aviso
- **WHEN** o usuário abre uma ficha com fonte provisória
- **THEN** a lista de fontes mostra "fonte a verificar" ao lado dessa fonte

### Requirement: Confiabilidade explícita
O sistema MUST exigir que cada ficha de equipamento e de evento declare a sua
confiabilidade, sem valor padrão.

#### Scenario: Ficha sem confiabilidade
- **WHEN** uma ficha é importada sem o campo de confiabilidade
- **THEN** o registro é rejeitado e o erro aponta o campo e o slug


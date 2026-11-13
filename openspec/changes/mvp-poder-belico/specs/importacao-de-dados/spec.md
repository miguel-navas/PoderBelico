## ADDED Requirements

### Requirement: Formato de entrada em JSON
O sistema SHALL importar fontes, equipamentos, eventos e participações a
partir de arquivos JSON e MUST rejeitar arquivos em outro formato nesta versão.

#### Scenario: Arquivo JSON válido
- **WHEN** o importador recebe um arquivo JSON com fichas de equipamentos
- **THEN** as fichas são processadas

#### Scenario: Arquivo CSV
- **WHEN** o importador recebe um arquivo .csv
- **THEN** o arquivo é rejeitado com uma mensagem informando que só JSON é aceito

### Requirement: Importação idempotente
O sistema SHALL identificar equipamentos, eventos e fontes pelo slug, e
participações pelo trio equipamento, evento e país, de modo que executar a
mesma importação duas vezes não crie duplicatas. A importação SHALL substituir
o registro inteiro e MUST NOT apagar registros que não constam no arquivo.

#### Scenario: Reimportar o mesmo arquivo
- **WHEN** o mesmo arquivo é importado duas vezes
- **THEN** o número de registros no banco é o mesmo após a segunda execução

#### Scenario: Atualizar um registro
- **WHEN** um arquivo traz um slug já existente com valores diferentes
- **THEN** o registro existente é atualizado com os novos valores

#### Scenario: Campo omitido na reimportação
- **WHEN** uma ficha é reimportada sem o campo "curiosidade" que antes existia
- **THEN** a curiosidade do registro fica vazia

#### Scenario: Registro ausente do arquivo
- **WHEN** um equipamento já gravado não consta no novo arquivo
- **THEN** ele permanece no banco

### Requirement: Ordem de carga e referências
O sistema SHALL processar fontes, equipamentos, eventos (o pai antes do
filho), participações e o índice de busca, nessa ordem, e MUST rejeitar o
registro que referencia país, força, categoria, fonte, evento pai ou
equipamento inexistente.

#### Scenario: Referência inexistente
- **WHEN** uma ficha traz o país "XXX" que não existe no banco
- **THEN** o registro é rejeitado e o erro aponta o campo e o slug

#### Scenario: Evento filho antes do pai no arquivo
- **WHEN** o arquivo lista uma batalha antes da guerra que a contém
- **THEN** o importador grava primeiro a guerra e depois a batalha

### Requirement: Validação por categoria
O sistema MUST validar as especificações de cada equipamento contra o esquema
da sua categoria e rejeitar registros com campos desconhecidos, ausentes
obrigatórios ou fora da unidade padrão.

#### Scenario: Campo desconhecido
- **WHEN** uma ficha de tanque traz o campo "envergadura_m"
- **THEN** o registro é rejeitado e o erro aponta o campo e o slug

#### Scenario: Tipo de valor inválido
- **WHEN** a velocidade máxima vem como texto ("706 km/h") em vez de número
- **THEN** o registro é rejeitado e o relatório indica o valor esperado

### Requirement: Gravação por registro com cascata
O sistema SHALL gravar cada registro válido de forma independente e MUST
rejeitar o registro que depende de outro rejeitado, informando o motivo
"dependência rejeitada".

#### Scenario: Importação com erros parciais
- **WHEN** um arquivo tem 10 registros e 2 são inválidos
- **THEN** os 8 válidos são gravados
- **AND** o relatório lista os 2 rejeitados com o motivo

#### Scenario: Evento rejeitado
- **WHEN** um evento é rejeitado e há participações que o referenciam
- **THEN** essas participações são rejeitadas com o motivo "dependência rejeitada"

### Requirement: Modo estrito
Com a opção `--estrito`, o sistema MUST desfazer toda a importação se qualquer
registro for rejeitado.

#### Scenario: Estrito com um registro inválido
- **WHEN** a importação roda com `--estrito` e um de 10 registros é inválido
- **THEN** nenhum registro é gravado
- **AND** o relatório lista o registro inválido

### Requirement: Modo simulação
Com a opção `--simular`, o sistema SHALL validar e relatar sem gravar nada.

#### Scenario: Simulação
- **WHEN** a importação roda com `--simular`
- **THEN** o relatório mostra o que seria criado, atualizado e rejeitado
- **AND** o banco permanece inalterado

### Requirement: Falha de infraestrutura
O sistema SHALL executar cada importação dentro de uma transação e desfazer
toda a execução se ocorrer falha de infraestrutura (e não de validação).

#### Scenario: Falha durante a gravação
- **WHEN** ocorre erro de gravação no meio da importação
- **THEN** nenhum registro da execução permanece no banco

### Requirement: Relatório de importação
O sistema SHALL gerar, ao fim de cada importação, um relatório com a
quantidade de registros criados, atualizados, ignorados e rejeitados e a lista
de erros e avisos por registro, e SHALL encerrar com código de saída diferente
de zero se houver registro rejeitado.

#### Scenario: Código de saída
- **WHEN** a importação termina com pelo menos um registro rejeitado
- **THEN** o código de saída do importador é diferente de zero

### Requirement: Índice de busca
O sistema SHALL atualizar a tabela de busca para cada equipamento e evento
criado ou alterado pela importação.

#### Scenario: Busca após a importação
- **WHEN** um equipamento novo é importado
- **THEN** a busca pelo nome dele o encontra, ignorando acentos

### Requirement: API sem escrita
A API MUST NOT expor rotas de escrita; a importação é feita somente pelo
importador de linha de comando.

#### Scenario: Tentativa de escrita pela API
- **WHEN** um cliente envia POST, PUT ou DELETE para uma rota de /api
- **THEN** a API responde que o método não é permitido ou que a rota não existe

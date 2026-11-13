# Poder Bélico

Acervo de equipamentos militares (marinha, força aérea e exército) de cada país,
da 1ª Guerra Mundial até hoje, com ficha técnica por equipamento e ligação com os
eventos em que foram usados.

Stack: SQLite, .NET 10 (Minimal API + EF Core), React + TypeScript (Vite).
Especificação (OpenSpec) em `openspec/`.

## Requisitos
- .NET 10 SDK
- Node.js 22 ou mais novo
- PowerShell (para os scripts `.ps1`)

## Primeira vez
```powershell
./configurar.ps1
```
Restaura os pacotes, gera a migration inicial (`Inicial`) e instala o front.

## Desenvolvimento (dois terminais)
```powershell
dotnet run --project src/PoderBelico.Api --urls http://localhost:5080
cd web; npm run dev
```
Em Development, a API aplica as migrations e carrega a semente na subida
(forças e categorias vêm da migration; países vêm de `dados/referencia/paises.json`).
Abra http://localhost:5173.

## Migration após atualizar o modelo
Depois de baixar uma versão em que o modelo mudou (ex.: `Fonte` ganhou `slug` e `provisoria`):
```powershell
dotnet ef migrations add FonteSlugProvisoria -p src/PoderBelico.Dados -s src/PoderBelico.Api -o Migrations
```
A API (Development) ou o importador aplicam a migration na próxima execução.

## Importar fichas
```powershell
./importar.ps1 dados/exemplos --simular          # valida sem gravar fichas
./importar.ps1 dados/exemplos                    # grava T-14 e A-10
./importar.ps1 dados/importacao/aeronaves dados/importacao/blindados dados/importacao/navios
./importar.ps1 dados/exemplos-eventos            # Segunda Guerra, Midway, Guerra do Golfo e participações
```
Um arquivo pode misturar fontes, equipamentos, eventos e participações. Ordem de gravação:
fontes → equipamentos → eventos (pai antes do filho) → participações. Participação é identificada
por equipamento + evento + país; o país precisa ser beligerante do evento (senão é rejeitada).
Aviso (sem rejeitar): país fora dos operadores do equipamento; período do evento fora do período do pai.
Só JSON. Opções: `--estrito` (qualquer rejeição desfaz tudo), `--simular`, `--banco`,
`--paises`, `--relatorio arquivo.json`. Código de saída 1 se algum registro for rejeitado.
Ficha só com fonte provisória nunca fica "confirmado" (é gravada como "estimado", com aviso).

## API (somente leitura)
```
GET /api/equipamentos?pais=&forca=&categoria=&status=&de=&ate=&pagina=&tamanho=
GET /api/equipamentos/{slug}     ficha com medidas convertidas (mph, nós, pés, libras...) e fontes
GET /api/eventos?tipo=&pais=&forca=&de=&ate=&pagina=   eventos em ordem cronológica (período cruza de..ate)
GET /api/eventos/{slug}          evento, evento pai, subeventos, beligerantes por lado,
                                 equipamentos por país e força, fontes
GET /api/linha-do-tempo?forca=&pais=   guerras com seus subeventos em árvore
GET /api/busca?q=                texto sem acento nem maiúsculas; equipamentos e eventos
GET /api/filtros                 forças, categorias, países e tipos para os filtros
```
O banco guarda tudo em unidade padrão (km/h, kg, m, km); a conversão é só na exibição.

## Testes
```powershell
dotnet test
cd web; npx vitest run
```

## Publicar
```powershell
./publicar.ps1
```
Gera o build do React, copia para o `wwwroot` da API e publica em `./publicado`.

## Estrutura
```
dados/referencia/paises.json   países do MVP (USA, RUS, GBR, DEU, JPN, SUN)
dados/exemplos-eventos/         exemplo de eventos (Segunda Guerra, Midway, Golfo) e participações
dados/exemplos/                fichas de exemplo (T-14 Armata, A-10) no formato de importação
dados/importacao/              fichas convertidas dos catálogos do Meta AI (fonte provisória, estimadas)
ferramentas/                   conversor único dos catálogos .md para o formato do importador
openspec/                      proposta, design, tarefas e specs (6 capabilities)
src/PoderBelico.Dados/         entidades, DbContext, esquemas JSON, validador, semente
src/PoderBelico.Importador/    importador de linha de comando (fontes, equipamentos, eventos e participações; JSON)
src/PoderBelico.Api/           API (rota /api/saude) e serviço do build do React
tests/PoderBelico.Api.Tests/   testes xUnit
web/                           front React
```

## Estado atual
- O MVP está completo: todas as tarefas de `openspec/changes/mvp-poder-belico/tasks.md` estão marcadas.
- Os testes .NET e os do front passam. O mapa de cobertura está em `docs/COBERTURA-DE-TESTES.md`.
- Os pacotes NuGet usam versões flutuantes (`10.0.*`, `11.*`). Vale fixar as versões exatas.

## Revisar nas fichas de exemplo
- **T-14:** status (`ATIVO`), anos e fabricante não vinham da sua ficha. Confirme.
  Os dados estão marcados como `ESTIMADO`. A frase "primeiro tanque de série com
  torre totalmente não tripulada" é discutível: confirme com fonte.
- **Fontes:** as duas fichas citam "acervo do autor" como fonte provisória. Troque
  pela fonte original (a spec exige pelo menos uma fonte por equipamento).

## Próximos passos
Substituir as fontes provisórias por fontes verificáveis, separar os submarinos Virginia e Ohio,
cadastrar os blindados que faltam (`dados/importacao/BLINDADOS-SEM-DADOS.md`) e ampliar países e eventos.

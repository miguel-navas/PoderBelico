# Importa fichas JSON para o banco. Exemplos:
#   ./importar.ps1 dados/exemplos --simular
#   ./importar.ps1 dados/importacao/aeronaves dados/importacao/blindados dados/importacao/navios
#   ./importar.ps1 dados/importacao --estrito --relatorio relatorio.json
# Opções: veja  ./importar.ps1 --ajuda
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet run --project src/PoderBelico.Importador -- @args
exit $LASTEXITCODE

# Primeira configuração do projeto: pacotes, migration inicial e dependências do front.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

dotnet restore

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    dotnet tool install --global dotnet-ef
}

if (-not (Test-Path "src/PoderBelico.Dados/Migrations")) {
    dotnet ef migrations add Inicial -p src/PoderBelico.Dados -s src/PoderBelico.Api -o Migrations
}

Push-Location web
try { npm install } finally { Pop-Location }

Write-Host ""
Write-Host "Pronto. Rode os testes com:  dotnet test   e   (na pasta web) npm test -- --run"
Write-Host "Para desenvolver: veja o LEIA-ME.md"

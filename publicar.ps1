# Gera o build do React, copia para o wwwroot da API e publica tudo em .\publicado
$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot

Push-Location "$raiz/web"
try {
    npm ci
    npm run build
}
finally {
    Pop-Location
}

$destino = "$raiz/src/PoderBelico.Api/wwwroot"
if (Test-Path $destino) { Remove-Item "$destino/*" -Recurse -Force }
New-Item -ItemType Directory -Force $destino | Out-Null
Copy-Item "$raiz/web/dist/*" $destino -Recurse

dotnet publish "$raiz/src/PoderBelico.Api" -c Release -o "$raiz/publicado"
Write-Host "Pronto: $raiz/publicado"

# Publicação na VM

- URL: https://nivel77.com.br/poderbelico/
- Google Cloud: projeto `strong-hue-161116`, VM `slotfy-demo`, zona `us-central1-c`.
- Serviço: `poderbelico.service`, usuário `www-data`, porta local `18083`.
- Versões: `/var/www/poderbelico/releases/`; a ligação `current` seleciona a versão ativa.
- Banco persistente: `/var/lib/poderbelico/poder-belico.db`.
- Nginx: `/etc/nginx/snippets/poderbelico.conf`, incluído no servidor HTTPS de `nivel77`.
- Backups da configuração: `/var/backups/poderbelico/`.

## Build

O front usa `PUBLIC_BASE_PATH=/poderbelico/` durante o build. O Vite, o BrowserRouter,
os recursos da página inicial e as consultas à API respeitam esse prefixo.
Sem essa variável, o desenvolvimento continua funcionando na raiz.

```powershell
cd C:\AI\PoderBelico\web
$env:PUBLIC_BASE_PATH = '/poderbelico/'
npm run build
Remove-Item Env:PUBLIC_BASE_PATH
cd ..
dotnet publish src/PoderBelico.Api -c Release -r linux-x64 --self-contained true -o output/deploy/release
```

Copiar `web/dist` para `output/deploy/release/wwwroot` antes de empacotar.
O pacote Linux inclui o runtime .NET, sem mudar o runtime dos outros sites da VM.
A publicação inicial usa um snapshot consistente do SQLite feito com `sqlite3.Connection.backup`.
O instalador não sobrescreve um banco persistente já existente.

## Instalação e diagnóstico

O instalador desta publicação está em `output/deploy/instalar-poderbelico.sh`.
Ele cria o serviço e a configuração de proxy, testa a saúde da API e executa `nginx -t`
antes de recarregar o Nginx. Uma falha aciona a restauração da configuração anterior.

```sh
sudo systemctl status poderbelico
sudo journalctl -u poderbelico -n 50 --no-pager
curl -fsS https://nivel77.com.br/poderbelico/api/saude
```

A API fica acessível somente pelo proxy HTTPS, no prefixo `/poderbelico/api/`.
A rota sem barra final redireciona para `/poderbelico/`.

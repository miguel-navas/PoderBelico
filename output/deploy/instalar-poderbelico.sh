#!/usr/bin/env bash
set -Eeuo pipefail
APP=/var/www/poderbelico
DATA=/var/lib/poderbelico
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
RELEASE="$APP/releases/$STAMP"
BACKUP="/var/backups/poderbelico/$STAMP"
CONFIG=$(readlink -f /etc/nginx/sites-enabled/nivel77)
PREVIOUS=$(readlink -f "$APP/current" 2>/dev/null || true)
mkdir -p "$BACKUP" "$APP/releases" "$DATA"
cp -a "$CONFIG" "$BACKUP/nivel77.conf"
if [ -f /etc/nginx/snippets/poderbelico.conf ]; then cp -a /etc/nginx/snippets/poderbelico.conf "$BACKUP/poderbelico.conf"; fi
if [ -f /etc/systemd/system/poderbelico.service ]; then cp -a /etc/systemd/system/poderbelico.service "$BACKUP/poderbelico.service"; fi
rollback() {
  echo 'Falha na publicação; restaurando a configuração anterior.' >&2
  cp -a "$BACKUP/nivel77.conf" "$CONFIG"
  if [ -f "$BACKUP/poderbelico.conf" ]; then cp -a "$BACKUP/poderbelico.conf" /etc/nginx/snippets/poderbelico.conf; fi
  if [ -n "$PREVIOUS" ] && [ -d "$PREVIOUS" ]; then
    ln -sfn "$PREVIOUS" "$APP/current"
    if [ -f "$BACKUP/poderbelico.service" ]; then cp -a "$BACKUP/poderbelico.service" /etc/systemd/system/poderbelico.service; fi
    systemctl daemon-reload
    systemctl restart poderbelico || true
  else
    systemctl stop poderbelico || true
  fi
  nginx -t && systemctl reload nginx
}
trap rollback ERR
mkdir -p "$RELEASE"
tar -xzf /tmp/poderbelico-linux-x64.tar.gz -C "$RELEASE"
chmod +x "$RELEASE/PoderBelico.Api"
if [ ! -f "$DATA/poder-belico.db" ]; then
  cp "$RELEASE/banco-inicial.db" "$DATA/poder-belico.db"
fi
chown -R www-data:www-data "$DATA"
chmod 750 "$DATA"
chmod 640 "$DATA/poder-belico.db"
ln -sfn "$RELEASE" "$APP/current"
cat > /etc/systemd/system/poderbelico.service <<'UNIT'
[Unit]
Description=Poder Belico - catalogo militar
After=network.target

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/var/www/poderbelico/current
ExecStart=/var/www/poderbelico/current/PoderBelico.Api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:18083
Environment="ConnectionStrings__Padrao=Data Source=/var/lib/poderbelico/poder-belico.db"
Environment=Banco__AplicarMigracoes=false
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/lib/poderbelico

[Install]
WantedBy=multi-user.target
UNIT
systemctl daemon-reload
systemctl enable poderbelico
systemctl restart poderbelico
ready=0
for attempt in $(seq 1 40); do
  if curl --max-time 5 -fsS http://127.0.0.1:18083/api/saude > "$BACKUP/saude.json"; then ready=1; break; fi
  sleep 1
done
if [ "$ready" != 1 ]; then journalctl -u poderbelico -n 40 --no-pager; false; fi
cat > /etc/nginx/snippets/poderbelico.conf <<'NGINX'
# Poder Belico: front e API isolados no subdiretorio.
location = /poderbelico { return 301 /poderbelico/; }
location ^~ /poderbelico/ {
    proxy_pass http://127.0.0.1:18083/;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    add_header X-Content-Type-Options nosniff always;
}
NGINX
python3 - "$CONFIG" <<'PY'
from pathlib import Path
import sys
path=Path(sys.argv[1]); content=path.read_text()
include='    include /etc/nginx/snippets/poderbelico.conf;'
if include not in content:
    anchor='    include /etc/nginx/snippets/juridico.conf;'
    assert content.count(anchor)==1, 'Ponto de inclusao do Nginx divergente'
    path.write_text(content.replace(anchor,anchor+'\n'+include))
PY
nginx -t
systemctl reload nginx
ready=0
for attempt in $(seq 1 30); do
  if curl --max-time 10 -fsS https://nivel77.com.br/poderbelico/api/saude > "$BACKUP/saude-publica.json" && python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); assert d["status"]=="ok" and d["banco"]' "$BACKUP/saude-publica.json" 2>/dev/null; then ready=1; break; fi
  sleep 1
done
[ "$ready" = 1 ]
cat "$BACKUP/saude-publica.json"
curl --max-time 15 -fsS 'https://nivel77.com.br/poderbelico/api/equipamentos?tamanho=1' > "$BACKUP/equipamentos.json"
python3 - "$BACKUP/equipamentos.json" <<'PY'
import json,sys
payload=json.load(open(sys.argv[1]))
assert payload['total']>0
print('\nEquipamentos publicados:', payload['total'])
PY
trap - ERR
printf 'Publicacao concluida: %s\nBackup: %s\n' "$RELEASE" "$BACKUP"

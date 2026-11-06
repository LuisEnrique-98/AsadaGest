#!/bin/bash
# Actualiza el sistema en el servidor a la ultima version de la rama master (Sprint 6a):
#
#   cd /opt/asadagest/despliegue && bash actualizar.sh
#
# 1. Descarga los cambios de GitHub (solo avanza; si hay cambios locales, se detiene).
# 2. Construye las imagenes nuevas (el sistema sigue funcionando mientras tanto).
# 3. Hace un respaldo de la base ANTES de migrar (por si la migracion sale mal).
# 4. Aplica las migraciones pendientes.
# 5. Reemplaza los contenedores (la aplicacion se interrumpe unos segundos; las sesiones se mantienen).
# Conviene hacerlo fuera del horario de atencion. Para volver atras ver LEEME.md, "Si algo sale mal".
set -euo pipefail
cd "$(dirname "$0")"

[ -f .env ] || { echo "No existe despliegue/.env: la primera vez use instalar.sh."; exit 1; }

echo "== 1/5 Descargando cambios =="
antes="$(git rev-parse --short HEAD)"
git pull --ff-only
despues="$(git rev-parse --short HEAD)"
if [ "$antes" = "$despues" ]; then
    echo "No hay cambios nuevos ($despues). Igual se reconstruye y se verifica."
else
    echo "Version $antes -> $despues:"
    git log --oneline "$antes..$despues"
fi

echo
echo "== 2/5 Construyendo imagenes =="
docker compose build

echo
echo "== 3/5 Respaldo previo =="
if docker compose ps --status running --services | grep -qx respaldo; then
    docker compose exec -T respaldo bash /opt/respaldos/respaldar.sh
else
    echo "El contenedor de respaldo no esta corriendo; se levanta para respaldar."
    docker compose up -d respaldo
    sleep 10
    docker compose exec -T respaldo bash /opt/respaldos/respaldar.sh
fi

echo
echo "== 4/5 Migraciones =="
docker compose run --rm app migrar

echo
echo "== 5/5 Reemplazando contenedores =="
docker compose up -d --remove-orphans
# El contenedor de respaldo copia los scripts al arrancar: se reinicia por si cambiaron.
docker compose restart respaldo
docker image prune -f > /dev/null

sleep 10
docker compose ps
echo
echo "Actualizado a $despues."

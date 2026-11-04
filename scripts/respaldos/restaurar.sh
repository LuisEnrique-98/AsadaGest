#!/bin/bash
# Restaura un respaldo (Sprint 5b).
#
# Por defecto restaura en una base NUEVA (asada_gestion_restaurada), sin tocar la base en uso, para
# revisar o recuperar datos puntuales:
#
#   docker compose exec respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump
#
# Para reemplazar la base en uso (recuperacion ante un desastre): detenga primero la aplicacion y
# confirme con CONFIRMAR=SI. La base actual se borra y se reemplaza por el respaldo:
#
#   docker compose exec -e CONFIRMAR=SI respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump asada_gestion
#
# Antes de reemplazar, se hace un respaldo de la base actual (por si se eligio el archivo equivocado).
set -euo pipefail
source "$(dirname "$0")/comun.sh"

archivo="${1:-}"
destino="${2:-${PGDATABASE}_restaurada}"

if [ -z "$archivo" ]; then
    echo "Uso: restaurar.sh ARCHIVO.dump [BASE_DESTINO]"
    echo "Respaldos disponibles (los mas recientes al final):"
    ls -1 "$DIR_DIARIOS"/*.dump "$DIR_SEMANALES"/*.dump 2>/dev/null | sed "s|^$RESPALDO_DIR/|  /respaldos/|"
    exit 1
fi

verificar_respaldo "$archivo"

if [ "$destino" = "$PGDATABASE" ]; then
    if [ "${CONFIRMAR:-}" != "SI" ]; then
        echo "Esto BORRA la base en uso ($PGDATABASE) y la reemplaza por el respaldo."
        echo "Detenga la aplicacion y repita el comando con:  -e CONFIRMAR=SI"
        exit 1
    fi
    anotar "Respaldo previo a la restauracion de $PGDATABASE"
    bash "$(dirname "$0")/respaldar.sh"
fi

anotar "Restaurando $(basename "$archivo") en la base $destino"
dropdb --if-exists --force "$destino"
createdb "$destino"
pg_restore --no-owner --exit-on-error --dbname="$destino" "$archivo"
anotar "Restauracion terminada: base $destino"

if [ "$destino" != "$PGDATABASE" ]; then
    echo "Listo. Revise los datos con:  docker compose exec postgres psql -U ${PGUSER:-asada} -d $destino"
    echo "Cuando ya no la necesite:     docker compose exec respaldo dropdb $destino"
fi

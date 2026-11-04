#!/bin/bash
# Hace un respaldo completo de la base ahora mismo (Sprint 5b).
#
#   docker compose exec respaldo bash /opt/respaldos/respaldar.sh
#
# - pg_dump en formato personalizado (-Fc): comprimido, y permite restaurar la base completa o
#   tablas sueltas con pg_restore.
# - Se escribe primero como .parcial y se renombra solo si pg_restore lo puede leer: nunca queda un
#   respaldo a medias con nombre de respaldo bueno.
# - Junto a cada respaldo queda su suma SHA-256, para detectar un archivo danado antes de restaurarlo.
# - Retencion: los ultimos RESPALDO_DIARIOS diarios y, una vez por semana, una copia en semanales/
#   (se conservan los ultimos RESPALDO_SEMANALES).
set -euo pipefail
source "$(dirname "$0")/comun.sh"

marca="$(date +%Y-%m-%d_%H%M%S)"
destino="$DIR_DIARIOS/${PGDATABASE}_${marca}.dump"
parcial="$destino.parcial"

anotar "Respaldo de $PGDATABASE iniciado"
trap 'rm -f "$parcial"; anotar "ERROR el respaldo de $PGDATABASE fallo"' ERR

pg_dump --format=custom --compress=6 --file="$parcial"
pg_restore --list "$parcial" > /dev/null
mv "$parcial" "$destino"
(cd "$DIR_DIARIOS" && sha256sum "$(basename "$destino")" > "$(basename "$destino").sha256")
trap - ERR

tamano="$(du -h "$destino" | cut -f1)"
anotar "Respaldo listo: diarios/$(basename "$destino") ($tamano)"

# Copia semanal: si en semanales/ no hay ninguna de los ultimos 7 dias (asi no se pierde la semana
# aunque la computadora haya estado apagada el dia previsto).
if [ -z "$(find "$DIR_SEMANALES" -name '*.dump' -mtime -7 2>/dev/null)" ]; then
    cp "$destino" "$destino.sha256" "$DIR_SEMANALES/"
    anotar "Copia semanal: semanales/$(basename "$destino")"
fi

# Retencion: se borran los mas antiguos, siempre conservando los N mas recientes.
borrar_antiguos() {
    local carpeta="$1" conservar="$2" archivo
    ls -1 "$carpeta"/*.dump 2>/dev/null | sort -r | tail -n +"$((conservar + 1))" | while read -r archivo; do
        rm -f "$archivo" "$archivo.sha256"
        anotar "Borrado por antiguedad: $(basename "$carpeta")/$(basename "$archivo")"
    done
}
borrar_antiguos "$DIR_DIARIOS" "$RESPALDO_DIARIOS"
borrar_antiguos "$DIR_SEMANALES" "$RESPALDO_SEMANALES"

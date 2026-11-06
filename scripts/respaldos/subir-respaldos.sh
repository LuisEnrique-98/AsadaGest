#!/bin/bash
# Copia los respaldos a uno o varios destinos fuera del servidor con rclone (Sprint 6a): Google Drive,
# OneDrive, almacenamiento S3 (Backblaze B2, Wasabi...) o cualquier otro que rclone soporte. Un
# respaldo que solo esta en el servidor se pierde junto con el servidor.
#
# Los destinos se indican en RESPALDO_DESTINOS (despliegue/.env), separados por espacios, con la forma
# REMOTO:CARPETA; cada REMOTO se configura antes con "rclone config" (ver despliegue/LEEME.md):
#
#   RESPALDO_DESTINOS="gdrive:RespaldosASADA onedrive:RespaldosASADA"
#
# El contenedor de respaldo lo ejecuta solo (con --pendientes) despues de cada respaldo y, si un destino
# fallo, lo reintenta cada hora. A mano, desde despliegue/:
#
#   docker compose exec respaldo bash /opt/respaldos/subir-respaldos.sh                    (todos los destinos)
#   docker compose exec respaldo bash /opt/respaldos/subir-respaldos.sh gdrive:RespaldosASADA   (solo uno)
#
# En cada destino quedan las carpetas diarios/ y semanales/ con los .dump y sus .sha256. Se copian solo
# los archivos nuevos (rclone compara tamano y fecha) y nunca se borra en el destino algo por haberse
# borrado en el servidor: si el disco del servidor se pierde, las copias siguen ahi. En el destino solo
# se borran los respaldos mas antiguos que RESPALDO_REMOTO_DIAS_DIARIOS / _SEMANALES dias, y solo
# despues de subir con exito los nuevos.
set -uo pipefail
source "$(dirname "$0")/comun.sh"

solo_pendientes=no
if [ "${1:-}" = "--pendientes" ]; then solo_pendientes=si; shift; fi

if [ $# -gt 0 ]; then
    destinos=("$@")
else
    read -r -a destinos <<< "${RESPALDO_DESTINOS:-}"
fi

if [ ${#destinos[@]} -eq 0 ]; then
    [ "$solo_pendientes" = si ] || echo "No hay destinos configurados (RESPALDO_DESTINOS en despliegue/.env)."
    exit 0
fi

if ! command -v rclone > /dev/null; then
    anotar "ERROR rclone no esta instalado en este contenedor: no se pueden subir respaldos a ${destinos[*]}"
    exit 1
fi

DIAS_DIARIOS="${RESPALDO_REMOTO_DIAS_DIARIOS:-$RESPALDO_DIARIOS}"
DIAS_SEMANALES="${RESPALDO_REMOTO_DIAS_SEMANALES:-$((RESPALDO_SEMANALES * 7))}"
REINTENTO_MINUTOS="${RESPALDO_REINTENTO_MINUTOS:-60}"

# Archivos de control por destino (en la carpeta de respaldos, ocultos):
#   .subido-X   nombre del ultimo respaldo diario que se subio bien a ese destino
#   .intento-X  existe solo si el ultimo intento fallo (para reintentar una vez por hora, no cada 5 min)
marca() { echo "$RESPALDO_DIR/.$1-$(echo "$2" | tr -c 'A-Za-z0-9\n' '_')"; }

# Un destino esta pendiente si el respaldo diario mas reciente todavia no se subio ahi.
pendiente() {
    local ultimo
    ultimo="$(ultimo_respaldo)"
    [ -n "$ultimo" ] || return 1
    [ "$(cat "$(marca subido "$1")" 2>/dev/null)" != "$(basename "$ultimo")" ]
}

intento_reciente() {
    local intento
    intento="$(marca intento "$1")"
    [ -f "$intento" ] && [ -n "$(find "$intento" -mmin "-$REINTENTO_MINUTOS")" ]
}

# rclone escribe su detalle en respaldos.log; en pantalla queda solo el resumen de anotar.
rclone_registrado() {
    rclone "$@" --log-level NOTICE --stats 0 --retries 3 --low-level-retries 10 >> "$REGISTRO" 2>&1
}

filtros=(--include '*.dump' --include '*.dump.sha256')

subir() {
    local destino="$1" carpeta dias
    touch "$(marca intento "$destino")"
    anotar "Subiendo respaldos a $destino"

    for carpeta in diarios semanales; do
        if ! rclone_registrado copy "$RESPALDO_DIR/$carpeta" "$destino/$carpeta" "${filtros[@]}"; then
            anotar "ERROR no se pudieron subir los respaldos $carpeta a $destino (detalle en respaldos.log); se reintenta en $REINTENTO_MINUTOS minutos"
            return 1
        fi
    done

    # Comprueba que el respaldo mas reciente llego completo (compara la suma o el tamano, segun lo que
    # permita el destino).
    local ultimo
    ultimo="$(ultimo_respaldo)"
    if [ -n "$ultimo" ] && ! rclone_registrado check "$DIR_DIARIOS" "$destino/diarios" --one-way --include "$(basename "$ultimo")"; then
        anotar "ERROR $(basename "$ultimo") no coincide en $destino (detalle en respaldos.log); se reintenta en $REINTENTO_MINUTOS minutos"
        return 1
    fi

    for carpeta in diarios semanales; do
        if [ "$carpeta" = diarios ]; then dias="$DIAS_DIARIOS"; else dias="$DIAS_SEMANALES"; fi
        rclone lsf "$destino/$carpeta" --max-depth 1 > /dev/null 2>&1 || continue   # carpeta aun vacia
        rclone_registrado delete "$destino/$carpeta" --min-age "${dias}d" "${filtros[@]}" \
            || anotar "Aviso: no se pudieron borrar respaldos antiguos de $destino/$carpeta (detalle en respaldos.log)"
    done

    basename "${ultimo:-ninguno}" > "$(marca subido "$destino")"
    rm -f "$(marca intento "$destino")"
    anotar "Respaldos subidos a $destino (ultimo: $(basename "${ultimo:-ninguno}"))"
}

fallos=0
for destino in "${destinos[@]}"; do
    destino="${destino%/}"   # "gdrive:Carpeta/" y "gdrive:Carpeta" son el mismo destino
    if [ "$solo_pendientes" = si ]; then
        pendiente "$destino" || continue
        intento_reciente "$destino" && continue
    fi
    subir "$destino" || fallos=$((fallos + 1))
done

[ "$fallos" -eq 0 ]

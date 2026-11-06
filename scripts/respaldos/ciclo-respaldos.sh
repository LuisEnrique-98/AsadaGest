#!/bin/bash
# Proceso del contenedor "respaldo" (Sprint 5b, ver docker-compose.yml):
#   - Al arrancar, si hoy todavia no hay respaldo, hace uno (por si la computadora estuvo apagada a la
#     hora prevista).
#   - Despues revisa cada 5 minutos: pasada la RESPALDO_HORA, si hoy no hay respaldo, lo hace.
#   - Una vez por semana, despues de un respaldo, ejecuta la prueba de restauracion.
#   - (Sprint 6a) Si hay destinos en RESPALDO_DESTINOS, copia los respaldos nuevos fuera del servidor
#     (subir-respaldos.sh); si un destino falla, lo reintenta cada hora.
# Se revisa cada pocos minutos en vez de "dormir hasta la hora" para que una computadora suspendida
# no se salte el respaldo del dia.
set -uo pipefail
source "$(dirname "$0")/comun.sh"

MARCA_PRUEBA="$RESPALDO_DIR/.ultima-prueba-restauracion"
hora_objetivo="${RESPALDO_HORA/:/}"

hoy_hay_respaldo() { ls "$DIR_DIARIOS/${PGDATABASE}_$(date +%Y-%m-%d)_"*.dump > /dev/null 2>&1; }

respaldar_y_probar() {
    bash "$(dirname "$0")/respaldar.sh" || return 1
    if [ ! -f "$MARCA_PRUEBA" ] || [ -n "$(find "$MARCA_PRUEBA" -mtime +6)" ]; then
        bash "$(dirname "$0")/probar-restauracion.sh" && touch "$MARCA_PRUEBA"
    fi
    subir_pendientes
}

# Sin destinos configurados no hace nada (como en la PC de desarrollo).
subir_pendientes() {
    [ -n "${RESPALDO_DESTINOS// /}" ] || return 0
    bash "$(dirname "$0")/subir-respaldos.sh" --pendientes || true
}

anotar "Servicio de respaldos iniciado (respaldo diario a las $RESPALDO_HORA; se conservan $RESPALDO_DIARIOS diarios y $RESPALDO_SEMANALES semanales)"
[ -z "${RESPALDO_DESTINOS// /}" ] || anotar "Copias fuera del servidor en: $RESPALDO_DESTINOS"
until pg_isready --quiet; do sleep 5; done

hoy_hay_respaldo || respaldar_y_probar

while true; do
    if [ "$(date +%H%M)" -ge "$hora_objetivo" ] && ! hoy_hay_respaldo; then
        respaldar_y_probar || anotar "ERROR el respaldo diario fallo; se reintenta en 5 minutos"
    fi
    subir_pendientes
    sleep 300
done

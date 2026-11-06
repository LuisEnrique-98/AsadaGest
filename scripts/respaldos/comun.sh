#!/bin/bash
# Valores comunes de los scripts de respaldo (Sprint 5b). La conexion a PostgreSQL se toma de las
# variables estandar PGHOST, PGPORT, PGDATABASE, PGUSER y PGPASSWORD (las define docker-compose.yml).

RESPALDO_DIR="${RESPALDO_DIR:-/respaldos}"
RESPALDO_HORA="${RESPALDO_HORA:-02:30}"          # hora local del respaldo diario (HH:MM)
RESPALDO_DIARIOS="${RESPALDO_DIARIOS:-30}"       # cuantos respaldos diarios se conservan
RESPALDO_SEMANALES="${RESPALDO_SEMANALES:-12}"   # cuantos respaldos semanales se conservan (~3 meses)
RESPALDO_DESTINOS="${RESPALDO_DESTINOS:-}"       # Sprint 6a: destinos de rclone fuera del servidor
PGDATABASE="${PGDATABASE:-asada_gestion}"
export PGDATABASE

DIR_DIARIOS="$RESPALDO_DIR/diarios"
DIR_SEMANALES="$RESPALDO_DIR/semanales"
REGISTRO="$RESPALDO_DIR/respaldos.log"

mkdir -p "$DIR_DIARIOS" "$DIR_SEMANALES"

# Anota un mensaje con fecha y hora en la salida (docker compose logs respaldo) y en respaldos.log.
anotar() {
    local linea
    linea="$(date '+%Y-%m-%d %H:%M:%S') $*"
    echo "$linea"
    echo "$linea" >> "$REGISTRO"
}

# Respaldo diario mas reciente (vacio si no hay ninguno). Los nombres llevan fecha y hora, asi que el
# orden alfabetico es el orden cronologico.
ultimo_respaldo() {
    ls -1 "$DIR_DIARIOS"/*.dump 2>/dev/null | sort | tail -n 1
}

# Verifica un respaldo contra su archivo .sha256 (si lo tiene) y que pg_restore lo pueda leer.
verificar_respaldo() {
    local archivo="$1"
    [ -f "$archivo" ] || { anotar "ERROR no existe el archivo $archivo"; return 1; }
    if [ -f "$archivo.sha256" ]; then
        (cd "$(dirname "$archivo")" && sha256sum --check --status "$(basename "$archivo").sha256") \
            || { anotar "ERROR el archivo $archivo no coincide con su suma de verificacion (danado o modificado)"; return 1; }
    fi
    pg_restore --list "$archivo" > /dev/null \
        || { anotar "ERROR pg_restore no puede leer $archivo"; return 1; }
}

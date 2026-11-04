#!/bin/bash
# Prueba de restauracion (Sprint 5b): restaura un respaldo en una base temporal, comprueba que trae
# las tablas y datos esperados, y borra la base temporal. Un respaldo que nunca se probo no es un
# respaldo. El contenedor la ejecuta sola una vez por semana; tambien se puede correr a mano:
#
#   docker compose exec respaldo bash /opt/respaldos/probar-restauracion.sh                 (el mas reciente)
#   docker compose exec respaldo bash /opt/respaldos/probar-restauracion.sh /respaldos/diarios/ARCHIVO.dump
#
# El resultado queda en respaldos/pruebas-restauracion.log (una linea por prueba, OK o FALLO).
set -uo pipefail
source "$(dirname "$0")/comun.sh"

PRUEBA="${PGDATABASE}_prueba_restauracion"
RESULTADOS="$RESPALDO_DIR/pruebas-restauracion.log"
archivo="${1:-$(ultimo_respaldo)}"

resultado() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') $1 $(basename "${archivo:-ninguno}") $2" >> "$RESULTADOS"
    anotar "Prueba de restauracion: $1 $2"
}

if [ -z "$archivo" ]; then resultado FALLO "no hay ningun respaldo para probar"; exit 1; fi
verificar_respaldo "$archivo" || { resultado FALLO "el archivo no paso la verificacion"; exit 1; }

dropdb --if-exists --force "$PRUEBA" 2>/dev/null
createdb "$PRUEBA" || { resultado FALLO "no se pudo crear la base temporal $PRUEBA"; exit 1; }
trap 'dropdb --if-exists --force "$PRUEBA" 2>/dev/null' EXIT

if ! pg_restore --no-owner --exit-on-error --dbname="$PRUEBA" "$archivo" 2>> "$REGISTRO"; then
    resultado FALLO "pg_restore fallo (detalle en respaldos.log)"; exit 1
fi

consultar() { psql --dbname="$PRUEBA" --no-psqlrc --tuples-only --no-align --command="$1" 2>/dev/null; }

# Ultima migracion aplicada (primera columna del historial de EF Core).
migracion="$(consultar 'SELECT h.* FROM "__EFMigrationsHistory" h ORDER BY 1 DESC LIMIT 1' | cut -d'|' -f1)"
if [ -z "$migracion" ]; then resultado FALLO "el respaldo no trae el historial de migraciones"; exit 1; fi

conteos=""
for tabla in organizaciones usuarios abonados servicios mediciones recibos auditoria; do
    n="$(consultar "SELECT count(*) FROM $tabla")" || n=""
    if [ -z "$n" ]; then resultado FALLO "falta la tabla $tabla"; exit 1; fi
    conteos="$conteos $tabla=$n"
done

if [ "$(consultar 'SELECT count(*) FROM organizaciones')" -lt 1 ] || [ "$(consultar 'SELECT count(*) FROM usuarios')" -lt 1 ]; then
    resultado FALLO "la base restaurada no tiene ASADAs o usuarios"; exit 1
fi

resultado OK "migracion=$migracion$conteos"
exit 0

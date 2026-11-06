#!/bin/bash
# Funciones compartidas por instalar.sh y actualizar.sh (Sprint 6a): dejan en el servidor las imagenes de
# la aplicacion y del respaldo que corresponden al commit actual del repositorio.
#
#   source imagenes.sh && obtener_imagenes
#
# 1. Escribe en .env IMAGEN_ETIQUETA=<commit actual> (asi cualquier "docker compose ..." posterior usa
#    exactamente esa version).
# 2. Descarga esas imagenes de ghcr.io. GitHub las construye unos 10-15 minutos despues de cada cambio en
#    master: si todavia no estan, reintenta durante ~15 minutos.
# 3. Si no se pueden descargar (o si CONSTRUIR_EN_SERVIDOR=si en .env), las construye aqui.

valor_env() { grep -E "^$1=" .env 2>/dev/null | tail -1 | cut -d= -f2-; }

fijar_env() {
    if grep -qE "^$1=" .env; then
        sed -i "s|^$1=.*|$1=$2|" .env
    else
        printf '%s=%s\n' "$1" "$2" >> .env
    fi
}

obtener_imagenes() {
    local etiqueta intento intentos="${IMAGEN_REINTENTOS:-30}"
    etiqueta="$(git rev-parse HEAD)"
    fijar_env IMAGEN_ETIQUETA "$etiqueta"
    echo "Version del sistema: $(git log -1 --format='%h %s')"

    if [ "$(valor_env CONSTRUIR_EN_SERVIDOR)" = si ]; then
        echo "CONSTRUIR_EN_SERVIDOR=si: se construyen las imagenes aqui (la primera vez tarda varios minutos)."
        docker compose build --pull
        actualizar_imagenes_base
        return
    fi

    for ((intento = 1; intento <= intentos; intento++)); do
        if docker compose pull --quiet app respaldo 2> /tmp/asada-pull.log; then
            echo "Imagenes descargadas de GitHub (${etiqueta:0:7})."
            actualizar_imagenes_base
            return 0
        fi
        if grep -qiE "denied|unauthorized" /tmp/asada-pull.log; then
            echo "GitHub nego la descarga: los paquetes asadagest-app / asadagest-respaldo deben ser publicos"
            echo "(LEEME.md, 'Imagenes construidas por GitHub'). Se construyen aqui."
            break
        fi
        echo "La imagen de ${etiqueta:0:7} aun no esta en GitHub (se construye despues de cada cambio en master)."
        echo "  Reintento en 30 segundos ($intento/$intentos)..."
        sleep 30
    done

    echo "No se pudieron descargar las imagenes; se construyen en el servidor (la primera vez tarda varios minutos)."
    docker compose build --pull
    actualizar_imagenes_base
}

# PostgreSQL 16 y Caddy 2: trae las correcciones de seguridad publicadas dentro de la misma version
# (16.x / 2.x). Se aplican al recrear los contenedores (docker compose up -d en actualizar.sh).
actualizar_imagenes_base() {
    docker compose pull --quiet postgres caddy || echo "Aviso: no se pudieron actualizar postgres/caddy; se usan las que ya estan."
}

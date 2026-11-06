#!/bin/bash
# Primera instalacion del sistema en el servidor (Sprint 6a). Se ejecuta una vez, como el usuario de
# trabajo, desde la carpeta despliegue del repositorio:
#
#   cd /opt/asadagest/despliegue && bash instalar.sh
#
# 1. Si no existe .env, lo crea preguntando la direccion del sitio y el correo del Superadministrador
#    (la contrasena de la base se genera sola).
# 2. Construye las imagenes, levanta PostgreSQL, aplica las migraciones y crea el Superadministrador
#    (su contrasena temporal aparece en pantalla UNA sola vez).
# 3. Levanta todo: aplicacion, Caddy (HTTPS) y respaldos.
# Si ya hay un .env, lo respeta (se puede volver a ejecutar sin perder nada). Para actualizaciones
# posteriores se usa actualizar.sh.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -f .env ]; then
    echo "== Configuracion inicial (se guarda en despliegue/.env) =="
    ip="$(curl -fsS --max-time 5 https://api.ipify.org 2>/dev/null || hostname -I | awk '{print $1}')"
    sugerida="${ip//./-}.sslip.io"
    echo
    echo "Direccion del sistema. Sin dominio propio, se recomienda $sugerida"
    echo "(un nombre gratuito que apunta a la IP $ip y permite un certificado HTTPS valido)."
    read -r -p "Direccion [$sugerida]: " direccion
    direccion="${direccion:-$sugerida}"

    echo
    echo "Certificado HTTPS: escriba un correo para obtener uno gratuito de Let's Encrypt (recomendado"
    echo "con sslip.io o un dominio), o 'internal' para uno propio de Caddy (necesario si la direccion"
    echo "es una IP sola; el navegador mostrara una advertencia)."
    if [[ "$direccion" =~ ^[0-9.]+$ ]]; then tls_defecto=internal; else tls_defecto=""; fi
    patron_correo='^[^@[:space:]]+@[^@[:space:]]+\.[^@[:space:]]+$'
    read -r -p "Correo o 'internal' [${tls_defecto}]: " tls
    tls="${tls:-$tls_defecto}"
    while [ "$tls" != internal ] && [[ ! "$tls" =~ $patron_correo ]]; do
        read -r -p "Escriba un correo valido o 'internal': " tls
    done

    echo
    read -r -p "Correo del Superadministrador de la plataforma: " superadmin
    while [[ ! "$superadmin" =~ $patron_correo ]]; do
        read -r -p "Correo no valido. Correo del Superadministrador: " superadmin
    done

    umask 077
    sed -e "s|^SITIO_DIRECCION=.*|SITIO_DIRECCION=$direccion|" \
        -e "s|^SITIO_TLS=.*|SITIO_TLS=$tls|" \
        -e "s|^POSTGRES_CONTRASENA=.*|POSTGRES_CONTRASENA=$(openssl rand -hex 24)|" \
        -e "s|^SUPERADMIN_CORREO=.*|SUPERADMIN_CORREO=$superadmin|" \
        .env.ejemplo > .env
    umask 022
    echo
    echo "Se creo despliegue/.env (permisos 600). Para cambiar algo despues: nano .env"
fi

# Carpeta de rclone (configuracion de los destinos de respaldo; queda fuera de Git).
mkdir -p rclone
chmod 700 rclone

# La carpeta de respaldos debe existir antes de levantar el contenedor (si no, Docker la crea como root
# sin permisos para el grupo).
ruta_respaldos="$(grep -E '^RUTA_RESPALDOS=' .env | cut -d= -f2-)"
ruta_respaldos="${ruta_respaldos:-/srv/asada/respaldos}"
if [ ! -d "$ruta_respaldos" ]; then
    echo "Creando $ruta_respaldos (pide la contrasena de sudo)"
    sudo install -d -m 750 -o root -g "$(id -gn)" "$ruta_respaldos"
fi

echo
echo "== Construyendo las imagenes (la primera vez tarda varios minutos) =="
docker compose build

echo
echo "== Base de datos: migraciones y Superadministrador =="
docker compose up -d postgres
docker compose run --rm app migrar

echo
echo "== Levantando el sistema =="
docker compose up -d
sleep 5
docker compose ps

direccion="$(grep -E '^SITIO_DIRECCION=' .env | cut -d= -f2-)"
echo
echo "Listo. Abra https://$direccion e ingrese con el correo del Superadministrador y la contrasena"
echo "temporal que aparece arriba (si ya existia, use la suya). Siguientes pasos en LEEME.md, paso 5."

#!/bin/bash
# Prepara un VPS nuevo con Ubuntu 22.04 o 24.04 para el sistema (Sprint 6a). Se ejecuta UNA vez, como
# root, recien contratado el servidor:
#
#   bash preparar-servidor.sh asada
#
# ("asada" es el usuario de trabajo que se crea; puede ser otro nombre.) Hace lo siguiente:
#   1. Actualiza el sistema y activa las actualizaciones de seguridad automaticas.
#   2. Zona horaria de Costa Rica y 2 GB de memoria de intercambio (swap) si el servidor no tiene: compilar
#      la imagen de .NET en un VPS pequeno puede quedarse sin memoria.
#   3. Instala Docker (con docker compose) y git.
#   4. Firewall (ufw): solo SSH, HTTP (80) y HTTPS (443). PostgreSQL no queda expuesto.
#   5. Crea el usuario de trabajo (con sudo y permiso para usar Docker), le copia las llaves SSH de
#      root y crea /opt/asadagest (para el repositorio) y /srv/asada/respaldos.
# No cambia la configuracion de SSH (para no dejarlo a uno afuera); ver LEEME.md, paso 2.
set -euo pipefail

usuario="${1:-asada}"

if [ "$(id -u)" -ne 0 ]; then
    echo "Ejecute este script como root (o con sudo)."; exit 1
fi
if ! grep -qi ubuntu /etc/os-release; then
    echo "Aviso: este script esta pensado para Ubuntu; continue bajo su propio riesgo."
fi

paso() { echo; echo "==> $*"; }

paso "1/5 Actualizando el sistema"
export DEBIAN_FRONTEND=noninteractive
apt-get update -q
apt-get upgrade -y -q
apt-get install -y -q ca-certificates curl git ufw unattended-upgrades
dpkg-reconfigure -f noninteractive unattended-upgrades

paso "2/5 Zona horaria America/Costa_Rica y memoria de intercambio"
timedatectl set-timezone America/Costa_Rica || echo "Aviso: no se pudo cambiar la zona horaria."
if [ -z "$(swapon --show --noheadings)" ]; then
    fallocate -l 2G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo "Se creo /swapfile (2 GB)."
else
    echo "Ya hay memoria de intercambio: $(swapon --show --noheadings | awk '{print $1, $3}')"
fi

paso "3/5 Instalando Docker"
if command -v docker > /dev/null; then
    echo "Docker ya esta instalado: $(docker --version)"
else
    curl -fsSL https://get.docker.com | sh
fi
systemctl enable --now docker
docker compose version

paso "4/5 Firewall: SSH, 80 y 443"
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 443/udp
ufw --force enable
ufw status

paso "5/5 Usuario de trabajo '$usuario' y carpetas"
if ! id "$usuario" > /dev/null 2>&1; then
    adduser --disabled-password --gecos "" "$usuario"
    echo "Defina una contrasena para '$usuario' (la pide sudo):"
    passwd "$usuario"
fi
usermod -aG sudo,docker "$usuario"
if [ -f /root/.ssh/authorized_keys ]; then
    install -d -m 700 -o "$usuario" -g "$usuario" "/home/$usuario/.ssh"
    install -m 600 -o "$usuario" -g "$usuario" /root/.ssh/authorized_keys "/home/$usuario/.ssh/authorized_keys"
    echo "Se copiaron las llaves SSH de root a $usuario."
fi
install -d -m 755 -o "$usuario" -g "$usuario" /opt/asadagest
install -d -m 750 -o root -g "$usuario" /srv/asada
install -d -m 750 -o root -g "$usuario" /srv/asada/respaldos

echo
echo "Servidor preparado. Siguiente paso (LEEME.md, paso 3): salga y vuelva a entrar como '$usuario':"
echo "  ssh $usuario@$(hostname -I | awk '{print $1}')"

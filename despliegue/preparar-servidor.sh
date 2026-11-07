#!/bin/bash
# Prepara un VPS nuevo con Ubuntu 22.04 o 24.04 para el sistema (Sprint 6a). Se ejecuta UNA vez, como
# root (o con sudo), recien contratado el servidor:
#
#   bash preparar-servidor.sh asada          (como root)
#   sudo bash preparar-servidor.sh asada     (Oracle Cloud: se entra como "ubuntu")
#
# ("asada" es el usuario de trabajo que se crea; puede ser otro nombre.) Hace lo siguiente:
#   1. Actualiza el sistema y activa las actualizaciones de seguridad automaticas.
#   2. Zona horaria de Costa Rica y 2 GB de memoria de intercambio (swap) si el servidor no tiene: compilar
#      la imagen de .NET en un VPS pequeno puede quedarse sin memoria.
#   3. Instala Docker (con docker compose) y git.
#   4. Firewall: solo SSH, HTTP (80) y HTTPS (443). PostgreSQL no queda expuesto. Con ufw, salvo en
#      Oracle Cloud: sus imagenes de Ubuntu traen reglas de iptables propias (que bloquean todo menos
#      SSH) y Oracle recomienda no usar ufw; ahi se agregan 80/443 a esas reglas y se guardan.
#   5. Crea el usuario de trabajo (con sudo y permiso para usar Docker), le copia las llaves SSH del
#      usuario con que se entro (root, o "ubuntu" en Oracle) y crea /opt/asadagest (para el
#      repositorio) y /srv/asada/respaldos.
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
apt-get install -y -q ca-certificates curl git unattended-upgrades
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
if [ -f /etc/iptables/rules.v4 ] && grep -q "InstanceServices" /etc/iptables/rules.v4; then
    echo "Oracle Cloud detectado: se usan sus reglas de iptables (no ufw)."
    command -v netfilter-persistent > /dev/null || apt-get install -y -q iptables-persistent
    for regla in "-p tcp --dport 80" "-p tcp --dport 443" "-p udp --dport 443"; do
        # shellcheck disable=SC2086
        iptables -C INPUT $regla -m conntrack --ctstate NEW -j ACCEPT 2> /dev/null \
            || iptables -I INPUT 5 $regla -m conntrack --ctstate NEW -j ACCEPT
    done
    netfilter-persistent save
    iptables -L INPUT -n --line-numbers | head -12
    echo "Recuerde abrir tambien 80 y 443 en la Security List de la red (consola de Oracle; LEEME.md)."
else
    apt-get install -y -q ufw
    ufw allow OpenSSH
    ufw allow 80/tcp
    ufw allow 443/tcp
    ufw allow 443/udp
    ufw --force enable
    ufw status
fi

paso "5/5 Usuario de trabajo '$usuario' y carpetas"
if ! id "$usuario" > /dev/null 2>&1; then
    adduser --disabled-password --gecos "" "$usuario"
    echo "Defina una contrasena para '$usuario' (la pide sudo):"
    passwd "$usuario"
fi
usermod -aG sudo,docker "$usuario"
# Llaves SSH del usuario con que se entro (con sudo: SUDO_USER, p. ej. "ubuntu" en Oracle). Las de root
# en Oracle traen una restriccion ("Please login as ubuntu") que no debe copiarse.
origen_llaves="/root/.ssh/authorized_keys"
if [ -n "${SUDO_USER:-}" ] && [ "$SUDO_USER" != root ] && [ -f "/home/$SUDO_USER/.ssh/authorized_keys" ]; then
    origen_llaves="/home/$SUDO_USER/.ssh/authorized_keys"
fi
if [ -f "$origen_llaves" ] && [ ! -f "/home/$usuario/.ssh/authorized_keys" ]; then
    install -d -m 700 -o "$usuario" -g "$usuario" "/home/$usuario/.ssh"
    grep -v 'Please login as' "$origen_llaves" > "/home/$usuario/.ssh/authorized_keys" || true
    chown "$usuario:$usuario" "/home/$usuario/.ssh/authorized_keys"
    chmod 600 "/home/$usuario/.ssh/authorized_keys"
    echo "Se copiaron las llaves SSH de $origen_llaves a $usuario."
fi
install -d -m 755 -o "$usuario" -g "$usuario" /opt/asadagest
install -d -m 750 -o root -g "$usuario" /srv/asada
install -d -m 750 -o root -g "$usuario" /srv/asada/respaldos

echo
echo "Servidor preparado. Siguiente paso (LEEME.md, paso 3): salga y vuelva a entrar como '$usuario':"
echo "  ssh $usuario@$(hostname -I | awk '{print $1}')"

# Contenedor de respaldos del servidor (Sprint 6a): la misma imagen de PostgreSQL 16 que usa la base
# (pg_dump/pg_restore de la misma version) mas rclone, para copiar los respaldos a Google Drive,
# OneDrive, almacenamiento S3 u otros destinos. Los scripts estan en scripts/respaldos/.
FROM postgres:16

# rclone oficial (la version de Debian es antigua). Se elige el binario segun la arquitectura del
# servidor (amd64 o arm64).
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends ca-certificates curl unzip; \
    arquitectura="$(dpkg --print-architecture)"; \
    curl -fsSL "https://downloads.rclone.org/rclone-current-linux-${arquitectura}.zip" -o /tmp/rclone.zip; \
    unzip -j /tmp/rclone.zip '*/rclone' -d /usr/local/bin; \
    chmod 755 /usr/local/bin/rclone; \
    rm -f /tmp/rclone.zip; \
    apt-get purge -y unzip; \
    apt-get autoremove -y; \
    rm -rf /var/lib/apt/lists/*; \
    rclone version

# Despliegue en un servidor (Sprint 6a)

Guia para poner el sistema en un VPS Linux con Docker, sin dominio propio por ahora, con HTTPS y con
respaldos fuera del servidor (Google Drive, OneDrive, descarga manual a una PC y, cuando se quiera,
almacenamiento S3).

```
Internet --443--> Caddy (HTTPS) --> app (.NET, puerto 8080) --> postgres
                                                                   ^
                          respaldo (pg_dump diario + rclone) -------+--> Google Drive / OneDrive / S3
                                     |
                                     +--> /srv/asada/respaldos  <-- descargar-respaldos.ps1 (PC)
```

Archivos de esta carpeta:

| Archivo | Para que |
|---|---|
| `docker-compose.yml` | Los 4 contenedores del servidor (no confundir con el de la raiz, que es de desarrollo) |
| `Caddyfile` | HTTPS y proxy hacia la aplicacion |
| `.env.ejemplo` | Plantilla de `.env` (direccion, contrasena de la base, correo del Superadministrador, respaldos) |
| `respaldo/Dockerfile` | PostgreSQL 16 + rclone para el contenedor de respaldos |
| `imagenes.sh` | Descarga las imagenes que construye GitHub (lo usan instalar.sh y actualizar.sh) |
| `preparar-servidor.sh` | Una vez, como root, en un VPS nuevo |
| `instalar.sh` | Primera instalacion del sistema |
| `actualizar.sh` | Cada vez que haya una version nueva en `master` |

Todos los comandos `docker compose ...` de esta guia se ejecutan **dentro de esta carpeta**
(`cd /opt/asadagest/despliegue`).

---

## 1. Contratar el servidor

- Un VPS con **Ubuntu 24.04** (o 22.04), **2 GB de RAM** o mas, 1-2 vCPU y 25 GB de disco o mas
  (Hetzner, DigitalOcean, Vultr, Contabo, Linode... cualquiera sirve), o el gratuito de Oracle Cloud
  (seccion siguiente). Procesador Intel/AMD (amd64) o Arm (arm64): las imagenes existen para los dos.
- Al crearlo, si el proveedor lo permite, agregue su **llave SSH** (ver abajo). Anote la **IP publica**.

### 1.b Oracle Cloud Always Free (servidor de pruebas)

1. Cree la cuenta en <https://www.oracle.com/cloud/free/> (pide tarjeta solo para verificar). La
   **region de origen** (home region) no se puede cambiar despues y las maquinas gratuitas solo se crean
   ahi: elija la mas cercana a Costa Rica que ofrezca Ampere A1.
2. **Compute -> Instances -> Create instance**:
   - Image: **Canonical Ubuntu 24.04** (no la "Minimal").
   - Shape: **Ampere -> VM.Standard.A1.Flex**, 2 OCPU y 12 GB (el maximo gratuito); debe decir
     "Always Free-eligible".
   - Networking: la red que crea por defecto, con **direccion IPv4 publica**.
   - SSH keys: **Paste public keys** y pegue el contenido de su `id_ed25519.pub` (ver abajo).
   - Boot volume: 50-100 GB (el total gratuito es 200 GB).
   - Si aparece **"Out of host capacity"**: pruebe otro Availability Domain o vuelva a intentarlo mas tarde.
3. **Abrir 80 y 443 en la red**: en la instancia, Primary VNIC -> Subnet -> Security Lists -> Default
   Security List -> **Add Ingress Rules**: Source CIDR `0.0.0.0/0`, IP Protocol TCP, Destination Port
   `80,443`. (Sin esto, Oracle bloquea las visitas antes de que lleguen al servidor.)
4. En Oracle se entra como **`ubuntu`** (no root): `ssh ubuntu@IP`, y el paso 2 se ejecuta con `sudo`.
   `preparar-servidor.sh` detecta Oracle y abre 80/443 en sus reglas de iptables (alli no se usa ufw).
5. Oracle puede **detener una maquina gratuita que pasa 7 dias casi sin uso** (CPU, red y memoria bajo
   20 %). Si pasa, se vuelve a encender en la consola (Instances -> Start) sin perder nada; los datos y los
   respaldos siguen ahi. Para un servidor de produccion conviene pasar la cuenta a *Pay As You Go* (lo
   gratuito sigue sin costo) o usar un VPS pagado.

Llave SSH desde Windows (PowerShell), si todavia no tiene una:

```powershell
ssh-keygen -t ed25519                     # Enter en todo; queda en C:\Users\<usted>\.ssh\id_ed25519.pub
Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub   # este texto es el que se pega en el panel del proveedor
```

Si el proveedor solo le dio una contrasena de root, copie la llave asi:

```powershell
Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub | ssh root@IP "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

## 2. Preparar el servidor (una vez)

```powershell
ssh root@IP
```

```bash
sudo apt-get update && sudo apt-get install -y git
git clone https://github.com/LuisEnrique-98/AsadaGest.git /tmp/asadagest
sudo bash /tmp/asadagest/despliegue/preparar-servidor.sh asada
```

(En Oracle, entrar con `ssh ubuntu@IP`; en otros proveedores, `ssh root@IP` y `sudo` sobra.)

Actualiza Ubuntu, activa las actualizaciones de seguridad automaticas, pone la hora de Costa Rica, crea
memoria de intercambio, instala Docker, activa el firewall (solo SSH, 80 y 443) y crea el usuario
`asada` (le pide una contrasena para `sudo`). Al terminar, **salga y entre como `asada`**:

```powershell
ssh asada@IP
```

Recomendado cuando ya confirmo que entra como `asada` con su llave (sin que le pida contrasena de SSH):
desactivar el acceso por contrasena y el de root.

```bash
printf 'PasswordAuthentication no\nPermitRootLogin no\n' | sudo tee /etc/ssh/sshd_config.d/90-asada.conf
sudo systemctl restart ssh
```

(No cierre la sesion actual hasta comprobar en otra ventana que `ssh asada@IP` sigue funcionando.)

## 3. Instalar el sistema

```bash
git clone https://github.com/LuisEnrique-98/AsadaGest.git /opt/asadagest
cd /opt/asadagest/despliegue
bash instalar.sh
```

`instalar.sh` pregunta:

1. **Direccion**. Sin dominio, acepte la sugerida: `IP-CON-GUIONES.sslip.io` (por ejemplo
   `203-0-113-5.sslip.io`). [sslip.io](https://sslip.io) es un servicio gratuito que hace que ese nombre
   apunte a su IP, y con un nombre (a diferencia de una IP sola) se puede obtener un certificado HTTPS
   valido. Tambien puede escribir solo la IP.
2. **Certificado**: con sslip.io o un dominio, escriba un **correo** (Let's Encrypt avisa ahi si un
   certificado tiene problemas). Con una IP sola, `internal`: funciona, pero cada navegador muestra una
   advertencia la primera vez ("la conexion no es privada" -> Avanzado -> Continuar).
3. **Correo del Superadministrador**.

Luego descarga las imagenes que construyo GitHub (o las construye ahi si aun no existen: 5-15 minutos la
primera vez), crea la base, aplica las migraciones y muestra **una sola vez** la contrasena temporal del
Superadministrador:

```
==================================================================
  Superadministrador creado
  Correo:               luis@ejemplo.com
  Contrasena temporal:  kp7m-x3rw-9tqa
==================================================================
```

Anotela. Si se pierde: `docker compose run --rm app restablecer-superadministrador` genera otra.

## 4. Comprobar

```bash
docker compose ps                  # los 4 contenedores "running"; app y postgres "healthy"
docker compose logs -f app         # Ctrl+C para salir
docker compose logs caddy | grep -i certificate   # con Let's Encrypt: "certificate obtained successfully"
```

Abra `https://LA-DIRECCION` en el navegador.

## 5. Primer ingreso y registro de Cuatro Bocas

1. Ingrese con el correo del Superadministrador y la contrasena temporal; el sistema pide cambiarla.
2. **ASADAs -> Registrar ASADA**: codigo `CB`, nombre `ASADA Cuatro Bocas`, y los datos de su primer
   Administrador. Se muestra la contrasena temporal del Administrador: entreguesela en persona.
3. El Administrador entra, cambia su contrasena, crea los demas usuarios (**Usuarios**), las categorias y
   tarifas (**Tarifas**) y revisa **Configuracion** (cedula juridica, umbral de consumo anormal).

La base del servidor empieza vacia: no trae nada de la base de desarrollo (ni la ASADA de prueba `PR`
ni los usuarios `.test`). Los abonados, medidores y lecturas de Cuatro Bocas se cargaran con la
importacion desde Excel (Sprint 6b).

---

## Imagenes construidas por GitHub

Cada vez que se integra un cambio en `master`, GitHub Actions (`.github/workflows/imagenes.yml`) construye
las imagenes `asadagest-app` y `asadagest-respaldo` para amd64 y arm64 y las publica en el registro de
GitHub, etiquetadas con el commit. `instalar.sh` y `actualizar.sh` anotan en `.env` el commit actual
(`IMAGEN_ETIQUETA`) y descargan exactamente esas imagenes: el servidor no compila nada, actualizar tarda
segundos y no hace falta memoria para compilar.

- GitHub tarda unos 10-15 minutos en construirlas despues de cada cambio (pestana **Actions** del
  repositorio). Si se actualiza antes, el script espera hasta ~15 minutos y, si siguen sin estar, las
  construye en el servidor.
- **Una sola vez, despues de la primera publicacion**: los paquetes nuevos quedan privados. En GitHub,
  su perfil -> **Packages** -> `asadagest-app` -> **Package settings** -> **Change visibility** ->
  **Public** (y lo mismo con `asadagest-respaldo`). Las imagenes no tienen secretos: el codigo ya es
  publico y las contrasenas estan solo en el `.env` del servidor. Si quedan privadas, los scripts lo
  avisan y construyen en el servidor.
- Para construir siempre en el servidor (sin GitHub): `CONSTRUIR_EN_SERVIDOR=si` en `.env`.

Ademas, `.github/workflows/ci.yml` compila, corre las pruebas y revisa paquetes con vulnerabilidades
conocidas en cada PR (marca verde o roja en el PR), y Dependabot abre PRs con las actualizaciones de
paquetes e imagenes base.

---

## Respaldos fuera del servidor

El contenedor `respaldo` hace un respaldo cada dia a las 02:30 en `/srv/asada/respaldos` (30 diarios y
12 semanales, prueba de restauracion semanal; igual que en desarrollo). Un respaldo que solo esta en el
servidor se pierde con el servidor, por eso hay dos caminos, combinables:

- **Automatico con rclone**: despues de cada respaldo, se copia a uno o varios destinos.
- **Manual**: desde una PC con Windows, `descargar-respaldos.ps1` baja los que falten.

### A. Automatico: Google Drive, OneDrive y (luego) S3

Cada destino es un "remoto" de rclone, que se configura una vez. Se puede usar uno, dos o todos a la vez.

**1. En su PC con Windows**, descargue rclone de <https://rclone.org/downloads/> (Windows, Intel/AMD
64 bit), descomprimalo y deje a mano la carpeta (por ejemplo `C:\rclone`). Solo se usa para autorizar
el acceso desde el navegador, porque el servidor no tiene navegador.

**2. En el servidor**, cree el remoto:

```bash
cd /opt/asadagest/despliegue
docker compose run --rm --entrypoint rclone respaldo config
```

Google Drive (las preguntas pueden variar un poco segun la version de rclone):

| Pregunta | Respuesta |
|---|---|
| `n/s/q>` | `n` (nuevo remoto) |
| `name>` | `gdrive` |
| `Storage>` | `drive` (Google Drive) |
| `client_id>` / `client_secret>` | Enter (vacio) |
| `scope>` | `drive.file` (rclone solo ve los archivos que el mismo crea) |
| `service_account_file>` | Enter |
| `Edit advanced config?` | `n` |
| `Use web browser to automatically authenticate?` | `n` |

rclone muestra un comando como `rclone authorize "drive" "eyJ..."`. **Copielo y ejecutelo en la PC**
(PowerShell, en la carpeta de rclone): `.\rclone.exe authorize "drive" "eyJ..."`. Se abre el navegador;
entre con la cuenta de Google de la ASADA y acepte. En PowerShell aparece un texto largo entre
`--->` y `<---End paste`: copielo y peguelo en el servidor (`config_token>`). Despues:
`Configure this as a Shared Drive?` -> `n`, y `y` para guardar. `q` para salir.

OneDrive: igual, con `name> onedrive`, `Storage> onedrive`, autorizar con
`.\rclone.exe authorize "onedrive"` y despues elegir el tipo de cuenta (`onedrive` personal o
`business`) y la unidad que ofrece.

**3. Pruebe el remoto:**

```bash
docker compose run --rm --entrypoint rclone respaldo lsd gdrive:
```

**4. Activelo** en `.env` (`nano .env`), separando varios destinos con espacios:

```
RESPALDO_DESTINOS=gdrive:RespaldosASADA onedrive:RespaldosASADA
```

```bash
docker compose up -d respaldo                                          # toma el cambio
docker compose exec respaldo bash /opt/respaldos/subir-respaldos.sh     # primera subida, ahora
docker compose logs respaldo | tail                                     # "Respaldos subidos a ..."
```

Desde ahi es automatico: despues de cada respaldo diario se suben los nuevos a cada destino y se
comprueba que el ultimo llego completo. Si un destino falla (sin internet, token vencido), se anota en
el registro y se reintenta cada hora, sin afectar a los otros destinos. En cada destino quedan
`diarios/` y `semanales/`; alli se borran solo los mas antiguos que 30 dias (diarios) y 84 dias
(semanales), y nunca por haberse borrado en el servidor. Se cambia con `RESPALDO_REMOTO_DIAS_DIARIOS` y
`RESPALDO_REMOTO_DIAS_SEMANALES`.

**Cifrado (recomendado).** Los respaldos tienen datos personales de los abonados (Ley 8968). rclone
puede cifrarlos antes de subirlos, de modo que en Drive/OneDrive solo se vean archivos ilegibles. Cree
un remoto adicional de tipo `crypt` encima del anterior:

| Pregunta | Respuesta |
|---|---|
| `name>` | `gdrive-cifrado` |
| `Storage>` | `crypt` |
| `remote>` | `gdrive:RespaldosASADA-cifrado` |
| `filename_encryption>` | `standard` |
| `directory_name_encryption>` | `true` |
| `password` | `g` (generar), guardela |
| `password2` | `g` (generar), guardela |

y use `RESPALDO_DESTINOS=gdrive-cifrado:asada`. **Guarde las dos contrasenas del cifrado (o una copia
de `despliegue/rclone/rclone.conf`) en dos lugares seguros fuera del servidor**: sin ellas, los respaldos
cifrados no se pueden leer.

**S3 mas adelante** (Backblaze B2, Wasabi, DigitalOcean Spaces...): otro remoto con `Storage> s3` (o
`b2`) y las llaves de acceso del proveedor; luego se agrega a la lista, por ejemplo
`RESPALDO_DESTINOS=gdrive-cifrado:asada onedrive:RespaldosASADA b2:asada-respaldos`. No hay que cambiar
nada mas.

La configuracion de rclone queda en `despliegue/rclone/rclone.conf` (fuera de Git: tiene los accesos a
las cuentas).

### B. Manual: descargar a una PC con Windows

En la PC, con una copia del repositorio (la de Visual Studio sirve), en PowerShell:

```powershell
.\scripts\despliegue\descargar-respaldos.ps1 -Servidor asada@IP
```

Baja a `Documentos\RespaldosASADA` los respaldos que todavia no tenga y verifica cada uno con su suma
SHA-256. No borra nada. Opciones: `-Destino D:\Respaldos`, `-SoloSemanales`. Conviene hacerlo una vez
por semana (o programarlo con el Programador de tareas de Windows).

### Restaurar

Igual que en desarrollo (README principal, seccion Respaldos), desde esta carpeta:

```bash
docker compose exec respaldo bash /opt/respaldos/restaurar.sh            # lista los disponibles
docker compose exec respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump
#   -> base nueva asada_gestion_restaurada, sin tocar la que esta en uso
```

Reemplazar la base en uso:

```bash
docker compose stop app
docker compose exec -e CONFIRMAR=SI respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump asada_gestion
docker compose start app
```

Traer un respaldo desde Drive/OneDrive al servidor (si ya no esta en `/srv/asada/respaldos`):

```bash
docker compose exec respaldo rclone copy gdrive:RespaldosASADA/diarios/ARCHIVO.dump /respaldos/diarios/
docker compose exec respaldo rclone copy gdrive:RespaldosASADA/diarios/ARCHIVO.dump.sha256 /respaldos/diarios/
```

O desde la PC: `scp ARCHIVO.dump ARCHIVO.dump.sha256 asada@IP:/srv/asada/respaldos/diarios/`.

**Si se pierde el servidor completo**: contratar otro, pasos 1 a 3, traer el respaldo y reemplazar la
base como arriba. Los usuarios entran con sus contrasenas de siempre (estan en el respaldo). Hay que
volver a configurar rclone (o copiar el `rclone.conf` guardado).

---

## Actualizar a una version nueva

Cuando haya cambios integrados en `master`:

```bash
cd /opt/asadagest/despliegue && bash actualizar.sh
```

Descarga los cambios y sus imagenes, **respalda la base**, migra y reemplaza los contenedores (unos
segundos sin servicio; las sesiones se mantienen). Mejor fuera del horario de atencion, y despues de que
GitHub termine de construir las imagenes del cambio (pestana Actions).

**Si algo sale mal**: volver a la version anterior del codigo y restaurar el respaldo previo (el mas
reciente en `diarios/`, hecho por actualizar.sh antes de migrar):

```bash
git log --oneline -5                       # ver la version anterior
git checkout VERSION_ANTERIOR
source ./imagenes.sh && obtener_imagenes   # imagenes de esa version
docker compose stop app
docker compose exec -e CONFIRMAR=SI respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump asada_gestion
docker compose up -d
git checkout master                        # cuando el problema este corregido, y luego actualizar.sh
```

## Pasar a un dominio propio

1. Comprar el dominio y crear un registro **A** que apunte a la IP del servidor (por ejemplo
   `sistema.asadacuatrobocas.com`).
2. En `.env`: `SITIO_DIRECCION=sistema.asadacuatrobocas.com` y `SITIO_TLS=correo@...`.
3. `docker compose up -d caddy`. Caddy obtiene el certificado solo.

## Comandos frecuentes

```bash
docker compose ps                                                  # estado
docker compose logs -f app                                         # registro de la aplicacion
docker compose logs respaldo | tail -20                            # respaldos y subidas
docker compose exec respaldo bash /opt/respaldos/respaldar.sh      # respaldo ahora
docker compose exec respaldo bash /opt/respaldos/subir-respaldos.sh   # subir ahora a todos los destinos
docker compose run --rm app restablecer-superadministrador         # contrasena temporal nueva
docker compose restart app                                         # reiniciar la aplicacion
df -h /                                                            # espacio en disco
```

## Problemas comunes

| Sintoma | Causa probable |
|---|---|
| El navegador no abre la pagina | `docker compose ps` (caddy y app deben estar "running"); firewall del proveedor (algunos tienen uno propio en el panel: abrir 80 y 443) |
| "Too many certificates" en `docker compose logs caddy` | Limite de Let's Encrypt para sslip.io; usar `SITIO_TLS=internal` por un tiempo o un dominio propio |
| `app` queda "unhealthy" | `docker compose logs app`; casi siempre la base: `docker compose logs postgres` |
| Todos salen de su sesion tras actualizar | Normal solo si se borro el volumen `claves_app`; si pasa siempre, revisar que exista (`docker volume ls`) |
| La subida a Drive/OneDrive falla con "token expired" / "invalid_grant" | Volver a autorizar: `docker compose run --rm --entrypoint rclone respaldo config reconnect gdrive:` |
| Construir en el servidor se detiene por memoria | Revisar que haya swap (`free -h`; `preparar-servidor.sh` crea 2 GB) o usar las imagenes de GitHub |
| "GitHub nego la descarga" al instalar/actualizar | Los paquetes estan privados: hacerlos publicos (seccion "Imagenes construidas por GitHub") |
| En Oracle no abre la pagina | Falta la regla de entrada 80/443 en la Security List (paso 1.b.3) |

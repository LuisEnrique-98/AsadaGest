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
  (Hetzner, DigitalOcean, Vultr, Contabo, Linode... cualquiera sirve), el gratuito de Oracle Cloud
  (seccion 1.b) o la capa gratuita de Microsoft Azure (seccion 1.c, donde esta el servidor de pruebas).
  Procesador Intel/AMD (amd64) o Arm (arm64): las imagenes existen para los dos.
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

### 1.c Microsoft Azure (servidor de pruebas actual)

Azure acepta tarjeta de debito (Oracle puede rechazarla). La cuenta nueva trae 12 meses de capa
gratuita: 750 horas al mes de una maquina **B2pts_v2** (Arm, 2 vCPU, 1 GB de RAM; con los 2 GB de swap
que crea el paso 2 alcanza) y un disco Premium SSD de 64 GB.

**Costo real: la IP publica no es gratuita.** La IP publica Standard estatica cuesta unos **USD 0.005
por hora (~USD 3.65 al mes)** aunque la maquina y el disco esten en USD 0. No hay alternativa gratuita:
sin IP publica el servidor no es accesible. Revise **Cost Management -> Analisis de costos** el primer
mes.

1. Cree la cuenta en <https://azure.microsoft.com/free/>. Antes de que termine la prueba de 30 dias,
   pasela a **pago por uso** (lo de la capa gratuita sigue gratis durante los 12 meses). Anote cuando
   vencen esos 12 meses: desde ahi la maquina y el disco se cobran.
2. Cree una **alerta de presupuesto**: Cost Management -> Supervision -> Presupuestos -> Agregar,
   mensual, de unos **USD 5** con avisos al 50 % y 100 % por correo. (Con USD 1 avisa todos los meses
   solo por la IP publica.)
3. Abra **Cloud Shell** (icono `>_` arriba en el portal, tipo Bash). El prompt se ve asi:
   `usuario [ ~ ]$`. En una suscripcion nueva hay que registrar los proveedores (si no, el portal no
   ofrece la serie B y las cuotas salen vacias):

   ```bash
   az provider register -n Microsoft.Compute --wait
   az provider register -n Microsoft.Network --wait
   az vm list-skus --location southcentralus --size Standard_B2pts --output table   # sin "Restrictions"
   ```

4. Cree la maquina (en Cloud Shell). En `--ssh-key-values` pegue **entre comillas** el texto de su
   `id_ed25519.pub` (ver abajo; Cloud Shell no tiene su llave privada, solo se le da la publica). El
   nombre de `--public-ip-address-dns-name` sera la direccion del sistema:
   `NOMBRE.southcentralus.cloudapp.azure.com`.

   ```bash
   az group create --name asada-pruebas --location southcentralus
   az vm create --resource-group asada-pruebas --name asada-pruebas \
     --image Canonical:ubuntu-24_04-lts:server-arm64:latest --size Standard_B2pts_v2 \
     --admin-username azureuser --ssh-key-values "ssh-ed25519 AAAA...TEXTO-DE-SU-LLAVE... usuario@pc" \
     --os-disk-size-gb 64 --storage-sku Premium_LRS \
     --public-ip-sku Standard --public-ip-address-dns-name asada-cuatrobocas
   az vm open-port --resource-group asada-pruebas --name asada-pruebas --port 80,443
   ```

   Si `B2pts_v2` no esta disponible en `southcentralus`, pruebe `centralus`, `westus2` o
   `northcentralus` (y cambie `southcentralus` en los comandos y en la direccion).
5. **Desde PowerShell de su PC** (no desde Cloud Shell, que no tiene la llave privada: ahi `ssh` responde
   "Connection closed"), entre como `azureuser` y siga el paso 2 con `sudo`:

   ```powershell
   ssh azureuser@asada-cuatrobocas.southcentralus.cloudapp.azure.com
   ```

   `preparar-servidor.sh` usa ufw (Azure no trae reglas propias como Oracle) y copia las llaves de
   `azureuser` al usuario `asada`. En el paso 3, cuando `instalar.sh` pregunte la **direccion**, escriba
   el nombre `NOMBRE.southcentralus.cloudapp.azure.com` en lugar del de sslip.io, y un correo para el
   certificado.

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

(En Oracle, entrar con `ssh ubuntu@IP`; en Azure, `ssh azureuser@NOMBRE...cloudapp.azure.com`; en otros
proveedores, `ssh root@IP` y `sudo` sobra.) Escriba `asada` con cuidado: con teclado en espanol, la
tecla `~` es "muerta" y puede colarse (`asada~`); si pasa, repita el comando con el nombre correcto.

Actualiza Ubuntu, activa las actualizaciones de seguridad automaticas, pone la hora de Costa Rica, crea
memoria de intercambio, instala Docker, activa el firewall (solo SSH, 80 y 443) y crea el usuario
`asada` (le pide una contrasena para `sudo`; **mientras la escribe no se ve nada**, es normal). Use una
contrasena solo de letras y numeros: los acentos y simbolos con teclas muertas hacen que las dos veces no
coincidan. Si aun asi no coinciden, el usuario queda creado sin contrasena; pongasela con esta linea (el
espacio al inicio evita que quede en el historial):

```bash
 echo 'asada:LetrasYNumeros123' | sudo chpasswd
```

Si al terminar aparece "System restart required", reinicie con `sudo reboot` y vuelva a entrar al
minuto. (Si Ubuntu ofrece pasar a una version nueva con `do-release-upgrade`, no lo haga.)

Al terminar, **salga y entre como `asada`**:

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
Lo recomendado (y lo que usa el servidor de pruebas) es **Google Drive cifrado**: un remoto
`gdrive-personal` (la cuenta de Google) y encima uno `respaldos-cifrados` (de tipo `crypt`), que es el
que se pone en `RESPALDO_DESTINOS`. Use nombres genericos como esos: no dicen que hay adentro.

Todos los comandos `docker compose ...` se ejecutan **en el servidor, en `/opt/asadagest/despliegue`**
(si se ejecutan en otra carpeta, por ejemplo `~`, responden "no configuration file provided").

#### A.0 Una sola vez: client_id propio de Google (solo para Google Drive)

rclone traia un acceso compartido a Google Drive (client_id) que **deja de funcionar durante 2026**;
rclone v1.76 ya lo advierte (`Continue using the shared client_id anyway?`). Hay que crear uno propio,
con la misma cuenta de Google donde quedaran los respaldos (idealmente la de la ASADA). Se hace en el
navegador, una vez:

1. En <https://console.cloud.google.com> cree un proyecto nuevo (por ejemplo `asada-respaldos`).
2. **APIs y servicios -> Biblioteca -> Google Drive API -> Habilitar.**
3. **Google Auth Platform** (pantalla de consentimiento OAuth): tipo **Externo**, nombre de la app (por
   ejemplo `AppAsada`) y su correo. En **Acceso a datos** agregue solo el scope
   `https://www.googleapis.com/auth/drive.file`. En **Publico** agregue su correo como usuario de prueba.
4. **Clientes -> Crear cliente**, tipo **App de escritorio**. Guarde el **ID de cliente** y el
   **secreto** fuera del servidor (se usan en A.2).
5. **Branding**: para publicar la app, Google exige una pagina principal y una politica de privacidad.
   Si no hay sitio web, sirve **GitHub Pages**: un repositorio publico llamado `USUARIO.github.io` con un
   `index.html` (una linea sobre para que es la app) y un `privacidad.html` (la app solo guarda en Drive
   los respaldos cifrados de la ASADA, no lee otros archivos y no comparte datos). En Branding: pagina
   principal `https://usuario.github.io/`, politica de privacidad
   `https://usuario.github.io/privacidad.html` y dominio autorizado `usuario.github.io` (en minusculas).
   No borre ese repositorio mientras se use la app.
6. **Publico -> Publicar app**, hasta que diga **"En produccion"**. Si se queda "En prueba", Google
   vence la autorizacion **a los 7 dias** y las subidas empiezan a fallar con `invalid_grant`. Con el
   scope `drive.file` no hace falta que Google verifique la app; al autorizar aparece "Google no
   verifico esta app": **Configuracion avanzada -> Ir a AppAsada**.

#### A.1 rclone en su PC con Windows

Solo se usa para autorizar el acceso desde el navegador, porque el servidor no tiene navegador. Descargue
rclone de <https://rclone.org/downloads/> (Windows, Intel/AMD 64 bit) y descomprimalo en `C:\rclone`. El
ZIP trae una subcarpeta (por ejemplo `C:\rclone\rclone-v1.76.0-windows-amd64`): el `rclone.exe` esta
ahi. Use **la misma version** que el servidor (`docker compose run --rm --entrypoint rclone respaldo
version`).

En **PowerShell de la PC** (prompt `PS C:\...>`), cree un alias para poder pegar despues el comando del
servidor tal cual (PowerShell no ejecuta `rclone` sin `.\` ni ruta completa):

```powershell
Set-Alias rclone "C:\rclone\rclone-v1.76.0-windows-amd64\rclone.exe"
rclone version
```

El alias dura mientras esa ventana de PowerShell este abierta: dejela abierta para A.2.

#### A.2 Remoto de Google Drive (`gdrive-personal`)

**En el servidor** (sesion SSH, prompt `asada@...:~$`):

```bash
cd /opt/asadagest/despliegue
docker compose run --rm --entrypoint rclone respaldo config
```

(las preguntas pueden variar un poco segun la version de rclone):

| Pregunta | Respuesta |
|---|---|
| `n/s/q>` | `n` (nuevo remoto) |
| `name>` | `gdrive-personal` (o `gdrive-asada` con la cuenta de la ASADA) |
| `Storage>` | `drive` (Google Drive) |
| `client_id>` | el ID de cliente de A.0 (si lo deja vacio, rclone pregunta `Continue using the shared client_id anyway?`: `n` y escribalo) |
| `client_secret>` | el secreto de A.0 |
| `scope>` | `drive.file` (rclone solo ve los archivos que el mismo crea) |
| `service_account_file>` | Enter |
| `Edit advanced config?` | `n` |
| `Use web browser to automatically authenticate?` | `n` |

rclone muestra un comando que empieza con `rclone authorize "drive" "eyJ..."`. **Copielo completo y
peguelo en la ventana de PowerShell de A.1.** Se abre el navegador; entre con la cuenta de Google y
acepte. El navegador solo dice "Exito! All done": **el texto que hay que copiar sale en PowerShell**,
entre `--->` y `<---End paste`. Peguelo en el servidor (`config_token>`). Despues:
`Configure this as a Shared Drive?` -> `n`, y `y` para guardar. No salga todavia (`q`): siga con A.3.

- Si se interrumpe a medias, el remoto queda guardado incompleto: en el menu, `d` para borrarlo y `n`
  para crearlo de nuevo.
- Para cambiarle el client_id a un remoto que ya existe: `e` (editar), escriba el ID y el secreto
  nuevos, y a `Token already configured - replace it?` responda `y` para autorizar de nuevo.

OneDrive: igual, con `name> onedrive`, `Storage> onedrive`, autorizar con `rclone authorize "onedrive"`
y despues elegir el tipo de cuenta (`onedrive` personal o `business`) y la unidad que ofrece.

#### A.3 Remoto cifrado (`respaldos-cifrados`)

Los respaldos tienen datos personales de los abonados (Ley 8968). Con un remoto `crypt` encima del
anterior, rclone los cifra antes de subirlos: en Drive solo se ven nombres y archivos ilegibles. En el
mismo `rclone config`:

| Pregunta | Respuesta |
|---|---|
| `n/s/q>` | `n` |
| `name>` | `respaldos-cifrados` |
| `Storage>` | `crypt` |
| `remote>` | `gdrive-personal:RespaldosASADA` |
| `filename_encryption>` | `standard` |
| `directory_name_encryption>` | `true` |
| `password` | `g` (generar), `128` bits, `y`; **copie la contrasena que muestra** |
| `password2` | `g` (generar), `128` bits, `y`; **copie la contrasena que muestra** |
| `Edit advanced config?` | `n`, y `y` para guardar; `q` para salir |

**Guarde las dos contrasenas del cifrado y una copia de `rclone.conf` en dos lugares seguros fuera del
servidor**: sin ellas, los respaldos cifrados no se pueden leer. `rclone.conf` es de root; para bajarlo:

```bash
# En el servidor (asada@...:~$): copia temporal en su carpeta
sudo cp /opt/asadagest/despliegue/rclone/rclone.conf ~/rclone.conf && sudo chown asada: ~/rclone.conf
```

```powershell
# En PowerShell de la PC (PS C:\...>)
scp asada@SERVIDOR:rclone.conf C:\RespaldosASADA\rclone.conf
```

```bash
# De vuelta en el servidor: borrar la copia temporal
rm ~/rclone.conf
```

(`rclone.conf` tiene los accesos a la cuenta y las contrasenas del cifrado: no lo deje en una carpeta
sincronizada con OneDrive ni lo envie por correo.)

#### A.4 Probar y activar

```bash
docker compose run --rm --entrypoint rclone respaldo lsd gdrive-personal:
```

Active el destino en `.env` (`nano .env`), separando varios con espacios:

```
RESPALDO_DESTINOS=respaldos-cifrados:asada
```

```bash
docker compose up -d respaldo                                          # toma el cambio
docker compose exec respaldo bash /opt/respaldos/subir-respaldos.sh     # primera subida, ahora
docker compose logs respaldo | tail                                     # "Respaldos subidos a ..."
docker compose exec respaldo rclone cryptcheck /respaldos/diarios respaldos-cifrados:asada/diarios
#   -> "0 differences found"
```

Desde ahi es automatico: despues de cada respaldo diario se suben los nuevos a cada destino y se
comprueba que el ultimo llego completo. Si un destino falla (sin internet, token vencido), se anota en
el registro y se reintenta cada hora, sin afectar a los otros destinos. En cada destino quedan
`diarios/` y `semanales/`; alli se borran solo los mas antiguos que 30 dias (diarios) y 84 dias
(semanales), y nunca por haberse borrado en el servidor. Se cambia con `RESPALDO_REMOTO_DIAS_DIARIOS` y
`RESPALDO_REMOTO_DIAS_SEMANALES`.

**S3 mas adelante** (Backblaze B2, Wasabi, DigitalOcean Spaces...): otro remoto con `Storage> s3` (o
`b2`) y las llaves de acceso del proveedor (conviene tambien un `crypt` encima); luego se agrega a la
lista, por ejemplo `RESPALDO_DESTINOS=respaldos-cifrados:asada b2-cifrado:asada`. No hay que cambiar
nada mas.

La configuracion de rclone queda en `despliegue/rclone/rclone.conf` (fuera de Git: tiene los accesos a
las cuentas).

#### A.5 Pasar los respaldos a otra cuenta de Google (por ejemplo, de una personal a la de la ASADA)

Sin cambiar `RESPALDO_DESTINOS` ni las contrasenas del cifrado:

1. Client_id propio en la cuenta nueva (A.0) y remoto `gdrive-asada` con el (A.2).
2. Copiar los archivos cifrados tal cual:
   `docker compose run --rm --entrypoint rclone respaldo copy gdrive-personal:RespaldosASADA gdrive-asada:RespaldosASADA`
3. En `rclone config`, editar (`e`) `respaldos-cifrados` y cambiar solo `remote` a
   `gdrive-asada:RespaldosASADA` (mismas contrasenas).
4. `docker compose up -d respaldo`, una subida de prueba y el `cryptcheck` de A.4.
5. Borrar la carpeta en el Drive anterior, el remoto `gdrive-personal`, el acceso en
   <https://myaccount.google.com/permissions> y desactivar el proyecto de Google Cloud anterior.

### B. Manual: descargar a una PC con Windows

En la PC, con una copia del repositorio (la de Visual Studio sirve), en PowerShell:

```powershell
.\scripts\despliegue\descargar-respaldos.ps1 -Servidor asada@IP -Destino C:\RespaldosASADA
```

Baja a la carpeta `-Destino` los respaldos que todavia no tenga y verifica cada uno con su suma
SHA-256. No borra nada. **Use un `-Destino` fuera de OneDrive** (como `C:\RespaldosASADA`): sin
`-Destino` van a `Documentos\RespaldosASADA`, y en muchas PC Documentos se sincroniza con OneDrive, lo
que subiria a la nube los respaldos **sin cifrar** (datos personales, Ley 8968). Otra opcion:
`-SoloSemanales`. Conviene hacerlo una vez por semana (o programarlo con el Programador de tareas de
Windows). Ejemplo con el servidor de pruebas:

```powershell
.\scripts\despliegue\descargar-respaldos.ps1 -Servidor asada@asada-cuatrobocas.southcentralus.cloudapp.azure.com -Destino C:\RespaldosASADA
```

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

Traer un respaldo desde Drive/OneDrive al servidor (si ya no esta en `/srv/asada/respaldos`). A
traves del remoto cifrado, rclone muestra y descifra los nombres reales:

```bash
docker compose exec respaldo rclone ls respaldos-cifrados:asada/diarios
docker compose exec respaldo rclone copy respaldos-cifrados:asada/diarios/ARCHIVO.dump /respaldos/diarios/
docker compose exec respaldo rclone copy respaldos-cifrados:asada/diarios/ARCHIVO.dump.sha256 /respaldos/diarios/
```

O desde la PC: `scp ARCHIVO.dump ARCHIVO.dump.sha256 asada@IP:/srv/asada/respaldos/diarios/`.

**Si se pierde el servidor completo**: contratar otro, pasos 1 a 3, traer el respaldo y reemplazar la
base como arriba. Los usuarios entran con sus contrasenas de siempre (estan en el respaldo). Hay que
volver a configurar rclone (o copiar el `rclone.conf` guardado a `despliegue/rclone/`; sin las
contrasenas del cifrado los respaldos de Drive no sirven).

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
| La subida a Drive/OneDrive falla con "token expired" / "invalid_grant" | Volver a autorizar: `docker compose run --rm --entrypoint rclone respaldo config reconnect gdrive-personal:`. Si pasa cada 7 dias, la app de Google Cloud sigue "En prueba": publicarla (A.0, paso 6) |
| `no configuration file provided` | El comando `docker compose` se ejecuto fuera de `/opt/asadagest/despliegue` |
| `rclone : El termino 'rclone' no se reconoce...` en PowerShell | Falta el alias de A.1 (`Set-Alias rclone ...`) en esa ventana, o la ruta no incluye la subcarpeta `rclone-vX-windows-amd64` |
| `ssh` desde Cloud Shell de Azure responde "Connection closed" | Cloud Shell no tiene su llave privada: conectarse desde PowerShell de la PC (1.c, paso 5) |
| Azure no ofrece la serie B o `az vm list-usage` sale vacio | Registrar los proveedores `Microsoft.Compute` y `Microsoft.Network` (1.c, paso 3) |
| La alerta de presupuesto de Azure avisa cada mes | La IP publica cuesta ~USD 3.65/mes aunque la maquina sea gratuita (1.c); subir la alerta a ~USD 5 |
| Construir en el servidor se detiene por memoria | Revisar que haya swap (`free -h`; `preparar-servidor.sh` crea 2 GB) o usar las imagenes de GitHub |
| "GitHub nego la descarga" al instalar/actualizar | Los paquetes estan privados: hacerlos publicos (seccion "Imagenes construidas por GitHub") |
| En Oracle no abre la pagina | Falta la regla de entrada 80/443 en la Security List (paso 1.b.3) |

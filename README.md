# Sistema de Gestion ASADA -- Sprint 0 (Cimientos)

Este repositorio contiene el arranque del proyecto: la solucion en capas, la base de datos
con PostgreSQL, autenticacion con ASP.NET Core Identity, el aislamiento multiorganizacion
(`IProveedorOrganizacion` + `HasQueryFilter`), datos iniciales (organizacion Cuatro Bocas +
un usuario Administrador), y una pagina de login en Blazor Server.

Corresponde al Sprint 0 de la hoja de ruta descrita en `Planificacion_Tecnica_ASADA_NET.docx`.

## Que se compilo y verifico, y que no

Este proyecto se genero en un entorno sin acceso a NuGet.org (solo se pudo instalar el SDK
de .NET 8 via el repositorio de Ubuntu). Eso significa:

- **`Asada.Dominio`** y **`Asada.Aplicacion`** se compilaron y verificaron de verdad
  (`dotnet build`), porque no dependen de ningun paquete externo. Compilan sin errores.
- **`Asada.Infraestructura`**, **`Asada.Presentacion`** y **`Asada.Dominio.Tests`** NO se
  pudieron restaurar ni compilar aqui, porque dependen de paquetes de NuGet (Npgsql, EF Core,
  ASP.NET Core Identity, xUnit) que no se pudieron descargar. El codigo se escribio con
  cuidado y se reviso a mano, pero **la primera vez que lo abras en tu maquina, el primer
  paso es justamente `dotnet restore` + `dotnet build` para confirmar que compila** -- ahi es
  donde tu entras como el "filtro" que mencionabas: cualquier error de compilacion que salga,
  lo revisamos juntos en la conversacion.

## Requisitos previos

- .NET 8 SDK
- Docker Desktop (o Docker Engine + Compose)
- La herramienta `dotnet-ef` (para migraciones): `dotnet tool install --global dotnet-ef`

## Pasos para arrancar

```bash
# 1. Restaurar y compilar -- este es el primer chequeo real de todo el codigo
dotnet restore
dotnet build

# 2. Levantar PostgreSQL local
docker compose up -d

# 3. Generar la migracion inicial (todavia no existe ninguna migracion en el repo)
dotnet ef migrations add InicialOrganizacionesUsuarios \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion

# 4. Aplicar la migracion (crea las tablas en PostgreSQL)
dotnet ef database update \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion

# 5. Correr la aplicacion
dotnet run --project src/Asada.Presentacion
```

Al arrancar en modo Desarrollo, `Program.cs` aplica automaticamente las migraciones
pendientes y siembra los datos iniciales (roles, la organizacion Cuatro Bocas, y el usuario
Administrador), asi que el paso 4 es mas que todo para inspeccionar el resultado si quieres
-- el paso 5 ya deja todo listo.

## Iniciar sesion

Abre la URL que muestre la consola (normalmente `https://localhost:7xxx`) y entra a
`/cuenta/iniciar-sesion` con:

- **Correo:** `admin@cuatrobocas.test`
- **Contrasena:** `CuatroBocas#2026`

Deberias ver "Bienvenido, Administrador Cuatro Bocas" y el nombre de la organizacion
(ASADA Cuatro Bocas / CB).

Desde el Sprint 5b tambien existe un **Superadministrador** de la plataforma (sin ASADA), que
registra ASADAs nuevas y atiende sus usuarios:

- **Correo:** `superadmin@plataforma.test`
- **Contrasena:** `Plataforma#2026`

**Importante:** estas contrasenas estan escritas en texto plano en
`SeedInicial.cs` unicamente porque son datos de arranque para desarrollo local. Antes de
usar datos reales o de desplegar en un servidor, hay que cambiarlas (Cambiar contrasena, arriba
a la derecha) o desactivar esos usuarios y crear otros por la interfaz.

## Decisiones tomadas durante la implementacion (vale la pena que las conozcas)

Al escribir el codigo aparecieron un par de ajustes frente a lo que decia el documento
tecnico en prosa. Ninguno cambia la arquitectura de fondo, pero es mejor que quede explicito:

1. **`Usuario` y `Rol` viven en `Asada.Infraestructura`, no en `Asada.Dominio`.**
   El documento original los ubicaba en Dominio, pero al ser clases que extienden
   `IdentityUser<int>`/`IdentityRole<int>` (de ASP.NET Core Identity), ubicarlas en Dominio
   rompia la regla de "Dominio sin ninguna dependencia externa". `Organizacion` si se quedo
   en Dominio, porque es un concepto de negocio puro. Si mas adelante un caso de uso de
   `Asada.Aplicacion` necesita trabajar con usuarios, se le define una interfaz propia ahi
   (como ya existe con `IProveedorOrganizacion`), sin que Aplicacion dependa de Infraestructura.

2. **Se usa el modelo estandar de ASP.NET Core Identity (roles many-to-many)** en vez de la
   columna unica `usuarios.rol_id` planteada en el esquema del SRS. Esto apoya la
   implementacion ya probada de Identity en vez de escribir un almacen de usuarios propio,
   que hubiera sido mas riesgoso de acertar sin poder compilarlo aqui. En la practica, cada
   usuario tiene un solo rol asignado desde la aplicacion, asi que el comportamiento para el
   usuario final es el mismo.

3. **La tabla `usuarios` NO tiene el filtro global multiorganizacion (`HasQueryFilter`)**,
   aunque tiene `organizacion_id`. La razon es un problema de "huevo y gallina": el propio
   inicio de sesion consulta esa tabla, y en ese momento todavia no hay ningun usuario
   autenticado del cual derivar una organizacion -- si el filtro estuviera activo, nadie
   podria iniciar sesion nunca (excepto el Superadministrador). `Organizacion` si tiene el
   filtro completo. El aislamiento para listados administrativos de usuarios (por ejemplo,
   "ver todos los usuarios de mi ASADA") se resolvera con un caso de uso propio en el Sprint 5
   (matriz de permisos), que si puede filtrar explicitamente.

4. **Los nombres de columnas propias de Identity** (`email`, `password_hash`,
   `normalized_user_name`, etc.) quedan en ingles, aunque el resto del esquema esta en
   espanol. Son generadas automaticamente por ASP.NET Core Identity; renombrarlas obligaria a
   mantener un mapeo manual en cada actualizacion futura del paquete, con poco beneficio real.

## Estructura

```
AsadaGestion.sln
docker-compose.yml
src/
  Asada.Dominio/            Organizacion + EstadoOrganizacion. Sin dependencias externas.
  Asada.Aplicacion/         IProveedorOrganizacion (interfaz).
  Asada.Infraestructura/    AsadaDbContext, Identity, ProveedorOrganizacion, seed.
  Asada.Presentacion/       Blazor Server: Program.cs, Login.razor, Home.razor.
tests/
  Asada.Dominio.Tests/      Pruebas unitarias de Organizacion (xUnit).
```

## Sprint 1 -- Abonados

Agrega el agregado **Abonado -> Propiedad -> Servicio**, con aislamiento por organizacion.

- **Dominio:** `Abonado`, `Propiedad`, `Servicio`, `TipoIdentificacion` (cedula fisica, cedula juridica,
  DIMEX, pasaporte), `EstadoRegistro`, `ReglasIdentificacion` (normaliza y valida el numero segun el tipo).
  La morosidad vive en `Servicio` (correccion C-01), no en `Abonado`.
- **Aplicacion:** `Resultado<T>`, `IRepositorioAbonados` y los casos de uso `CrearAbonado`,
  `BuscarAbonados`, `ObtenerExpedienteAbonado`, `AgregarPropiedad`, `AgregarServicio`.
- **Infraestructura:** configuraciones EF, `HasQueryFilter` para las 3 entidades nuevas, y
  `RepositorioAbonados`. El correlativo (`CB-AB-000125`) sale de un contador por organizacion
  (`organizaciones.ultimo_correlativo_abonado`) incrementado con `UPDATE ... RETURNING` dentro de
  la misma transaccion que guarda el abonado.
- **Presentacion:** `/abonados` (busqueda + paginacion), `/abonados/nuevo`, `/abonados/{id}`
  (expediente con alta de propiedades y servicios) y `/cuenta/acceso-denegado`.

Pasos para probarlo:

```bash
dotnet build
dotnet test

dotnet ef migrations add Sprint1Abonados \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion

dotnet run --project src/Asada.Presentacion   # en Desarrollo aplica la migracion sola
```

Decisiones a tener presentes:

1. Las paginas son de **renderizado estatico (SSR)**, igual que el login: `ProveedorOrganizacion`
   lee `HttpContext`, que no esta disponible dentro de un circuito interactivo de Blazor Server.
   Si en el futuro se agrega `@rendermode InteractiveServer` a alguna pagina, hay que cambiar el
   proveedor para que lea `AuthenticationStateProvider`.
2. Solo `Administrador` y `Operador` pueden crear/agregar; `Consulta` solo ve. La matriz formal es Sprint 5.
3. Identificacion unica por organizacion (`organizacion_id` + tipo + numero), guardada sin guiones.
4. La validacion es manual (sin FluentValidation) para que `Asada.Aplicacion` siga sin dependencias.

## Sprint 1b -- Edicion de abonados

`/abonados/{id}/editar` (Administrador y Operador) corrige los datos del abonado; el codigo no cambia.
El boton Desactivar/Reactivar del expediente (solo Administrador) cambia el estado sin borrar nada.
La identificacion sigue siendo unica por ASADA y el propio abonado no cuenta como duplicado de si mismo.

## Sprint 2 -- Mediciones

- **Medidores** (`/servicios/{id}/medidores`): instalar, retirar y ver el historial. Un servicio tiene a lo sumo
  un medidor activo (tambien garantizado por un indice unico filtrado en PostgreSQL). Cambiar un medidor =
  retirar el actual (con su lectura final) e instalar el nuevo.
- **Periodo de lectura:** la ultima semana de cada mes (`ReglasPeriodo`). Es una regla fija por ahora.
- **Lecturas** (`/lecturas`): estado de un periodo, con los servicios con y sin lectura. Con la ventana ya
  cerrada, cada servicio sin lectura genera una alerta; el operador la anota (`/lecturas/seguimiento/{id}`)
  eligiendo "Leer lo antes posible" o "Esperar al mes siguiente".
- **Registro de lectura** (`/lecturas/registrar/{id}`): calcula el consumo con `CalculadoraConsumo`.
  RB-013: si hubo cambio de medidor entre dos lecturas se suman los tramos de cada medidor.
- **Revision manual:** una lectura nunca se rechaza por "rara"; se guarda y queda marcada si (a) la fecha cae fuera
  de la ventana, (b) el periodo anterior quedo sin lectura (RB-014: el consumo NO se reparte entre meses), o
  (c) el consumo supera en mas de 50 % el promedio de los ultimos 3.
- Solo Administrador y Operador registran; Consulta solo ve. La fecha de "hoy" es la de Costa Rica (UTC-6).

Pasos:

```bash
dotnet build
dotnet test
dotnet ef migrations add Sprint2Mediciones \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion
dotnet run --project src/Asada.Presentacion
```

Recuerde: los formularios `EditForm` NO llevan `<AntiforgeryToken />` (ya lo incluyen).

## Sprint 3 -- Tarifas y recibos

- **Categorias** (`/tarifas`): cada ASADA define las suyas (p. ej. Domiciliar, Empresarial). Cada servicio se asigna
  a una desde el expediente (boton "Categoria", `/servicios/{id}/categoria`). Un servicio sin categoria no se factura.
- **Tarifas versionadas** (`/tarifas/nueva/{categoria}`): cargo fijo + hasta 11 tramos con limite + un precio de
  excedente obligatorio (consumo mayor al ultimo limite); cada m³ se cobra al precio de su tramo + hidrantes (por m³, monto fijo o no se cobra). Una tarifa no se
  edita: un cambio de precios es una version nueva con fecha "vigente desde" posterior a la anterior y al ultimo mes
  ya facturado. Para un periodo rige la ultima version vigente al cierre del mes.
- **Simulador** (`/tarifas/{id}?consumo=20`): calcula el desglose sin guardar nada, para revisar contra el pliego.
- **Recibos del periodo** (`/recibos`): estado de cada servicio (con recibo, listo, sin lectura, sin categoria,
  sin tarifa) y boton "Generar". Se puede generar varias veces: solo se facturan los que aun no tienen recibo.
  Sin lectura = no se factura. Lectura para revision = se factura y queda senalada.
- **Recibo** (`/recibos/{id}`): numero `CB-REC-2026-000001` (correlativo por ASADA y anio, reservado de forma atomica),
  datos congelados al emitir (C-02) y boton "Imprimir" del navegador. El PDF/DOCX llega en el Sprint 4.
- Permisos: categorias y tarifas solo Administrador; asignar categoria y generar recibos Administrador y Operador;
  Consulta ve todo. La anulacion de recibos llega en el Sprint 5.
- Montos con 2 decimales, redondeo comercial por linea; el total es la suma de las lineas.

Pasos:

```bash
dotnet build
dotnet test
dotnet ef migrations add Sprint3TarifasRecibos \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion
dotnet run --project src/Asada.Presentacion
```

Los tramos del formulario de tarifa se reciben como texto a proposito: el navegador envia vacias las filas sin
usar y un `decimal?` vacio dentro de una lista hace que el enlace de Blazor descarte la fila completa.

## Sprint 4 -- Recibos en PDF

`/recibos/{id}/pdf` y `/recibos/pdf?anio=&mes=`: media carta con talon, dos recibos por hoja carta (QuestPDF).

## Sprint 5a -- Seguridad, auditoria y cierre de periodo

Decisiones del 13 de octubre de 2026: anular recibos, corregir o anular lecturas y cerrar o reabrir periodos es
solo del Administrador, siempre con motivo.

- **Matriz de permisos** (`Seguridad/Permisos.cs`, pagina `/permisos`): cada accion sensible es una politica con
  nombre (`[Authorize(Policy = Permisos.AnularRecibos)]`); las paginas ya no escriben nombres de rol.
- **Auditoria automatica** (`/auditoria`, solo Administrador): un interceptor de EF Core (`InterceptorAuditoria`)
  anota cada alta o cambio de abonados, servicios, medidores, lecturas, periodos, tarifas, recibos y datos de la
  ASADA: quien, cuando y que campos (antes -> despues). Tabla `auditoria`, filtrada por organizacion.
- **Quien hizo que**: cada lectura guarda quien la registro y cada recibo quien lo genero.
- **Anular un recibo** (`/recibos/{id}/anular`): queda con su numero, marcado ANULADO, y el servicio vuelve a estar
  listo para facturar (el recibo nuevo tiene otro numero).
- **Corregir o anular una lectura** (`/lecturas/{id}/corregir`): solo la ultima lectura del servicio, sin recibo
  emitido (anule primero el recibo) y en periodo abierto. La anulada no se borra y deja libre el periodo.
- **Cierre de periodo** (`/periodos`): despues de la ventana de lectura y sin servicios con lectura pero sin recibo.
  Cerrado bloquea lecturas, correcciones, generacion y anulacion de recibos. Reabrir pide motivo.
- **Configuracion** (`/configuracion`): datos de contacto de la ASADA (se imprimen en el recibo) y umbral de consumo
  anormal por ASADA (antes fijo en 50 %).

Pasos (la migracion la genera usted; revise que `estado` de `mediciones` tenga `defaultValue: "Vigente"` y
`porcentaje_consumo_anormal` tenga `defaultValue: 50m`, para que los datos existentes queden bien):

```bash
dotnet build
dotnet test
dotnet ef migrations add Sprint5aSeguridadAuditoria \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion
dotnet run --project src/Asada.Presentacion
```

## Sprint 5b -- Usuarios y respaldos

Decisiones del 4 de noviembre de 2026: contrasenas de 10 o mas caracteres con letras y numeros, sin
vencimiento; 5 intentos fallidos bloquean la cuenta 15 minutos; el Superadministrador registra ASADAs y su
primer Administrador; respaldos con un contenedor de Docker.

- **Usuarios de la ASADA** (`/usuarios`, solo Administrador): crear, editar nombre/correo/rol, desactivar y
  reactivar, restablecer la contrasena y desbloquear. Al crear o restablecer, el sistema genera una
  **contrasena temporal** que se muestra una sola vez; el usuario debe cambiarla al entrar (hasta entonces solo
  ve la pagina de cambiar contrasena). Ninguna ASADA se queda sin un Administrador activo, y nadie se cambia el
  rol ni se desactiva a si mismo.
- **Cambiar contrasena** (`/cuenta/cambiar-contrasena`, enlace arriba a la derecha): cualquier usuario.
- **Sesiones**: al desactivar un usuario, cambiarle el rol o restablecer su contrasena, sus sesiones abiertas se
  cierran (o reciben los permisos nuevos) en menos de un minuto. Un usuario desactivado, o de una ASADA
  inactiva, no puede entrar.
- **Plataforma** (`/plataforma/asadas`, solo Superadministrador): lista de ASADAs, registrar una ASADA nueva con
  su primer Administrador, y entrar a los usuarios de cualquiera (`/usuarios?asada=N`). No ve abonados, lecturas
  ni recibos de las ASADAs.
- **Auditoria**: cada accion sobre usuarios, cada inicio de sesion, cada intento fallido (con el motivo) y cada
  bloqueo quedan en `/auditoria` (tipo de registro "Usuario"). Las contrasenas nunca se anotan.
- `EstadoUsuario` paso de `Asada.Infraestructura.Identidad` a `Asada.Dominio.Enums` (se guarda como texto; la
  base no cambia por esto). `NombresRol` ahora reutiliza `Asada.Aplicacion.Usuarios.RolesUsuario`.

Pasos (la migracion agrega solo la columna `usuarios.debe_cambiar_contrasena`; revise que tenga
`defaultValue: false`):

```bash
dotnet build
dotnet test
dotnet ef migrations add Sprint5bUsuarios \
  --project src/Asada.Infraestructura \
  --startup-project src/Asada.Presentacion
docker compose up -d
dotnet run --project src/Asada.Presentacion
```

## Respaldos

`docker compose up -d` levanta, junto a PostgreSQL, el contenedor `asada_respaldo`
(`scripts/respaldos/`):

- Un **respaldo completo** (`pg_dump`, formato personalizado y comprimido) cada dia a las 02:30 hora de Costa
  Rica, y uno al arrancar si hoy todavia no hay (por si la computadora estuvo apagada). Cada archivo se verifica
  con `pg_restore --list` y lleva su suma SHA-256.
- Se conservan los **ultimos 30 diarios** y **12 semanales** (una copia por semana). Se cambia con
  `RESPALDO_HORA`, `RESPALDO_DIARIOS` y `RESPALDO_SEMANALES` en `docker-compose.yml`.
- Una vez por semana, una **prueba de restauracion**: restaura el ultimo respaldo en una base temporal, revisa
  tablas y datos, y la borra. Resultado en `respaldos/pruebas-restauracion.log`.
- Los archivos quedan en la carpeta `respaldos/` del proyecto (excluida de Git: tienen datos personales de los
  abonados, Ley 8968). **Copie esa carpeta a otro disco o a la nube con regularidad**: un respaldo en el mismo
  disco no protege si el disco falla.

```bash
docker compose logs respaldo                                           # que hizo y cuando
docker compose exec respaldo bash /opt/respaldos/respaldar.sh                # respaldo ahora mismo
docker compose exec respaldo bash /opt/respaldos/probar-restauracion.sh      # probar el ultimo respaldo
docker compose exec respaldo bash /opt/respaldos/restaurar.sh                # lista los respaldos disponibles
docker compose exec respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump
#   -> lo restaura en una base nueva, asada_gestion_restaurada, sin tocar la base en uso
```

Para **reemplazar la base en uso** (recuperacion ante un desastre): detenga la aplicacion y confirme con
`CONFIRMAR=SI`. Antes de reemplazarla se hace un respaldo de la base actual, por si se eligio el archivo
equivocado:

```bash
docker compose exec -e CONFIRMAR=SI respaldo bash /opt/respaldos/restaurar.sh /respaldos/diarios/ARCHIVO.dump asada_gestion
```

El contenedor copia los scripts de `scripts/respaldos` a `/opt/respaldos` al arrancar, quitando los fines de
linea de Windows (CRLF); por eso los comandos usan `/opt/respaldos/`. Si cambia un script, reinicie el
contenedor: `docker compose restart respaldo`.

## Sprint 6a -- Despliegue en un servidor

Todo lo del servidor esta en `despliegue/` y su guia paso a paso en **`despliegue/LEEME.md`** (VPS con
Ubuntu + Docker, HTTPS con Caddy, respaldos fuera del servidor). Resumen:

- **`Dockerfile`** (raiz): imagen de la aplicacion (.NET 8, usuario sin privilegios, verificacion de salud en
  `/salud`). En el servidor no hace falta instalar .NET.
- **`despliegue/docker-compose.yml`**: postgres (sin puerto publicado), app, caddy (puertos 80/443) y respaldo
  (PostgreSQL 16 + rclone). Los secretos van en `despliegue/.env` (fuera de Git; plantilla `.env.ejemplo`).
  El `docker-compose.yml` de la raiz sigue siendo solo para desarrollo y no cambia.
- **Sin dominio propio**: `SITIO_DIRECCION=IP-CON-GUIONES.sslip.io` da un nombre gratuito con certificado
  valido de Let's Encrypt; con una IP sola, `SITIO_TLS=internal` (el navegador advierte). Pasar a un dominio
  propio es cambiar dos lineas del `.env`.
- **Comandos de la aplicacion** (`ComandosConsola.cs`): `migrar` aplica las migraciones, crea los roles y el
  Superadministrador (correo de `Plataforma__CorreoSuperadministrador`, contrasena temporal que se muestra una
  vez); `restablecer-superadministrador` genera otra temporal. En Produccion la aplicacion ya no migra sola:
  `actualizar.sh` respalda la base y luego ejecuta `migrar`. En el servidor no se crean la ASADA `CB` ni los
  usuarios `.test` de desarrollo: el Superadministrador registra Cuatro Bocas desde la pantalla.
- **Claves de la sesion persistentes** (`Despliegue:CarpetaClaves`, volumen `claves_app`): actualizar o
  reiniciar no saca a los usuarios de su sesion ni rompe los formularios abiertos.
- **Detras de Caddy**: `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (en el Dockerfile) hace que la aplicacion vea
  el esquema https y la IP real.
- **Respaldos fuera del servidor** (`scripts/respaldos/subir-respaldos.sh`): despues de cada respaldo se copian
  con rclone a los destinos de `RESPALDO_DESTINOS` (Google Drive, OneDrive, S3...; uno o varios, opcionalmente
  cifrados); si uno falla se reintenta cada hora. Sin destinos (como en desarrollo) no hace nada. Ademas,
  `scripts/despliegue/descargar-respaldos.ps1` baja a una PC con Windows los respaldos que falten y verifica su
  SHA-256.

En la PC de desarrollo nada cambia: `dotnet run` sigue migrando y sembrando datos de prueba al arrancar.

## Siguiente paso

Sprint 6b -- Importacion de abonados, propiedades, servicios, medidores y lecturas desde Excel (plantilla,
validacion con informe de errores antes de cargar, carga en una sola transaccion) y capacitacion.

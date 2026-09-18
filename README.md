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

**Importante:** esta contrasena esta escrita en texto plano en
`SeedInicial.cs` unicamente porque es un dato de arranque para desarrollo local. Antes de
usar datos reales o de desplegar en un servidor, hay que cambiarla o eliminar este usuario
y crear uno nuevo por la interfaz.

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

## Siguiente paso

Una vez que confirmes que `dotnet build` compila limpio en tu maquina (y me cuentes que
errores salieron, si salio alguno), seguimos con el **Sprint 1 -- Abonados**.

# Imagen de la aplicacion para el servidor (Sprint 6a). La construye GitHub Actions en cada cambio de
# master (.github/workflows/imagenes.yml) para amd64 y arm64, y el servidor solo la descarga. Tambien se
# puede construir en el servidor (docker compose build app); no hace falta tener .NET instalado alli.
#
# Etapa 1: compilar y publicar con el SDK de .NET 8. El SDK corre en la arquitectura de la maquina que
# construye (BUILDPLATFORM) y compila para la del servidor (TARGETARCH): asi la imagen arm64 (Oracle
# Ampere) se genera en GitHub sin emular un procesador Arm, que seria muy lento.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
ARG TARGETARCH
WORKDIR /fuente

# Primero solo los .csproj: mientras no cambien los paquetes, Docker reutiliza la descarga de NuGet
# y una actualizacion solo recompila el codigo.
COPY src/Asada.Dominio/Asada.Dominio.csproj src/Asada.Dominio/
COPY src/Asada.Aplicacion/Asada.Aplicacion.csproj src/Asada.Aplicacion/
COPY src/Asada.Infraestructura/Asada.Infraestructura.csproj src/Asada.Infraestructura/
COPY src/Asada.Presentacion/Asada.Presentacion.csproj src/Asada.Presentacion/
# amd64 -> x64 (nombre de .NET); arm64 queda igual. Sin TARGETARCH (docker build sin buildx), x64.
RUN arquitectura="$(echo "${TARGETARCH:-amd64}" | sed 's/amd64/x64/')" \
    && echo "$arquitectura" > /tmp/arquitectura \
    && dotnet restore src/Asada.Presentacion/Asada.Presentacion.csproj --arch "$arquitectura"

COPY src/ src/
RUN dotnet publish src/Asada.Presentacion/Asada.Presentacion.csproj \
        --configuration Release --no-restore --arch "$(cat /tmp/arquitectura)" --self-contained false \
        --output /publicado /p:UseAppHost=false

# Etapa 2: solo el runtime de ASP.NET Core (imagen mas liviana, sin el SDK ni el codigo fuente).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# curl: para la verificacion de salud. libfontconfig1: la usa el motor de QuestPDF (recibos en PDF)
# en Linux.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Carpeta de las claves de cifrado de la sesion (Despliegue:CarpetaClaves). El volumen de Docker que
# se monta aqui hereda este dueno, asi la aplicacion la puede escribir sin correr como root.
RUN mkdir -p /claves && chown app:app /claves

WORKDIR /app
COPY --from=compilacion /publicado .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    Despliegue__CarpetaClaves=/claves \
    TZ=America/Costa_Rica

# Usuario sin privilegios que ya trae la imagen oficial de .NET 8.
USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/salud || exit 1

# Sin argumentos levanta el sitio. Con "migrar" o "restablecer-superadministrador" ejecuta ese comando
# y termina (ver src/Asada.Presentacion/ComandosConsola.cs).
ENTRYPOINT ["dotnet", "Asada.Presentacion.dll"]

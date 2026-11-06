# Descarga a esta PC los respaldos del servidor que todavia no tenga (Sprint 6a), y verifica cada uno
# con su suma SHA-256. Es la copia "a mano" fuera del servidor: se puede correr cuando se quiera (por
# ejemplo, una vez por semana), ademas de las copias automaticas a Google Drive / OneDrive / S3.
#
# Requisitos: Windows 10/11 (trae ssh y scp) y acceso por SSH al servidor (ver despliegue/LEEME.md).
#
# Uso desde PowerShell, en la carpeta del repositorio:
#   .\scripts\despliegue\descargar-respaldos.ps1 -Servidor asada@203.0.113.5
# Opciones:
#   -Destino "D:\RespaldosASADA"        carpeta local (por defecto: Documentos\RespaldosASADA)
#   -RutaRemota /srv/asada/respaldos    carpeta de los respaldos en el servidor (RUTA_RESPALDOS del .env)
#   -SoloSemanales                      descarga solo la carpeta semanales (menos archivos)
#
# Si Windows no deja ejecutar el script ("la ejecucion de scripts esta deshabilitada"), use:
#   powershell -ExecutionPolicy Bypass -File .\scripts\despliegue\descargar-respaldos.ps1 -Servidor asada@203.0.113.5
#
# No borra nada: ni en el servidor ni en esta PC. Los archivos tienen datos personales de los abonados
# (Ley 8968): guardelos en un lugar protegido.

param(
    [Parameter(Mandatory = $true)][string]$Servidor,
    [string]$Destino = (Join-Path ([Environment]::GetFolderPath("MyDocuments")) "RespaldosASADA"),
    [string]$RutaRemota = "/srv/asada/respaldos",
    [switch]$SoloSemanales
)

$ErrorActionPreference = "Stop"

function Probar-Suma([string]$archivo) {
    $suma = "$archivo.sha256"
    if (-not (Test-Path -LiteralPath $suma)) { return $false }
    $esperada = ((Get-Content -LiteralPath $suma -TotalCount 1) -split '\s+')[0].ToLowerInvariant()
    $real = (Get-FileHash -LiteralPath $archivo -Algorithm SHA256).Hash.ToLowerInvariant()
    return ($esperada -eq $real)
}

$carpetas = if ($SoloSemanales) { @("semanales") } else { @("diarios", "semanales") }
$patrones = ($carpetas | ForEach-Object { "$_/*.dump" }) -join " "

Write-Host "Consultando los respaldos en $Servidor ..."
$lista = & ssh $Servidor "cd '$RutaRemota' && ls -1 $patrones 2>/dev/null"
if ($LASTEXITCODE -ne 0 -and -not $lista) {
    Write-Host "No se pudo leer la lista de respaldos en ${Servidor}:$RutaRemota (revise el acceso por SSH y la ruta)." -ForegroundColor Red
    exit 1
}

$remotos = @($lista | Where-Object { $_ -match '^(diarios|semanales)/[^/]+\.dump$' })
if ($remotos.Count -eq 0) {
    Write-Host "El servidor no tiene respaldos en $RutaRemota." -ForegroundColor Yellow
    exit 0
}

$descargados = 0; $yaEstaban = 0; $fallidos = 0
foreach ($relativo in $remotos) {
    $local = Join-Path $Destino ($relativo -replace '/', '\')
    New-Item -ItemType Directory -Force -Path (Split-Path $local) | Out-Null

    if ((Test-Path -LiteralPath $local) -and (Probar-Suma $local)) { $yaEstaban++; continue }

    Write-Host "Descargando $relativo ..."
    & scp -q "${Servidor}:$RutaRemota/$relativo" "${Servidor}:$RutaRemota/$relativo.sha256" (Split-Path $local)
    if ($LASTEXITCODE -eq 0 -and (Probar-Suma $local)) {
        $descargados++
    } else {
        Write-Host "  ERROR: $relativo no se descargo bien (o su suma SHA-256 no coincide)." -ForegroundColor Red
        Remove-Item -LiteralPath $local, "$local.sha256" -ErrorAction SilentlyContinue
        $fallidos++
    }
}

Write-Host ""
Write-Host "Listo: $descargados descargado(s), $yaEstaban ya estaban, $fallidos con error. Carpeta: $Destino"
if ($fallidos -gt 0) { exit 1 }

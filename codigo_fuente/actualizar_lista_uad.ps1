# Descarga la version mas reciente de la lista comunitaria de paquetes (Universal Android Debloater Next Generation,
# licencia GPL-3.0) y la guarda comprimida en recursos\uad_lists.json.gz.
# Despues: ejecuta COMPILAR.bat, o copia ese archivo junto a TV_Cleaner_Pro.exe (el programa lo usa si existe).
$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot
$url = 'https://raw.githubusercontent.com/Universal-Debloater-Alliance/universal-android-debloater-next-generation/main/resources/assets/uad_lists.json'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Write-Host 'Descargando lista UAD...'
$respuesta = Invoke-WebRequest -Uri $url -UseBasicParsing
$bytes = [Text.Encoding]::UTF8.GetBytes($respuesta.Content)
if ($bytes.Length -lt 100000) { throw 'La descarga parece incompleta. No se cambio nada.' }
$null = $respuesta.Content | ConvertFrom-Json   # verifica que sea JSON valido
$destino = Join-Path $PSScriptRoot 'recursos\uad_lists.json.gz'
if (Test-Path $destino) { Copy-Item $destino "$destino.anterior" -Force }
$archivo = [IO.File]::Create($destino)
$gz = New-Object IO.Compression.GZipStream($archivo, [IO.Compression.CompressionMode]::Compress)
$gz.Write($bytes, 0, $bytes.Length)
$gz.Close(); $archivo.Close()
Write-Host ("Listo: lista actualizada ({0:N0} bytes). Copia anterior: recursos\uad_lists.json.gz.anterior" -f $bytes.Length)

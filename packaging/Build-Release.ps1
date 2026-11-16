# WiX es una herramienta del desarrollador. Las dos descargas funcionan con Windows 11 de serie.
param([string]$WixBin = $env:WIX)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
& (Join-Path $root 'build.ps1') -OutputDirectory $dist
$version = [version]([Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $dist 'Marvi.exe')).FileVersion)
$release = $version.ToString(3)
$work = [IO.Directory]::CreateDirectory((Join-Path $root ('obj/package-' + [guid]::NewGuid()))).FullName
try {
    $portable = [IO.Directory]::CreateDirectory((Join-Path $work 'portable')).FullName
    Copy-Item -LiteralPath (Join-Path $dist 'Marvi.exe') -Destination $portable
    [IO.File]::WriteAllText((Join-Path $portable 'Marvi.portable'), '')
    Compress-Archive -Path (Join-Path $portable '*') -DestinationPath (Join-Path $work 'portable.zip')
    if ($WixBin -and (Test-Path (Join-Path $WixBin 'bin/candle.exe'))) { $WixBin = Join-Path $WixBin 'bin' }
    if (!$WixBin -or !(Test-Path (Join-Path $WixBin 'candle.exe'))) { throw 'Indicá la carpeta de WiX 3.14.1 con -WixBin.' }
    $object = Join-Path $work 'Marvi.wixobj'
    & (Join-Path $WixBin 'candle.exe') -nologo "-dVersion=$release" "-dSourceDir=$root/src/Marvi" "-dPayload=$dist" -out $object (Join-Path $PSScriptRoot 'Marvi.wxs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del instalador.' }
    & (Join-Path $WixBin 'light.exe') -nologo -sval -spdb -out (Join-Path $work 'Marvi.msi') $object
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el MSI.' }
    Copy-Item -LiteralPath (Join-Path $work 'Marvi.msi') -Destination (Join-Path $dist "Marvi-$release-Instalador.msi") -Force
    Copy-Item -LiteralPath (Join-Path $work 'portable.zip') -Destination (Join-Path $dist "Marvi-$release-Portable.zip") -Force
} finally {
    $parent = [IO.Path]::GetFullPath((Join-Path $root 'obj'))
    if ((Split-Path -Parent $work) -eq $parent -and (Split-Path -Leaf $work) -like 'package-*') {
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}

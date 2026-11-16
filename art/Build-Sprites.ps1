# Regenerate the runtime sprites from their editable artwork.
param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../src/Marvi/Assets'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$generator = Join-Path $env:TEMP ('Marvi-Art-' + [guid]::NewGuid() + '.exe')
try {
    & $compiler /nologo /target:exe "/out:$generator" /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'SpriteArtist.cs') (Join-Path $PSScriptRoot 'CareArtwork.cs') (Join-Path $PSScriptRoot 'TrickArtwork.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el generador.' }
    & $generator $PSScriptRoot $OutputDirectory
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron generar los sprites.' }
} finally {
    if (Test-Path -LiteralPath $generator) { Remove-Item -LiteralPath $generator }
}



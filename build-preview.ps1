# Conserva el punto de entrada de las versiones anteriores.
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory (Join-Path $PSScriptRoot 'preview')

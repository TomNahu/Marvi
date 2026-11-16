# Prueba la misma aplicación WPF que se distribuye, con un perfil temporal y un reloj controlado.
param([switch]$Measure)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = [IO.Directory]::CreateDirectory((Join-Path $root 'obj/checks')).FullName
& (Join-Path $root 'build.ps1') -OutputDirectory $out
$fx = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$refs = @('PresentationCore','PresentationFramework','WindowsBase','UIAutomationProvider','UIAutomationTypes') |
    ForEach-Object { '/r:' + (Join-Path $fx ('WPF/' + $_ + '.dll')) }
$checks = (Get-ChildItem -Path (Join-Path $PSScriptRoot '*.cs') -File).FullName
& (Join-Path $fx 'csc.exe') /nologo /optimize+ /codepage:65001 "/out:$out/Marvi.Checks.exe" "/r:$out/Marvi.exe" @refs /r:System.Xaml.dll /r:System.Runtime.Serialization.dll /r:System.Drawing.dll @checks
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron compilar las pruebas.' }
$arguments = @()
if ($Measure) { $arguments += 'measure' }
& (Join-Path $out 'Marvi.Checks.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Falló la verificación de Marvi.' }

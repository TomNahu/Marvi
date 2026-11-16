# Compila con el .NET Framework incluido en Windows 11, sin instalar un SDK.
param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'src/Marvi'
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$destination = [IO.Directory]::CreateDirectory($OutputDirectory).FullName
$bundle = Join-Path ([IO.Path]::GetTempPath()) ('Marvi-' + [Guid]::NewGuid() + '.resources')
try {
    $writer = [Resources.ResourceWriter]::new($bundle)
    try {
        # WPF busca streams por nombre en Marvi.g.resources. ResourceWriter toma propiedad de cada stream.
        foreach ($file in (Get-ChildItem -Path (Join-Path $source 'Assets/*.png') -File | Sort-Object Name)) {
            $writer.AddResource('assets/' + $file.Name, $file.OpenRead(), $true)
        }
        $writer.AddResource('menutheme.xaml', [IO.File]::OpenRead((Join-Path $source 'MenuTheme.xaml')), $true)
    } finally { $writer.Dispose() }

    $references = @('PresentationCore','PresentationFramework','WindowsBase') | ForEach-Object {
        '/r:' + (Join-Path $framework ('WPF/' + $_ + '.dll'))
    }
    $code = (Get-ChildItem -Path (Join-Path $source '*.cs') -File | Sort-Object Name).FullName
    & $compiler /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 "/out:$destination/Marvi.exe" "/resource:$bundle,Marvi.g.resources" "/win32icon:$source/Marvi.ico" @references /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll @code
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar Marvi.' }
} finally {
    if ([IO.File]::Exists($bundle)) { [IO.File]::Delete($bundle) }
}

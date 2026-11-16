param()
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$fx = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
New-Item -ItemType Directory -Force preview | Out-Null
$bundle = Join-Path ([IO.Path]::GetTempPath()) ('Marvi-'+[Guid]::NewGuid().ToString('N')+'.resources')
try {
    $writer = New-Object System.Resources.ResourceWriter($bundle)
    $streams = @()
    try {
        foreach ($file in Get-ChildItem src/Marvi/Assets/*.png) {
            $stream = $file.OpenRead()
            $streams += $stream
            $writer.AddResource("assets/$($file.Name)", $stream, $false)
        }
        $theme = [System.IO.File]::OpenRead((Resolve-Path src/Marvi/MenuTheme.xaml))
        $streams += $theme
        $writer.AddResource("menutheme.xaml", $theme, $false)
        $writer.Generate()
    } finally {
        $writer.Dispose()
        foreach ($stream in $streams) { $stream.Dispose() }
    }
    & "$fx/csc.exe" /nologo /target:winexe /out:preview\Marvi.exe "/resource:$bundle,Marvi.g.resources" "/r:$fx/WPF/PresentationCore.dll" "/r:$fx/WPF/PresentationFramework.dll" "/r:$fx/WPF/WindowsBase.dll" /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll @(Get-ChildItem src/Marvi/*.cs | ForEach-Object FullName)
    if ($LASTEXITCODE -ne 0) { throw "Falló la compilación de Marvi." }
} finally {
    if (Test-Path -LiteralPath $bundle) { Remove-Item -LiteralPath $bundle }
}

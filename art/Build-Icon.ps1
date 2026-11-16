# Empaqueta el dibujo de la casita en las escalas que solicita el Explorador de Windows.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'Marvi-casita.png'))
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
try {
    foreach ($size in $sizes) {
        $frame = [Drawing.Bitmap]::new($size, $size)
        $graphics = [Drawing.Graphics]::FromImage($frame)
        $stream = [IO.MemoryStream]::new()
        try {
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($source, 0, 0, $size, $size)
            $frame.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
            $images += ,$stream.ToArray()
        } finally { $stream.Dispose(); $graphics.Dispose(); $frame.Dispose() }
    }
} finally { $source.Dispose() }

$writer = [IO.BinaryWriter]::new([IO.File]::Create((Join-Path $PSScriptRoot '../src/Marvi/Marvi.ico')))
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = [byte]($sizes[$i] % 256)
        $writer.Write($dimension); $writer.Write($dimension); $writer.Write([uint16]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
} finally { $writer.Dispose() }

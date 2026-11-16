# Ensayo de producción. Usa el ZIP en una carpeta temporal y prueba instalación y retirada por usuario.
param([Parameter(Mandatory=$true)][string]$Installer, [Parameter(Mandatory=$true)][string]$Portable)
$ErrorActionPreference = 'Stop'
$installerFile = (Resolve-Path -LiteralPath $Installer).Path
$zipFile = (Resolve-Path -LiteralPath $Portable).Path
$tempRoot = [IO.Path]::GetTempPath().TrimEnd('\')
$run = [IO.Directory]::CreateDirectory((Join-Path $tempRoot ('Marvi-pack-' + [guid]::NewGuid()))).FullName
$installed = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs/Marvi/Marvi.exe'
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Marvi.lnk'
$dataPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Marvi'
$process = $null
$registered = $false
$passed = $false
function Invoke-Msi([string]$action, [string]$log) {
    $arguments = "$action `"$installerFile`" /qn /norestart /l*v `"$log`""
    $msi = Start-Process -FilePath msiexec.exe -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($msi.ExitCode -ne 0) { throw "Windows Installer devolvió $($msi.ExitCode). Registro: $log" }
}

# La mascota tiene una ventana transparente fuera de la barra de tareas. CloseMainWindow puede ignorarla.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class MarviPackageWindow {
    private delegate bool Visit(IntPtr window, IntPtr unused);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visit visit, IntPtr unused);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int process);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int size);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint msg, IntPtr wParam, IntPtr lParam);
    public static bool ClosePet(int pid) {
        bool sent=false;
        EnumWindows(delegate(IntPtr window, IntPtr unused) {
            int owner; GetWindowThreadProcessId(window, out owner);
            if (owner!=pid) return true;
            var title=new StringBuilder(128); GetWindowText(window,title,title.Capacity);
            if (title.ToString()!="Marvi") return true;
            sent=PostMessage(window,0x0010,IntPtr.Zero,IntPtr.Zero);
            return !sent;
        },IntPtr.Zero);
        return sent;
    }
}
'@
try {
    if (Get-Process -Name Marvi -ErrorAction SilentlyContinue) { throw 'Cerrá Marvi antes de probar los paquetes.' }
    $arp = Get-ChildItem 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall' -ErrorAction SilentlyContinue |
        Get-ItemProperty | Where-Object DisplayName -eq 'Marvi'
    if ($arp -or (Test-Path -LiteralPath $installed) -or (Test-Path -LiteralPath $shortcut)) {
        throw 'Hay una instalación previa. Esta prueba requiere un usuario sin Marvi instalado.'
    }
    $hadProfile = Test-Path -LiteralPath $dataPath
    $unzip = Join-Path $run 'portable'
    Expand-Archive -LiteralPath $zipFile -DestinationPath $unzip
    $files = @(Get-ChildItem -LiteralPath $unzip -Force)
    if ($files.Count -ne 2 -or !(Test-Path "$unzip/Marvi.portable") -or !(Test-Path "$unzip/Marvi.exe")) {
        throw 'Contenido inesperado en la descarga portátil.'
    }
    $process = Start-Process -FilePath (Join-Path $unzip 'Marvi.exe') -WorkingDirectory $unzip -PassThru
    if (!$process.WaitForInputIdle(15000)) { throw 'La edición portátil no terminó de iniciar.' }
    Start-Sleep -Seconds 2
    if ($process.HasExited -or !(Test-Path "$unzip/Datos/Cartas")) { throw 'La edición portátil no usó el perfil de la carpeta extraída.' }
    $process.Refresh()
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $initialCpu = $process.TotalProcessorTime.TotalSeconds
    $maxRss = $maxPrivate = 0
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 1
        $process.Refresh()
        $maxRss = [Math]::Max([long]$maxRss, $process.WorkingSet64)
        $maxPrivate = [Math]::Max([long]$maxPrivate, $process.PrivateMemorySize64)
    }
    'Portable, arranque limpio: {0:N1} MiB residentes, {1:N1} MiB privados, {2:N2}% de un núcleo ({3:N1} s).' -f ($maxRss/1MB), ($maxPrivate/1MB), (100*($process.TotalProcessorTime.TotalSeconds-$initialCpu)/$watch.Elapsed.TotalSeconds), $watch.Elapsed.TotalSeconds
    if (![MarviPackageWindow]::ClosePet($process.Id) -or !$process.WaitForExit(8000)) { throw 'La ventana portátil no cerró normalmente.' }
    Invoke-Msi '/i' (Join-Path $run 'instalar.log')
    $registered = $true
    if (!(Test-Path $shortcut) -or !(Test-Path $installed)) { throw 'Falta el acceso de Inicio o el ejecutable instalado.' }
    $link = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcut)
    if ([IO.Path]::GetFullPath($link.TargetPath) -ne [IO.Path]::GetFullPath($installed)) { throw 'Destino de Inicio incorrecto.' }
    if ((Get-FileHash $installed).Hash -ne (Get-FileHash "$unzip/Marvi.exe").Hash) { throw 'Los ejecutables difieren.' }
    Invoke-Msi '/x' (Join-Path $run 'desinstalar.log')
    $registered = $false
    if ((Test-Path $installed) -or (Test-Path $shortcut) -or $hadProfile -ne (Test-Path $dataPath)) { throw 'La desinstalación dejó archivos de programa o alteró el perfil.' }
    $passed = $true
    'Correcto: portátil independiente, MSI por usuario, acceso de Inicio, mismo ejecutable y desinstalación.'
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($registered) { Invoke-Msi '/x' (Join-Path $run 'limpiar.log') }
    if ($passed -and (Split-Path -Parent $run) -eq $tempRoot -and (Split-Path -Leaf $run) -like 'Marvi-pack-*') {
        Remove-Item -LiteralPath $run -Recurse -Force
    } elseif (!$passed) { Write-Output "Archivos de diagnóstico: $run" }
}

param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw   = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$csc  = Join-Path $fw 'csc.exe'
$wpf  = Join-Path $fw 'WPF'
$bin  = Join-Path $root 'bin'
New-Item -ItemType Directory -Force $bin | Out-Null

$core = Get-ChildItem (Join-Path $root 'src\Core') -Filter *.cs | ForEach-Object FullName
$app  = Get-ChildItem (Join-Path $root 'src\App')  -Filter *.cs | ForEach-Object FullName
$common = @('/nologo', '/optimize+', '/langversion:5', '/r:System.Core.dll', '/r:System.IO.Compression.dll')

function Invoke-Csc([string[]]$csArgs) {
    & $csc @csArgs
    if ($LASTEXITCODE -ne 0) { throw "Compilacion fallida" }
}

if ($Test) {
    Invoke-Csc ($common + @('/target:exe', "/out:$bin\CoreTests.exe") + $core + @((Join-Path $root 'tests\CoreTests.cs')))
    & "$bin\CoreTests.exe"
    exit $LASTEXITCODE
}

$refs = @("/r:$wpf\PresentationFramework.dll", "/r:$wpf\PresentationCore.dll", "/r:$wpf\WindowsBase.dll",
          "/r:$fw\System.Xaml.dll", '/r:System.dll')
$icon = Join-Path $root 'src\App\app.ico'
$extra = @()
if (Test-Path $icon) { $extra += "/win32icon:$icon" }
$manifest = Join-Path $root 'src\App\app.manifest'
if (Test-Path $manifest) { $extra += "/win32manifest:$manifest" }
Invoke-Csc ($common + $refs + $extra + @('/target:winexe', "/out:$bin\Dota2KeySync.exe") + $core + $app)
Write-Host "OK -> $bin\Dota2KeySync.exe"

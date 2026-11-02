$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $destination | Out-Null
& $compiler /nologo /target:winexe ("/out:" + (Join-Path $destination 'MoveReminder.exe')) ("/win32manifest:" + (Join-Path $PSScriptRoot 'app.manifest')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'MoveReminder.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Output (Join-Path $destination 'MoveReminder.exe')

param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $taskRoot 'publish\Applet.WindowMover.at365' }
dotnet publish (Join-Path $taskRoot 'Applet.WindowMover\Applet.WindowMover.csproj') -c Release -o $taskOutput
if ($LASTEXITCODE -ne 0) { throw "WindowMover publish failed ($LASTEXITCODE)" }
Copy-Item -LiteralPath (Join-Path $taskRoot 'extension.json') -Destination (Join-Path $taskOutput 'extension.json') -Force
Write-Output "Applet output: $taskOutput"

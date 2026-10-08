param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $taskRoot 'publish\Applet.WindowMover.at365' }
dotnet publish (Join-Path $taskRoot 'Applet.WindowMover\Applet.WindowMover.csproj') -c Release -o $taskOutput
if ($LASTEXITCODE -ne 0) { throw "WindowMover publish failed ($LASTEXITCODE)" }
Copy-Item -LiteralPath (Join-Path $taskRoot 'extension.json') -Destination (Join-Path $taskOutput 'extension.json') -Force
Write-Output "Applet output: $taskOutput"

# Match the existing deploy payload; remove obsolete build outputs before creating the ZIP.
foreach ($taskOldName in @('AppDock.SDK.dll', 'AppDock.SDK.pdb')) {
    $taskOldFile = Join-Path $taskOutput $taskOldName
    if (Test-Path -LiteralPath $taskOldFile -PathType Leaf) { Remove-Item -LiteralPath $taskOldFile }
}

$taskUpdateHostRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\AppDock.at365'))
& (Join-Path $taskUpdateHostRoot 'scripts\pack-applet-update.ps1') -SourceDirectory $taskOutput -OutputDirectory (Join-Path (Split-Path $PSScriptRoot -Parent) 'publish')

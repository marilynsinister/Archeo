param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskManifest = Get-Content -LiteralPath (Join-Path $taskRoot 'manifest.json') -Raw | ConvertFrom-Json
$taskOutput = Join-Path $taskRoot ('bin\' + $Configuration + '\net6.0')
$taskDll = Join-Path $taskOutput 'Archeo.dll'
if (!(Test-Path -LiteralPath $taskDll)) { throw 'Build Archeo.csproj before packaging.' }
$taskStage = Join-Path $taskRoot 'dist\Archeo'
New-Item -ItemType Directory -Path $taskStage -Force | Out-Null
foreach ($taskFile in @('manifest.json','assets','i18n','README.md','CHANGELOG.md','ROADMAP.md','LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $taskRoot $taskFile) -Destination $taskStage -Recurse -Force
}
Copy-Item -LiteralPath $taskDll -Destination $taskStage -Force
$taskZip = Join-Path $taskRoot ('dist\Archeo-' + $taskManifest.Version + '.zip')
Compress-Archive -LiteralPath $taskStage -DestinationPath $taskZip -Force
Write-Output $taskZip

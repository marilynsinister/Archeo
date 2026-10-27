#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Message = ('Deploy Archeo: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$GamePath = 'D:\Games\Steam\steamapps\common\Stardew Valley',
    [string]$GameModsPath = '',
    [string]$RepositoryUrl = 'https://github.com/marilynsinister/Archeo.git'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GameModsPath)) {
    $GameModsPath = Join-Path $GamePath 'mods'
}
$taskDeployPath = Join-Path $GameModsPath 'Archeo'

function Invoke-TaskGit {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & git -C $taskRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed (exit code $LASTEXITCODE)."
    }
}

function Get-RepositoryIdentity {
    param([string]$Url)
    return (($Url.TrimEnd('/') -replace '\.git$', '') -replace '^git@github\.com:', 'https://github.com/').TrimEnd('/')
}

Get-Command dotnet, git -ErrorAction Stop | Out-Null
foreach ($taskFile in @('Stardew Valley.dll', 'StardewModdingAPI.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $GamePath $taskFile) -PathType Leaf)) {
        throw "Game or SMAPI file is missing: $(Join-Path $GamePath $taskFile)"
    }
}

# A new source folder can be connected automatically only to an empty remote.
# Existing history must come from a clone, so it cannot be accidentally replaced.
if (!(Test-Path -LiteralPath (Join-Path $taskRoot '.git'))) {
    $taskRemoteHeads = @(Invoke-TaskGit ls-remote --heads $RepositoryUrl)
    if ($taskRemoteHeads.Count -gt 0) {
        throw "This folder has no .git, but the remote already has history. Clone $RepositoryUrl and copy the source files into that clone before deploying."
    }
    Invoke-TaskGit init --initial-branch=main
}

$taskRemotes = @(Invoke-TaskGit remote)
if ($taskRemotes -notcontains 'origin') {
    Invoke-TaskGit remote add origin $RepositoryUrl
}
foreach ($taskRemoteUrl in @((Invoke-TaskGit remote get-url --all origin)) + @((Invoke-TaskGit remote get-url --push --all origin))) {
    if ((Get-RepositoryIdentity $taskRemoteUrl) -ne (Get-RepositoryIdentity $RepositoryUrl)) {
        throw "origin points to another repository: $taskRemoteUrl. Expected $RepositoryUrl."
    }
}

# Verify the author and branch before changing the installed mod.
foreach ($taskSetting in @('user.name', 'user.email')) {
    $taskValue = & git -C $taskRoot config --get $taskSetting
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($taskValue)) {
        throw "Set your Git author first: git config --global $taskSetting YOUR_VALUE"
    }
}
$taskBranch = & git -C $taskRoot symbolic-ref --quiet --short HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Check out a Git branch before deploying (HEAD is detached).'
}

Write-Host "Building and deploying to $taskDeployPath"
$taskBuildArguments = @(
    'build', (Join-Path $taskRoot 'Archeo.csproj'),
    '--configuration', $Configuration,
    "-p:GamePath=$GamePath", "-p:GameModsPath=$GameModsPath",
    '-p:ModFolderName=Archeo', '-p:EnableModDeploy=true',
    '-p:EnableModZip=false', '-p:EnableGameDebugging=false',
    '-p:MSBuildEnableWorkloadResolver=false'
)
& dotnet @taskBuildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Build or deployment failed (exit code $LASTEXITCODE). No commit was created."
}

Invoke-TaskGit add --all -- .
& git -C $taskRoot diff --cached --quiet
$taskDiffExitCode = $LASTEXITCODE
if ($taskDiffExitCode -eq 1) {
    Invoke-TaskGit commit -m $Message
} elseif ($taskDiffExitCode -eq 0) {
    Write-Host 'No source changes to commit.'
} else {
    throw "Could not check staged changes (exit code $taskDiffExitCode)."
}

# Also retry a previous commit whose push failed. Never force-push history.
Invoke-TaskGit push --set-upstream origin "HEAD:refs/heads/$taskBranch"
Write-Host "Deployed to $taskDeployPath and pushed to $RepositoryUrl ($taskBranch)."

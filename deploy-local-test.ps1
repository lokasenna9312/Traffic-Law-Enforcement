$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $workspaceRoot 'Traffic Law Enforcement\Traffic Law Enforcement.csproj'
$buildOutput = Join-Path $workspaceRoot 'Traffic Law Enforcement\bin\Release\net48'
$deployRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData').Replace('Local', 'LocalLow')) 'Colossal Order\Cities Skylines II\Mods\Traffic Law Enforcement'
$toolchainRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Content\Game\.ModdingToolchain'
$vswherePath = Join-Path $toolchainRoot 'vswhere.exe'

if (Test-Path $vswherePath) {
    $msbuildPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($msbuildPath)) {
    throw 'MSBuild.exe was not found. Install the Visual Studio MSBuild component first.'
}

[System.Environment]::SetEnvironmentVariable('CSII_TOOLPATH', $toolchainRoot, 'User')
$env:CSII_TOOLPATH = $toolchainRoot
$modPropsPath = Join-Path $toolchainRoot 'Mod.props'
if (-not (Test-Path $modPropsPath)) {
    throw "Mod.props not found at '$modPropsPath'. Verify that Cities Skylines II modding toolchain is installed."
}
& $msbuildPath $projectPath /restore /t:Build /p:Configuration=Release /p:TargetFramework=net48 /v:m
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Force -Path $deployRoot | Out-Null
Get-ChildItem -Path $buildOutput -Filter '*.dll' | ForEach-Object {
    Copy-Item $_.FullName $deployRoot -Force
}

Get-ChildItem -Path $buildOutput -Filter '*.pdb' | ForEach-Object {
    Copy-Item $_.FullName $deployRoot -Force
}

$publishConfigPath = Join-Path $workspaceRoot 'Traffic Law Enforcement\Properties\PublishConfiguration.xml'
if (Test-Path $publishConfigPath) {
    Copy-Item $publishConfigPath $deployRoot -Force
}

Write-Output "Deployed local smoke-test files to: $deployRoot"

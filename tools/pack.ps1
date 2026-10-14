param(
    [string]$Version = "0.1.0",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "publish\ZZZKeyEcho"
$zipName = "ZZZKeyEcho-v$Version-$Runtime.zip"
$zipPath = Join-Path $root "publish\$zipName"

Write-Host "清理旧的发布目录..."
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

Write-Host "发布 $Configuration / $Runtime..."
dotnet publish (Join-Path $root "src\ZZZKeyEcho.App\ZZZKeyEcho.App.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained `
    "-p:PublishSingleFile=true" `
    "-p:Version=$Version" `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "发布失败（exit $LASTEXITCODE）"
}

Get-ChildItem -Path $publishDir -Filter *.pdb -Recurse | Remove-Item -Force

Write-Host "打包 $zipName..."
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)

Set-Content -Path "$zipPath.sha256" -Value "$hash  $zipName" -Encoding ASCII

Write-Host ""
Write-Host "完成：publish\$zipName（$size MB）"
Write-Host "SHA256：$hash"
Write-Host "（已写入 publish\$zipName.sha256）"

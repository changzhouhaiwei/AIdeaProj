param(
    [string]$VersionName = "1.0.0",
    [int]$BuildNumber = 1
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$androidProject = Join-Path $repoRoot "SortAndroid"
$jniLibs = Join-Path $androidProject "unityLibrary\src\main\jniLibs"

if (-not (Test-Path $jniLibs)) {
    Write-Host "SortAndroid 还没有 Unity 导出的 so。"
    Write-Host "用 Unity 打开 SortClient，Android Export Project 到 SortAndroid 之后再跑这个脚本。"
    exit 1
}

$gradle = Join-Path $androidProject "gradlew.bat"
if (-not (Test-Path $gradle)) {
    $gradle = "gradle"
}

Push-Location $androidProject
try {
    & $gradle ":launcher:assembleRelease" "-PVERSION_NAME=$VersionName" "-PVERSION_CODE=$BuildNumber"
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}

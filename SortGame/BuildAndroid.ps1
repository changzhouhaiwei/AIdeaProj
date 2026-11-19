param(
    [string]$VersionName,
    [int]$BuildNumber,
    [string]$BuildTarget,
    [string]$SkipUnityExport = "false",
    [string]$ExportSymbolsZip = "false",
    [string]$UploadCrashlyticsMapping = "false",
    [string]$GradleProxyHost = "127.0.0.1",
    [int]$GradleProxyPort = 7890
)

$ErrorActionPreference = "Stop"
$script:transcriptStarted = $false

trap {
    if ($script:transcriptStarted) {
        Stop-Transcript | Out-Null
        $script:transcriptStarted = $false
    }

    throw $_
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$unityProject = Join-Path $repoRoot "SortClient"
$projectVersionFile = Join-Path $unityProject "ProjectSettings\ProjectVersion.txt"
$androidProject = Join-Path $repoRoot "SortAndroid"
$launcherGradle = Join-Path $androidProject "launcher\build.gradle"
$symbolsRoot = Join-Path $androidProject "unityLibrary\symbols"
$wrapperProperties = Join-Path $androidProject "gradle\wrapper\gradle-wrapper.properties"
$outputRoot = Join-Path $repoRoot "BuildOutputs\Android"
$logRoot = Join-Path $outputRoot "Logs"
$localGradleRoot = Join-Path $repoRoot ".gradle\android-build-tools"
$localJdkRoot = Join-Path $localGradleRoot "jdk17"
$gameName = "Sort"
$apkStoreFile = Join-Path $repoRoot "Tools\sort-apk.jks"
$apkStorePassword = "123456"
$apkKeyAlias = "sortapk"
$apkKeyPassword = "123456"
$releaseStoreFile = Join-Path $repoRoot "Tools\sofishmatch.jks"
$releaseStorePassword = "123456"
$releaseKeyAlias = "key0123456"
$releaseKeyPassword = "123456"

function Read-UnityVersion {
    if (-not (Test-Path $projectVersionFile)) {
        return $null
    }

    $content = Get-Content -Path $projectVersionFile -Raw
    $match = [regex]::Match($content, "m_EditorVersion:\s*([^\r\n]+)")
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    return $null
}

function Resolve-UnityEditorCommand {
    $unityVersion = Read-UnityVersion
    if (-not [string]::IsNullOrWhiteSpace($unityVersion)) {
        $versionedUnity = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
        if (Test-Path $versionedUnity) {
            return $versionedUnity
        }
    }

    $unityCommand = Get-Command "Unity.exe" -ErrorAction SilentlyContinue
    if ($null -ne $unityCommand) {
        return $unityCommand.Source
    }

    $unityCommand = Get-Command "Unity" -ErrorAction SilentlyContinue
    if ($null -ne $unityCommand) {
        return $unityCommand.Source
    }

    throw "Unity Editor not found. Expected version from ProjectVersion.txt: $unityVersion"
}

function Invoke-UnityAndroidProjectExport([string]$version, [int]$build, [string]$timestamp) {
    if (-not (Test-Path $unityProject)) {
        throw "Unity project not found: $unityProject"
    }

    $unity = Resolve-UnityEditorCommand
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
    $unityLogPath = Join-Path $logRoot "${gameName}_${version}_${build}_unity-export_${timestamp}.log"
    $unityArgs = @(
        "-batchmode",
        "-quit",
        "-projectPath", $unityProject,
        "-executeMethod", "CustomBuildPlayer.ExportAndroidProjectCommandLine",
        "-VersionName", $version,
        "-BuildNumber", $build,
        "-logFile", $unityLogPath
    )

    Write-Host ""
    Write-Host "Exporting Android project with Unity..."
    Write-Host "Unity: $unity"
    Write-Host "Unity project: $unityProject"
    Write-Host "Unity export log: $unityLogPath"
    & $unity @unityArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Unity Android project export failed. See log: $unityLogPath"
    }
}

function Read-DefaultVersionName {
    if (-not (Test-Path $launcherGradle)) {
        return "1.0.7"
    }

    $content = Get-Content -Path $launcherGradle -Raw
    $match = [regex]::Match($content, "VERSION_NAME'\)\s*\?:\s*'([^']+)'")
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    $match = [regex]::Match($content, "versionName\s+'([^']+)'")
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return "1.0.7"
}

function Read-DefaultBuildNumber {
    if (-not (Test-Path $launcherGradle)) {
        return 7
    }

    $content = Get-Content -Path $launcherGradle -Raw
    $match = [regex]::Match($content, "VERSION_CODE'\)\s*\?:\s*'(\d+)'")
    if ($match.Success) {
        return [int]$match.Groups[1].Value
    }

    $match = [regex]::Match($content, "versionCode\s+(\d+)")
    if ($match.Success) {
        return [int]$match.Groups[1].Value
    }

    return 7
}

function ConvertTo-BuildTarget([string]$value) {
    switch ($value.ToLowerInvariant()) {
        "1" { return "apk" }
        "apk" { return "apk" }
        "2" { return "aab" }
        "aab" { return "aab" }
        "3" { return "both" }
        "both" { return "both" }
        default { throw "Invalid build option: $value" }
    }
}

function ConvertTo-Bool([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $false
    }

    switch ($value.ToLowerInvariant()) {
        "1" { return $true }
        "true" { return $true }
        "yes" { return $true }
        "y" { return $true }
        default { return $false }
    }
}

function Get-JavaHomeFromRoot([string]$rootPath) {
    if ([string]::IsNullOrWhiteSpace($rootPath) -or -not (Test-Path $rootPath)) {
        return $null
    }

    $javaExe = Get-ChildItem -Path $rootPath -Filter "java.exe" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*\bin\java.exe" } |
        Select-Object -First 1

    if ($null -eq $javaExe) {
        return $null
    }

    return Split-Path -Parent (Split-Path -Parent $javaExe.FullName)
}

function Test-Java17OrNewer([string]$javaHome) {
    if ([string]::IsNullOrWhiteSpace($javaHome)) {
        return $false
    }

    $javaExe = Join-Path $javaHome "bin\java.exe"
    if (-not (Test-Path $javaExe)) {
        return $false
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $versionOutput = & $javaExe -version 2>&1 | Out-String
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $match = [regex]::Match($versionOutput, 'version "(\d+)')
    if (-not $match.Success) {
        return $false
    }

    return [int]$match.Groups[1].Value -ge 17
}

function Get-UnityJdkHome {
    if (-not (Test-Path $launcherGradle)) {
        return $null
    }

    $content = Get-Content -Path $launcherGradle -Raw
    $match = [regex]::Match($content, 'ndkPath\s+"([^"]+)"')
    if (-not $match.Success) {
        return $null
    }

    $ndkPath = $match.Groups[1].Value -replace '/', '\'
    $androidPlayerPath = Split-Path -Parent $ndkPath
    return Get-JavaHomeFromRoot (Join-Path $androidPlayerPath "OpenJDK")
}

function Install-Jdk17 {
    $jdk = Get-JavaHomeFromRoot $localJdkRoot
    if (-not [string]::IsNullOrWhiteSpace($jdk)) {
        return $jdk
    }

    $jdkZip = Join-Path $localJdkRoot "temurin-jdk17-windows-x64.zip"
    New-Item -ItemType Directory -Force -Path $localJdkRoot | Out-Null

    if (-not (Test-Path $jdkZip)) {
        $url = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse?project=jdk"
        Write-Host "Downloading JDK 17 from: $url"
        Write-Host "Saving to: $jdkZip"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $url -OutFile $jdkZip
    }

    Write-Host "Extracting JDK 17..."
    Expand-Archive -Path $jdkZip -DestinationPath $localJdkRoot -Force

    $jdk = Get-JavaHomeFromRoot $localJdkRoot
    if ([string]::IsNullOrWhiteSpace($jdk)) {
        throw "JDK 17 was downloaded but java.exe was not found under $localJdkRoot"
    }

    return $jdk
}

function Initialize-JavaHome {
    $candidates = @(
        $env:JAVA_HOME,
        (Get-JavaHomeFromRoot $localJdkRoot),
        (Get-UnityJdkHome)
    )

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Java17OrNewer $candidate) {
            $env:JAVA_HOME = $candidate
            $env:Path = "$(Join-Path $candidate "bin");$env:Path"
            Write-Host "JAVA_HOME: $env:JAVA_HOME"
            return
        }
    }

    $installedJdk = Install-Jdk17
    $env:JAVA_HOME = $installedJdk
    $env:Path = "$(Join-Path $installedJdk "bin");$env:Path"
    Write-Host "JAVA_HOME: $env:JAVA_HOME"
}

function Read-GradleDistributionUrl {
    $line = Get-Content -Path $wrapperProperties |
        Where-Object { $_ -match '^\s*distributionUrl\s*=' } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($line)) {
        throw "distributionUrl was not found in $wrapperProperties"
    }

    $url = ($line -replace '^\s*distributionUrl\s*=\s*', '').Trim()
    return $url -replace '\\:', ':' -replace '\\/', '/'
}

function Get-RequiredGradleVersionFolder {
    $url = Read-GradleDistributionUrl
    $fileName = [System.IO.Path]::GetFileName(([System.Uri]$url).AbsolutePath)
    # gradle-8.13-bin.zip -> gradle-8.13
    if ($fileName -match '^(gradle-.+?)-(bin|all)\.zip$') {
        return $Matches[1]
    }

    throw "Unable to parse Gradle version from distributionUrl file name: $fileName"
}

function Get-LocalGradleCommand([string]$requiredVersionFolder) {
    if (-not (Test-Path $localGradleRoot)) {
        return $null
    }

    $versionRoot = Join-Path $localGradleRoot $requiredVersionFolder
    if (-not (Test-Path $versionRoot)) {
        return $null
    }

    $gradleBat = Get-ChildItem -Path $versionRoot -Filter "gradle.bat" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*\bin\gradle.bat" } |
        Select-Object -First 1

    if ($null -eq $gradleBat) {
        return $null
    }

    return $gradleBat.FullName
}

function Install-GradleFromWrapper {
    $url = Read-GradleDistributionUrl
    $fileName = [System.IO.Path]::GetFileName(([System.Uri]$url).AbsolutePath)
    $requiredVersionFolder = Get-RequiredGradleVersionFolder
    $zipPath = Join-Path $localGradleRoot $fileName

    New-Item -ItemType Directory -Force -Path $localGradleRoot | Out-Null

    if (-not (Test-Path $zipPath)) {
        Write-Host "Downloading Gradle from: $url"
        Write-Host "Saving to: $zipPath"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $url -OutFile $zipPath
    }

    Write-Host "Extracting Gradle $requiredVersionFolder..."
    Expand-Archive -Path $zipPath -DestinationPath $localGradleRoot -Force

    $gradle = Get-LocalGradleCommand $requiredVersionFolder
    if ([string]::IsNullOrWhiteSpace($gradle)) {
        throw "Gradle was downloaded but gradle.bat was not found under $(Join-Path $localGradleRoot $requiredVersionFolder)"
    }

    return $gradle
}

function Resolve-GradleCommand {
    $gradlew = Join-Path $androidProject "gradlew.bat"
    if (Test-Path $gradlew) {
        return $gradlew
    }

    $requiredVersionFolder = Get-RequiredGradleVersionFolder
    $localGradle = Get-LocalGradleCommand $requiredVersionFolder
    if (-not [string]::IsNullOrWhiteSpace($localGradle)) {
        Write-Host "Using local Gradle: $localGradle"
        return $localGradle
    }

    Write-Host "Local Gradle $requiredVersionFolder not found, installing from wrapper distributionUrl..."
    return Install-GradleFromWrapper
}

function Invoke-GradleBuild([string]$taskName, [string]$version, [int]$build, [bool]$useReleaseSigning, [bool]$uploadCrashlyticsMapping, [string]$proxyHost, [int]$proxyPort, [string]$storeFile, [string]$storePassword, [string]$keyAlias, [string]$keyPassword) {
    if ($useReleaseSigning -and -not (Test-Path $storeFile)) {
        throw "Release keystore not found: $storeFile"
    }

    $gradle = Resolve-GradleCommand
    $gradleArgs = @(
        $taskName,
        "--no-daemon",
        "-PVERSION_NAME=$version",
        "-PVERSION_CODE=$build",
        "-PUSE_RELEASE_SIGNING=$useReleaseSigning",
        "-PUPLOAD_CRASHLYTICS_MAPPING=$uploadCrashlyticsMapping",
        "-PRELEASE_STORE_FILE=$storeFile",
        "-PRELEASE_STORE_PASSWORD=$storePassword",
        "-PRELEASE_KEY_ALIAS=$keyAlias",
        "-PRELEASE_KEY_PASSWORD=$keyPassword"
    )

    if ($uploadCrashlyticsMapping -and -not [string]::IsNullOrWhiteSpace($proxyHost) -and $proxyPort -gt 0) {
        $gradleArgs += @(
            "-Dhttp.proxyHost=$proxyHost",
            "-Dhttp.proxyPort=$proxyPort",
            "-Dhttps.proxyHost=$proxyHost",
            "-Dhttps.proxyPort=$proxyPort"
        )
    }

    $displayArgs = $gradleArgs | ForEach-Object {
        if ($_ -match 'PASSWORD=') {
            return ($_ -replace '=.*$', '=******')
        }

        return $_
    }

    Write-Host ""
    Write-Host "Running: $gradle $($displayArgs -join ' ')"
    & $gradle @gradleArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Gradle task failed: $taskName"
    }
}

function Copy-LatestArtifact([string]$sourceDir, [string]$extension, [string]$version, [int]$build, [string]$timestamp, [string]$targetDir) {
    if (-not (Test-Path $sourceDir)) {
        throw "Artifact directory not found: $sourceDir"
    }

    $artifact = Get-ChildItem -Path $sourceDir -Filter "*.$extension" -Recurse |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $artifact) {
        throw "No .$extension artifact found in $sourceDir"
    }

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    $safeVersion = $version -replace '[^\w\.-]', '_'
    $targetFile = Join-Path $targetDir "${gameName}_v${safeVersion}_b${build}_${timestamp}.$extension"
    Copy-Item -Path $artifact.FullName -Destination $targetFile -Force
    Write-Host "Copied $extension to: $targetFile"
}

function Export-SymbolsZip([string]$version, [int]$build, [string]$timestamp, [string]$targetDir) {
    if (-not (Test-Path $symbolsRoot)) {
        Write-Warning "Symbols directory not found, skip symbols.zip: $symbolsRoot"
        return
    }

    $symbolFiles = Get-ChildItem -Path $symbolsRoot -Recurse -File -ErrorAction SilentlyContinue
    if ($null -eq $symbolFiles -or $symbolFiles.Count -eq 0) {
        Write-Warning "Symbols directory is empty, skip symbols.zip: $symbolsRoot"
        return
    }

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    $safeVersion = $version -replace '[^\w\.-]', '_'
    $zipPath = Join-Path $targetDir "${gameName}_v${safeVersion}_b${build}_${timestamp}_symbols.zip"

    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $symbolsRoot "*") -DestinationPath $zipPath -Force
    Write-Host "Copied symbols.zip to: $zipPath"
}

function Remove-PreviousOutputs([string]$version, [int]$build, [string]$target, [string]$targetDir) {
    $safeVersion = $version -replace '[^\w\.-]', '_'

    if ($target -eq "both") {
        if (Test-Path $targetDir) {
            Remove-Item -Path $targetDir -Recurse -Force
            Write-Host "Removed previous output folder: $targetDir"
        }

        return
    }

    if (-not (Test-Path $outputRoot)) {
        return
    }

    $patterns = @(
        "${gameName}_v${safeVersion}_b${build}_*.apk",
        "${gameName}_v${safeVersion}_b${build}_*.aab",
        "${gameName}_v${safeVersion}_b${build}_*_symbols.zip"
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $outputRoot -Filter $pattern -File -ErrorAction SilentlyContinue |
            ForEach-Object {
                Remove-Item -Path $_.FullName -Force
                Write-Host "Removed previous output file: $($_.FullName)"
            }
    }
}

$defaultVersionName = Read-DefaultVersionName
$defaultBuildNumber = Read-DefaultBuildNumber

if ([string]::IsNullOrWhiteSpace($VersionName)) {
    $inputVersion = Read-Host "Version name [$defaultVersionName]"
    if ([string]::IsNullOrWhiteSpace($inputVersion)) {
        $VersionName = $defaultVersionName
    } else {
        $VersionName = $inputVersion.Trim()
    }
}

if ($BuildNumber -le 0) {
    $inputBuild = Read-Host "Build number [$defaultBuildNumber]"
    if ([string]::IsNullOrWhiteSpace($inputBuild)) {
        $BuildNumber = $defaultBuildNumber
    } elseif ([int]::TryParse($inputBuild.Trim(), [ref]$BuildNumber) -eq $false -or $BuildNumber -le 0) {
        throw "Build number must be a positive integer."
    }
}

if ([string]::IsNullOrWhiteSpace($BuildTarget)) {
    Write-Host ""
    Write-Host "Build option:"
    Write-Host "1. Export APK"
    Write-Host "2. Export AAB"
    Write-Host "3. Export APK and AAB"
    $BuildTarget = Read-Host "Select [3]"
    if ([string]::IsNullOrWhiteSpace($BuildTarget)) {
        $BuildTarget = "3"
    }
}

$normalizedTarget = ConvertTo-BuildTarget $BuildTarget
$shouldSkipUnityExport = ConvertTo-Bool $SkipUnityExport
$shouldExportSymbolsZip = ConvertTo-Bool $ExportSymbolsZip
$shouldUploadCrashlyticsMapping = ConvertTo-Bool $UploadCrashlyticsMapping
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$safeVersion = $VersionName -replace '[^\w\.-]', '_'
$finalOutputDir = $outputRoot
if ($normalizedTarget -eq "both") {
    $finalOutputDir = Join-Path $outputRoot "${gameName}_${safeVersion}_${BuildNumber}"
}

Remove-PreviousOutputs $VersionName $BuildNumber $normalizedTarget $finalOutputDir

New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
$logPath = Join-Path $logRoot "${gameName}_${safeVersion}_${BuildNumber}_${normalizedTarget}_${timestamp}.log"
Start-Transcript -Path $logPath -Force | Out-Null
$script:transcriptStarted = $true

Write-Host ""
Write-Host "Android project: $androidProject"
Write-Host "Version name: $VersionName"
Write-Host "Build number: $BuildNumber"
Write-Host "Build target: $normalizedTarget"
Write-Host "Skip Unity export: $shouldSkipUnityExport"
Write-Host "Export symbols.zip: $shouldExportSymbolsZip"
Write-Host "Upload Crashlytics mapping: $shouldUploadCrashlyticsMapping"
Write-Host "APK keystore: $apkStoreFile"
Write-Host "AAB keystore: $releaseStoreFile"
if ($shouldUploadCrashlyticsMapping -and -not [string]::IsNullOrWhiteSpace($GradleProxyHost) -and $GradleProxyPort -gt 0) {
    Write-Host "Gradle proxy: ${GradleProxyHost}:${GradleProxyPort}"
}
Write-Host "Gradle wrapper properties kept: $wrapperProperties"
Write-Host "Build log: $logPath"

if (-not $shouldSkipUnityExport) {
    Invoke-UnityAndroidProjectExport $VersionName $BuildNumber $timestamp
}

if (-not (Test-Path $androidProject)) {
    throw "Android project not found: $androidProject"
}

if (-not (Test-Path $wrapperProperties)) {
    throw "Missing gradle wrapper properties: $wrapperProperties"
}

Initialize-JavaHome

Push-Location $androidProject
try {
    if ($normalizedTarget -eq "apk" -or $normalizedTarget -eq "both") {
        Invoke-GradleBuild ":launcher:assembleRelease" $VersionName $BuildNumber $true $shouldUploadCrashlyticsMapping $GradleProxyHost $GradleProxyPort $apkStoreFile $apkStorePassword $apkKeyAlias $apkKeyPassword
        Copy-LatestArtifact (Join-Path $androidProject "launcher\build\outputs\apk\release") "apk" $VersionName $BuildNumber $timestamp $finalOutputDir
    }

    if ($normalizedTarget -eq "aab" -or $normalizedTarget -eq "both") {
        Invoke-GradleBuild ":launcher:bundleRelease" $VersionName $BuildNumber $true $shouldUploadCrashlyticsMapping $GradleProxyHost $GradleProxyPort $releaseStoreFile $releaseStorePassword $releaseKeyAlias $releaseKeyPassword
        Copy-LatestArtifact (Join-Path $androidProject "launcher\build\outputs\bundle\release") "aab" $VersionName $BuildNumber $timestamp $finalOutputDir
    }
}
finally {
    Pop-Location
}

if ($shouldExportSymbolsZip) {
    Export-SymbolsZip $VersionName $BuildNumber $timestamp $finalOutputDir
}

Write-Host ""
Write-Host "Done. Output directory: $finalOutputDir"
Start-Process explorer.exe -ArgumentList "`"$finalOutputDir`""
Stop-Transcript | Out-Null
$script:transcriptStarted = $false

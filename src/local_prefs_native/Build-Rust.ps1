[CmdletBinding()]
param(
    [string]$UnityEditorPath
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$unityProjectPath = Join-Path $projectRoot "src\LocalPrefs.Unity"

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $projectVersionPath = Join-Path $unityProjectPath "ProjectSettings\ProjectVersion.txt"
    $versionLine = Get-Content $projectVersionPath -Encoding UTF8 | Select-Object -First 1
    $unityVersion = ($versionLine -split ":", 2)[1].Trim()
    $UnityEditorPath = Join-Path "C:\Program Files\Unity\Hub\Editor" "$unityVersion\Editor\Unity.exe"
}

if (-not (Test-Path $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor was not found at '$UnityEditorPath'. Pass -UnityEditorPath explicitly."
}

$unityEditorDirectory = Split-Path $UnityEditorPath
$emscriptenRoot = Join-Path $unityEditorDirectory "Data\PlaybackEngines\WebGLSupport\BuildTools\Emscripten"
$emccPath = Join-Path $emscriptenRoot "emscripten\emcc.bat"

if (-not (Test-Path $emccPath -PathType Leaf)) {
    throw "Unity WebGL Build Support was not found for '$UnityEditorPath'."
}

$previousEmConfig = $env:EM_CONFIG
$previousPath = $env:PATH
$previousRustFlags = $env:RUSTFLAGS

try {
    $env:EM_CONFIG = Join-Path $emscriptenRoot ".emscripten"
    $env:PATH = (Join-Path $emscriptenRoot "emscripten") + ";" +
        (Join-Path $emscriptenRoot "llvm") + ";" + $env:PATH
    $env:RUSTFLAGS = "-Ctarget-cpu=mvp"

    rustup component add rust-src --toolchain nightly
    if ($LASTEXITCODE -ne 0) {
        throw "Installing the nightly rust-src component failed with exit code $LASTEXITCODE."
    }

    rustup target add wasm32-unknown-emscripten --toolchain nightly
    if ($LASTEXITCODE -ne 0) {
        throw "Installing the wasm32-unknown-emscripten target failed with exit code $LASTEXITCODE."
    }

    cargo +nightly build `
        -Z build-std=panic_abort,std `
        --target wasm32-unknown-emscripten `
        --release `
        --lib `
        --manifest-path (Join-Path $PSScriptRoot "Cargo.toml")

    if ($LASTEXITCODE -ne 0) {
        throw "The Rust WebGL build failed with exit code $LASTEXITCODE."
    }

    $sourcePath = Join-Path $PSScriptRoot "target\wasm32-unknown-emscripten\release\liblocal_prefs_native.a"
    $destinationPath = Join-Path $unityProjectPath "Packages\jp.andantetribe.localprefs\Plugins\WebGL\liblocal_prefs_native.a"
    Copy-Item $sourcePath $destinationPath -Force
}
finally {
    $env:EM_CONFIG = $previousEmConfig
    $env:PATH = $previousPath
    $env:RUSTFLAGS = $previousRustFlags
}

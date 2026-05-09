param(
    [string]$UnsignedApk = "D:\Multiplayer-Gun-Battle\TempApkPatch\patched-aligned-unsigned.apk",
    [string]$OutputApk = "C:\Users\30398\OneDrive\Desktop\GameTest\球球战争-patched.apk",
    [string]$Keystore = "D:\Multiplayer-Gun-Battle\user.keystore",
    [string]$Alias = "homieboy",
    [string]$ApkSigner = "C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t7\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\34.0.0\apksigner.bat"
)

$ErrorActionPreference = "Stop"

function ConvertToPlainText([securestring]$SecureValue) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

if (-not (Test-Path -LiteralPath $UnsignedApk)) {
    throw "Unsigned APK not found: $UnsignedApk"
}

if (-not (Test-Path -LiteralPath $Keystore)) {
    throw "Keystore not found: $Keystore"
}

if (-not (Test-Path -LiteralPath $ApkSigner)) {
    throw "apksigner not found: $ApkSigner"
}

$ksPass = ConvertToPlainText (Read-Host "Keystore password" -AsSecureString)
$keyPass = ConvertToPlainText (Read-Host "Key password for alias '$Alias' (press Enter if same as keystore)")

if ([string]::IsNullOrEmpty($keyPass)) {
    $keyPass = $ksPass
}

Remove-Item -LiteralPath $OutputApk -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath $UnsignedApk -Destination $OutputApk

& $ApkSigner sign `
    --ks $Keystore `
    --ks-key-alias $Alias `
    --ks-pass "pass:$ksPass" `
    --key-pass "pass:$keyPass" `
    --out $OutputApk `
    $OutputApk

& $ApkSigner verify --verbose --print-certs $OutputApk

Write-Host ""
Write-Host "Signed patched APK:"
Write-Host $OutputApk

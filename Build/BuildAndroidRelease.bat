echo off

if %1.==. goto MissingArgs
if %2.==. goto MissingArgs

REM Debug or Release?
set buildmode=Release

set keystore=%1
set keystorepassword=%2

REM Path to MsBuild
REM set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
set msbuild="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\msbuild.exe"
REM Path to ZipAlign
set zipalign="C:\Program Files (x86)\Android\android-sdk\build-tools\30.0.3\zipalign.exe"
REM Path to JarSigner
set jarsigner="C:\Program Files (x86)\Android\android-sdk\build-tools\30.0.3\apksigner.bat"
REM Path to APK file
set apkPath=..\IdApp\IdApp.Android\bin\%buildmode%\com.Tag.IdApp.aab

REM Path to signed and aligned APK file
set alignedApkPath=..\IdApp\IdApp.Android\bin\%buildmode%\com.Tag.IdApp_signed_aligned.aab

REM Jar Signer specifics
set jarsignerparams=sign --min-sdk-version 21 --ks %keystore% --ks-pass pass:%keystorepassword% %apkPath%
REM ZipAlign specifics
set zipalignparams=-f 4 %apkPath% %alignedApkPath%

echo.
echo ==============================
echo Compile Android.
echo ==============================
echo.
call %msbuild% ..\IdApp\IdApp.Android\IdApp.Android.csproj /p:Configuration=%buildmode% /t:Rebuild /t:PackageForAndroid /p:AndroidSupportedAbis="armeabi-v7a;x86;arm64-v8a;x86_64"

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Sign Android APK.
  echo ==============================
  echo.
  call %jarsigner% %jarsignerparams%
)

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Build completed successfully.
  echo ==============================
  echo.
)

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo ZipAlign Android APK.
  echo ==============================
  echo.
  call %zipalign% %zipalignparams%
)

if NOT %errorlevel%==0 (
  echo.
  echo ==============================
  echo Build failed.
  echo ==============================
  echo.
)

goto End

:MissingArgs
  echo.
  echo Missing arguments. This script requires two arguments.
  echo.
  echo   Usage:   BuildAndroid.bat /path/to/keystore keystorepassword
  echo.
  echo   Example: BuildAndroid.bat "C:\Users\JohnDoe\Documents\IdApp\TagDemo Cert\TagDemo Cert.keystore" P4ssw0rd
  echo.
 
:End


REM C:\Program Files\Microsoft\jdk-11.0.12.7-hotspot\bin\jarsigner.exe -keystore "D:\work\Keystore\Trust Anchor Group AB\Trust Anchor Group AB.keystore" -storepass qa0HYlEX8V5pAq5hVbTX9qiKz8pBv1omk7PQa_8ttbk -keypass Password -digestalg SHA-256 -sigalg SHA256withRSA -signedjar bin\Release\com.tag.IdApp-Signed.aab obj\Release\110\android\bin\com.tag.IdApp.aab  "TAGDemo Cert"

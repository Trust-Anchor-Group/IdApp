echo off

if %1.==. goto MissingArgs
if %2.==. goto MissingArgs

REM Debug or Release?
set buildmode=Debug

set keystore=%1
set keystorepassword=%2

REM Path to MsBuild
set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
REM Path to VsTest
set vstest="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
REM Path to ZipAlign
set zipalign="C:\Program Files (x86)\Android\android-sdk\build-tools\30.0.2\zipalign.exe"
REM Path to JarSigner
set jarsigner="C:\Program Files (x86)\Android\android-sdk\build-tools\30.0.2\apksigner.bat"
REM Path to APK file
set apkPath=..\IdApp\IdApp.Android\bin\%buildmode%\com.Tag.IdApp.apk

REM Path to signed and aligned APK file
set alignedApkPath=..\IdApp\IdApp.Android\bin\%buildmode%\com.Tag.IdApp_signed_aligned.apk

REM all unit test dll's in a space separated list
set vstestparams=..\IdApp.Tests\bin\%buildmode%\netcoreapp3.1\IdApp.Tests.dll ..\Tag.Neuron.Xamarin.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.Tests.dll ..\Tag.Neuron.Xamarin.UI.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.UI.Tests.dll
REM Jar Signer specifics
set jarsignerparams=sign --ks %keystore% --ks-pass pass:%keystorepassword% %apkPath%
REM ZipAlign specifics
set zipalignparams=-f 4 %apkPath% %alignedApkPath%

echo.
echo ==============================
echo Compile Solution.
echo ==============================
echo.
call %msbuild% ..\IdApp.sln /property:Configuration=%buildmode% /t:Rebuild

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Run Unit Tests.
  echo ==============================
  echo.
  call %vstest% %vstestparams%
)

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Build Android APK
  echo ==============================
  echo.
  call %msbuild% ..\IdApp\IdApp.Android\IdApp.Android.csproj /property:Configuration=%buildmode% /t:PackageForAndroid /p:AndroidSupportedAbis="armeabi-v7a;x86;arm64-v8a;x86_64"
)

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
  echo ZipAlign Android APK.
  echo ==============================
  echo.
  call %zipalign% %zipalignparams%
)

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Build completed successfully.
  echo ==============================
  echo.
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
  echo   Usage: build.bat "/path/to/keystore" MySecretPassword
  echo.
 
:End

echo off

if %1.==. goto MissingArgs
if %2.==. goto MissingArgs
if %3.==. goto MissingArgs

REM Debug or Release?
set buildmode=Debug

REM Path to MsBuild
set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
REM Path to VsTest
set vstest="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

REM all unit test dll's in a space separated list
set vstestparams=..\IdApp.Tests\bin\%buildmode%\netcoreapp3.1\IdApp.Tests.dll ..\Tag.Neuron.Xamarin.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.Tests.dll ..\Tag.Neuron.Xamarin.UI.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.UI.Tests.dll

echo.
echo ==============================
echo Compile iOS
echo ==============================
echo.
call %msbuild% ..\IdApp\IdApp.iOS\IdApp.iOS.csproj /p:Configuration=%buildmode% /t:Rebuild /p:Platform="iPhone" /p:IpaPackageDir="..\IdApp\IdApp.iOS\bin\%buildmode%" /p:ServerAddress=%1 /p:ServerUser=%2 /p:ServerPassword=%3

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
  echo Missing arguments. This script requires three arguments.
  echo.
  echo   Usage:   BuildIPhone.bat serveraddress serveruser serverpassword
  echo.
  echo   Example: BuildIPhone.bat MyMac.local johndoe topsecret
  echo.
 
:End

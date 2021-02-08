set buildmode=Debug
set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
set vstest="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

REM build solution
call %msbuild% ..\IdApp.sln /property:Configuration=%buildmode% /t:Rebuild

REM run unit tests
if %errorlevel%==0 (
  set vstestparams=..\IdApp.Tests\bin\%buildmode%\netcoreapp3.1\IdApp.Tests.dll ..\Tag.Neuron.Xamarin.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.Tests.dll ..\Tag.Neuron.Xamarin.UI.Tests\bin\%buildmode%\netcoreapp3.1\Tag.Neuron.Xamarin.UI.Tests.dll
  call %vstest% %vstestparams%
)





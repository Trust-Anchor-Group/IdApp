# Version and name
Set-Variable -Name packageName -Value "[[your.package.name or new one]]"
Set-Variable -Name versionCode -Value 10
Set-Variable -Name versionName -Value "1.2.3"

# Tools
Set-Variable -Name msbuild -Value "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
Set-Variable -Name zipalign -Value "C:\Program Files (x86)\Android\android-sdk\build-tools\28.0.3\zipalign.exe"
Set-Variable -Name jarsigner -Value "C:\Program Files (x86)\Android\android-sdk\build-tools\28.0.3\apksigner.bat"

# Build config
Set-Variable -Name buildManifest -Value "Properties/AndroidManifest.xml"
Set-Variable -Name androidProjectFolder -Value "C:\Git\[[Path to project]].Droid"
Set-Variable -Name androidProject -Value "$($androidProjectFolder)\\[[Project]].Mobile.Droid.csproj"
Set-Variable -Name currentDate -Value Get-Date -Format "yyyyMMddHHmmss"
Set-Variable -Name outputPath -Value "C:\temp\$($currentDate)"

# var abis = new string[] { "armeabi", "armeabi-v7a", "x86", "arm64-v8a", "x86_64" };

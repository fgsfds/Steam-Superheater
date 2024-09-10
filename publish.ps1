dotnet publish ".\src\Avalonia.Desktop\Avalonia.Desktop.csproj" -p:PublishProfile=Windows
dotnet publish ".\src\Avalonia.Desktop\Avalonia.Desktop.csproj" -p:PublishProfile=Linux

$version = (Get-Item .\publish\Superheater.exe).VersionInfo.FileVersion
$version = $version.Substring(0,$version.Length-2)
$version = $version.Replace('.','')

Compress-Archive -Path ".\publish\Superheater.exe" -DestinationPath ".\publish\superheater_${version}_win-x64.zip"
Compress-Archive -Path ".\publish\Superheater" -DestinationPath ".\publish\superheater_${version}_linux-x64.zip"


rem Batch file to test nuget package locally

cd /d %~dp0

mkdir .\Out\NuGet\Release

.\Libs\NugetPackages\NuGet.CommandLine.3.4.3\tools\nuget.exe pack .\.nuget\infrastructure.nuspec ^
 -OutputDirectory .\Out\NuGet\Release  ^
 -Properties Configuration=Release  ^
 -Properties Version=0.1.5.0  ^
 -Properties "Author=Simple Answers Energy, Inc."  ^
 -Properties "Owner=Simple Answers Energy, Inc."  ^
 -Properties "NuGetCopyright=Copyright (c) 2016 Simple Answers Energy, Inc."  ^
 -Properties RunOut=Out\Run\Release  ^
 -Properties GitHubUrl=https://github.com/SAEnergy/Infrastructure  ^
 -Properties LicenseUrl=https://github.com/SAEnergy/Infrastructure/blob/master/LICENSE.md  ^
 -Properties NugetIconUrl=https://raw.githubusercontent.com/SAEnergy/Infrastructure/master/.nuget/NuGet.ico  ^
 -IncludeReferencedProjects

pause

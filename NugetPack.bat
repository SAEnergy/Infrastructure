
rem Batch file to test nuget package locally


rem Build NugetPackage

cd /d %~dp0

mkdir .\Out\NuGet\Local

.\Libs\NugetPackages\NuGet.CommandLine.3.4.3\tools\nuget.exe pack .\.nuget\infrastructure.nuspec ^
 -OutputDirectory .\Out\NuGet\Local  ^
 -Properties Configuration=Release  ^
 -Properties Version=9.9.9.9  ^
 -Properties "Author=Simple Answers Energy, Inc."  ^
 -Properties "Owner=Simple Answers Energy, Inc."  ^
 -Properties "NuGetCopyright=Copyright (c) 2016 Simple Answers Energy, Inc."  ^
 -Properties RunOut=Out\Run\Debug  ^
 -Properties GitHubUrl=https://github.com/SAEnergy/Infrastructure  ^
 -Properties LicenseUrl=https://github.com/SAEnergy/Infrastructure/blob/master/LICENSE.md  ^
 -Properties NugetIconUrl=https://raw.githubusercontent.com/SAEnergy/Infrastructure/master/.nuget/NuGet.ico  ^
 -Verbosity Detailed ^
 -IncludeReferencedProjects

rem Delete old packages

for /d %%G in ("..\Superstructure\Libs\NugetPackages\sae*") do rmdir /s /q "%%G"

rem Restore packages in superstructure

.\Libs\NugetPackages\NuGet.CommandLine.3.4.3\tools\nuget.exe  restore ..\Superstructure\Superstructure.sln


pause
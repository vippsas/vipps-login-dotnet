@echo off
set outputDir=%~dp0

if not "%1"=="" (
  set outputDir=%1
)

set version=1.0.0.0
nuget pack src\Vipps.Login\Vipps.Login.csproj -IncludeReferencedProjects -Version %version%
nuget pack src\Vipps.Login.Episerver\Vipps.Login.Episerver.csproj -IncludeReferencedProjects -Version %version%
nuget pack src\Vipps.Login.Episerver.Commerce\Vipps.Login.Episerver.Commerce.csproj -IncludeReferencedProjects -Version %version%

@echo on
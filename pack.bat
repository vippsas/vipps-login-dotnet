@echo off
set outputDir=%~dp0

if not "%1"=="" (
  set outputDir=%1
)

nuget pack src\Vipps.Login\Vipps.Login.csproj -IncludeReferencedProjects -Version 1.0.0.0
nuget pack src\Vipps.Login.Episerver\Vipps.Login.Episerver.csproj -IncludeReferencedProjects -Version 1.0.0.0

@echo on
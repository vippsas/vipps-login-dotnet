@echo off
set outputDir=%~dp0

if not "%1"=="" (
  set outputDir=%1
)

nuget pack src\VippsLogin\Epi.VippsLogin.csproj -IncludeReferencedProjects -Version 1.0.0.8

@echo on
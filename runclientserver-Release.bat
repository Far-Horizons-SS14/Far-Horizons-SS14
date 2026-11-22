@echo off
dotnet build Content.Client -p:NoWarn="NU1902%%3BNU1903" --configuration Release
dotnet build Content.Server -p:NoWarn="NU1902%%3BNU1903" --configuration Release

Start "Client" "runclient-Release.bat"
Start "Server" "runserver-Release.bat"

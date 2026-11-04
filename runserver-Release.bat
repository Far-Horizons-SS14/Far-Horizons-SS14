@echo off
dotnet run -p:NoWarn="NU1902%%3BNU1903" --project Content.Server --configuration Release
pause

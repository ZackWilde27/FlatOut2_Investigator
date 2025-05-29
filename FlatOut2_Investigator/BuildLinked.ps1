# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/FlatOut2_Investigator/*" -Force -Recurse
dotnet publish "./FlatOut2_Investigator.csproj" -c Release -o "$env:RELOADEDIIMODS/FlatOut2_Investigator" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
@echo off

:Start

set /p "version=TYPE IN VERSION NUMBER: "

IF "%version%" == "" GOTO Start

mkdir publish

echo Deleting old releases...
del /q publish\*



echo Publishing framework dependent version...
dotnet publish .\ConfigManager\ConfigManager.csproj /p:PublishProfile=ConfigManager\Properties\PublishProfiles\FrameworkDependentProfile.pubxml

echo Removing PDBs...
del ConfigManager\bin\Release\net7.0-windows\publish\CsgoConfigManager\*.pdb

echo Zipping folder...
7z a CsgoConfigManager.zip .\ConfigManager\bin\Release\net7.0-windows\publish\CsgoConfigManager

echo Moving and renaming file...
move /y "CsgoConfigManager.zip" "publish\csgo_config_manager_FD_%version%.zip"



echo Publishing standalone (self-contained) version...
dotnet publish .\ConfigManager\ConfigManager.csproj /p:PublishProfile=ConfigManager\Properties\PublishProfiles\SelfContainedProfile.pubxml

echo Removing PDBs...
del ConfigManager\bin\Release\net7.0-windows\publish\CsgoConfigManager\*.pdb

echo Zipping folder...
7z a CsgoConfigManager.zip .\ConfigManager\bin\Release\net7.0-windows\publish\CsgoConfigManager

echo Moving and renaming file...
move /y "CsgoConfigManager.zip" "publish\csgo_config_manager_SC_%version%.zip"



pause
@echo off

:Start

set /p "version=TYPE IN VERSION NUMBER: "

IF "%version%" == "" GOTO Start

mkdir publish

echo Deleting old releases...
del /q publish\*



echo Publishing framework dependent version...
dotnet publish .\DamageCalculator\DamageCalculator.csproj /p:PublishProfile=DamageCalculator\Properties\PublishProfiles\FrameworkDependentProfile.pubxml

echo Removing PDBs...
del DamageCalculator\bin\Release\net7.0-windows\publish\DamageCalculator\*.pdb

echo Zipping folder...
7z a DamageCalculator.zip .\DamageCalculator\bin\Release\net7.0-windows\publish\DamageCalculator

echo Moving and renaming file...
move /y "DamageCalculator.zip" "publish\csgo_damage_calculator_FD_x64_%version%.zip"



echo Publishing standalone (self-contained) version...
dotnet publish .\DamageCalculator\DamageCalculator.csproj /p:PublishProfile=DamageCalculator\Properties\PublishProfiles\SelfContainedProfile.pubxml

echo Removing PDBs...
del DamageCalculator\bin\Release\net7.0-windows\publish\DamageCalculator\*.pdb

echo Zipping folder...
7z a DamageCalculator.zip .\DamageCalculator\bin\Release\net7.0-windows\publish\DamageCalculator

echo Moving and renaming file...
move /y "DamageCalculator.zip" "publish\csgo_damage_calculator_SC_x64_%version%.zip"



pause
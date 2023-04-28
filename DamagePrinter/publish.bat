@echo off

:Start

set /p "version=TYPE IN VERSION NUMBER: "

IF "%version%" == "" GOTO Start

mkdir publish

echo Deleting old releases...
del /q publish\*



echo Publishing framework dependent version...
dotnet publish .\DamagePrinterGUI\DamagePrinterGUI.csproj /p:PublishProfile=DamagePrinterGUI\Properties\PublishProfiles\FrameworkDependentProfile.pubxml

echo Removing PDBs...
del DamagePrinterGUI\bin\Release\net7.0-windows\publish\DamagePrinterGUI\*.pdb

echo Zipping folder...
7z a DamagePrinterGUI.zip .\DamagePrinterGUI\bin\Release\net7.0-windows\publish\DamagePrinterGUI

echo Moving and renaming file...
move /y "DamagePrinterGUI.zip" "publish\csgo_damage_printer_gui_FD_x64_%version%.zip"



echo Publishing standalone (self-contained) version...
dotnet publish .\DamagePrinterGUI\DamagePrinterGUI.csproj /p:PublishProfile=DamagePrinterGUI\Properties\PublishProfiles\SelfContainedProfile.pubxml

echo Removing PDBs...
del DamagePrinterGUI\bin\Release\net7.0-windows\publish\DamagePrinterGUI\*.pdb

echo Zipping folder...
7z a DamagePrinterGUI.zip .\DamagePrinterGUI\bin\Release\net7.0-windows\publish\DamagePrinterGUI

echo Moving and renaming file...
move /y "DamagePrinterGUI.zip" "publish\csgo_damage_printer_gui_SC_x64_%version%.zip"



pause
# CSGO-Projects

## Damage Calculator
* Maybe add a path-drawing mode additionally to Bomb and Shooting mode
* MainWindow code and structure is ugly and bloated, I tried refactoring and I noticed it would take days and I am gonna let it be and hope I'll do it better next time from the start

## Damage Printer
* Needs a recode because of fugly code, probably in WPF (Done more or less)
* Also remove the extra features outside of damage printing (Done)
* Add automatic autoexec creation or extension for the con_logfile command specifically (Done I think)

## Damage Printer GUI (Partial Recode, all of these points should be done)
* Use console output file from autoexec (con_logfile) (only if .log extension), otherwise put it in, or create the file, if nonexistent
* Support closing and opening the game while the program is open
* Save selected options in Documents. Settings might be:
- Minimum damage to be printed
- Also print killed players
- Withhold double console output
- Print amount of shots used
- Print more specific terms
- Only print when I die
- Print in in-game chat
- Only print for team

## Config-Manager V2 (Maybe switch from WPF to Avalonia UI, not going to recode anything before CS2)
* ToDo list in Microsoft ToDo
* Recode of the original manager with extra features, maybe a protocol for GameBanana's one-click integration

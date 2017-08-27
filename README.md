# SplasherTAS
Simple TAS Tools for the game Splasher

## Installation
- Go to [Releases](https://github.com/ShootMe/SplasherTAS/releases)
- Download Assembly-CSharp.dll, Assembly-CSharp-Addons.dll, J2i.Net.XInputWrapper.dll, UnityEngine.dll, and XInputInterface.dll
- Place those in your Splasher game data directory (usually C:\Program Files (x86)\Steam\steamapps\common\Splasher\Splasher_Data\Managed\)
- Make sure to back up the original Assembly-CSharp.dll and UnityEngine.dll before copying. (Can rename them .bak or something)

## Input File
Input file is called Splasher.tas and needs to be in the main Splasher directory (usually C:\Program Files (x86)\Steam\steamapps\common\Splasher\Splasher.tas)

Format for the input file is (Frames),(Actions)

ie) 123,R,J (For 123 frames, hold Right and Jump)

## Actions Available
- R = Right
- L = Left
- U = Up
- D = Down
- J = Jump
- W = Shoot Water
- G = Shoot Goo
- B = Shoot Bouncy
- S = Start
- X = Select
- [ = Left Bumper
- ] = Right Bumper

## Splasher Studio
Can be used in stead of notepad or similar for easier editing of the TAS file. Is located in [Releases](https://github.com/ShootMe/SplasherTAS/releases) as well.

If Splasher.exe is running it will automatically open Splasher.tas if it exists. You can hit Ctrl+O to open a different file, which will automatically save it to Splasher.tas as well. Ctrl+Shift+S will open a Save As dialog as well.

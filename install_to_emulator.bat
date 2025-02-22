@echo off
echo Installing to emulator...
adb install -r "%~dp0\Build\MyGame.apk"
pause 
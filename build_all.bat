@echo off
echo Beginning AssetValidationService build
call C:\Users\Administrator\Documents\ecosim\src\services\AssetValidationServiceV2\build.bat
echo Beginning Roblox.Website build
call C:\Users\Administrator\Documents\ecosim\src\services\Roblox\Roblox.Website\constructwebsite.bat
echo Beginning Roblox2016Site build
call C:\Users\Administrator\Documents\ecosim\src\services\2016-roblox-main\build.bat
echo OK

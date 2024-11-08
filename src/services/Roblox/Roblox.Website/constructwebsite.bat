@echo off
cd C:\Users\Administrator\Documents\ecosim\src\services\Roblox\Roblox.Website
mkdir C:\ecosim_builds\
mkdir C:\ecosim_builds\Roblox.Website\
copy PublicKeyBlob.txt C:\ecosim_builds\Roblox.Website\
copy PrivateKeyBlob.txt C:\ecosim_builds\Roblox.Website\
copy PrivateKey.pem C:\ecosim_builds\Roblox.Website\
copy appsettings.json C:\ecosim_builds\Roblox.Website\
dotnet publish -c=Release -o=C:\ecosim_builds\Roblox.Website\ -v=n
echo POLISHING BUILD
cd C:\ecosim_builds\Roblox.Website
del /S *.pdb
del web.config
echo DONE
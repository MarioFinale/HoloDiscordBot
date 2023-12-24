dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true /p:PublishSelfContained=true -o ./bin/windows/
cd ..
pause
dotnet publish -c Release -r linux-x64 --self-contained /p:PublishSingleFile=true /p:PublishSelfContained=true -o ./bin/unix/
cd ..
pause
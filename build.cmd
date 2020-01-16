
set version=%1
set key=%2

cd %~dp0

dotnet build magic.lambda.http/magic.lambda.http.csproj --configuration Release --source https://api.nuget.org/v3/index.json
dotnet nuget push magic.lambda.http/bin/Release/magic.lambda.http.%version%.nupkg -k %key% -s https://api.nuget.org/v3/index.json

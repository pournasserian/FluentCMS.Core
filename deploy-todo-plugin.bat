@echo off
echo Building Todo API Plugin...

rem Create plugins directory if it doesn't exist
if not exist "plugins" mkdir plugins

rem Build the TodoApi project
dotnet build src/FluentCMS.TodoApi/FluentCMS.TodoApi.csproj -c Release

rem Copy the build output to the plugins directory
copy "src\FluentCMS.TodoApi\bin\Release\net9.0\FluentCMS.TodoApi.dll" "plugins\"

echo Deployment completed. Plugin is now available in the 'plugins' directory.

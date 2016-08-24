@ECHO OFF
dotnet restore %~dp0..\
pushd ..
tools\nuget.exe restore "src\Microsoft.Extensions.Diagnostics.FilterParserGenerator\Microsoft.Extensions.Diagnostics.FilterParserGenerator.csproj" -PackagesDirectory packages
popd
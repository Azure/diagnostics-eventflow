@ECHO OFF
dotnet restore %~dp0..\
pushd %~dp0..
tools\nuget.exe restore "src\Microsoft.Diagnostics.EventFlow.FilterParserGenerator\Microsoft.Diagnostics.EventFlow.FilterParserGenerator.csproj" -PackagesDirectory packages
popd
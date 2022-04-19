@ECHO OFF
dotnet restore %~dp0..\
pushd %~dp0..
tools\nuget.exe restore "src\Microsoft.Diagnostics.EventFlow.Signing\Microsoft.Diagnostics.EventFlow.Signing.csproj" -PackagesDirectory packages
tools\nuget.exe restore "src\Microsoft.Diagnostics.EventFlow.NugetSigning\Microsoft.Diagnostics.EventFlow.NugetSigning.csproj" -PackagesDirectory packages
popd

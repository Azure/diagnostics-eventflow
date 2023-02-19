@ECHO OFF
dotnet restore --force %~dp0..\Warsaw.sln
pushd %~dp0..
tools\nuget.exe restore "src\Microsoft.Diagnostics.EventFlow.Signing\Microsoft.Diagnostics.EventFlow.Signing.csproj" -PackagesDirectory packages
tools\nuget.exe restore "src\Microsoft.Diagnostics.EventFlow.NugetSigning\Microsoft.Diagnostics.EventFlow.NugetSigning.csproj" -PackagesDirectory packages
popd

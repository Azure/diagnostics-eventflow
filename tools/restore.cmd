@ECHO OFF
dotnet restore --force %~dp0..\
pushd %~dp0..
dotnet restore "src\Microsoft.Diagnostics.EventFlow.Signing\Microsoft.Diagnostics.EventFlow.Signing.csproj" --packages packages
dotnet restore "src\Microsoft.Diagnostics.EventFlow.NugetSigning\Microsoft.Diagnostics.EventFlow.NugetSigning.csproj" --packages packages
popd

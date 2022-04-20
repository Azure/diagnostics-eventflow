@ECHO OFF
pushd %~dp0..
dotnet build /m /p:Configuration=Release /binaryLogger /verbosity:minimal
popd

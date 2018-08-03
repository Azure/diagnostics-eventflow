@ECHO OFF
pushd %~dp0..
msbuild /m /p:Configuration=Release /binaryLogger /verbosity:minimal
popd

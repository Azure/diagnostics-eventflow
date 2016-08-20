@ECHO OFF
pushd %~dp0..
msbuild /m /p:Configuration=Release /fl
popd
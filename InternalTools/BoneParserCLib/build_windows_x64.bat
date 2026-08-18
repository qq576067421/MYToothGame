@echo off
setlocal
pushd "%~dp0"
call "d:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat"
if errorlevel 1 exit /b 1
cmake --preset windows-x64-msvc-release
if errorlevel 1 exit /b 1
cmake --build --preset windows-x64-release
if errorlevel 1 exit /b 1
popd
endlocal

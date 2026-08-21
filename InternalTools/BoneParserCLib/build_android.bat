@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0"

rem ===== 默认构建参数 =====
set "DEFAULT_ABI=arm64-v8a"
set "DEFAULT_ANDROID_PLATFORM=android-30"
set "CMAKE_EXE=cmake"
set "CMAKE_GENERATOR=Ninja"
set "STRIP_ANDROID_UNITY_SO=1"
set "UNITY_ANDROID_LIBS_DIR=..\..\Project\ToothGameProject\Assets\Plugins\Android\libs"

rem ===== 本机路径配置，换电脑时优先修改这里 =====
set "LOCAL_ANDROID_NDK_ROOT="
set "LOCAL_ANDROID_STRIP_EXE="
set "LOCAL_ANDROID_NDK_PARENT_DIR=D:\Android\Sdk\ndk"
set "LOCAL_USER_ANDROID_NDK_PARENT_DIR=%LOCALAPPDATA%\Android\Sdk\ndk"
set "LOCAL_UNITY_HUB_EDITOR_DIR=C:\Program Files\Unity\Hub\Editor"

set "TARGET_ABI=%~1"
if "%TARGET_ABI%"=="" set "TARGET_ABI=%DEFAULT_ABI%"

set "ANDROID_PLATFORM=%~2"
if "%ANDROID_PLATFORM%"=="" set "ANDROID_PLATFORM=%DEFAULT_ANDROID_PLATFORM%"

for %%U in ("%UNITY_ANDROID_LIBS_DIR%") do set "UNITY_ANDROID_LIBS_DIR_FULL=%%~fU"

call :ResolveNdk
if errorlevel 1 goto fail

call :ResolveStrip
if errorlevel 1 goto fail

if /I "%TARGET_ABI%"=="all" (
    call :BuildAbi arm64-v8a
    if errorlevel 1 goto fail
    goto ok
)

call :BuildAbi "%TARGET_ABI%"
if errorlevel 1 goto fail
goto ok

:ResolveNdk
set "BONE_PARSER_CLIB_ANDROID_NDK="
call :TryNdk "%LOCAL_ANDROID_NDK_ROOT%"
call :TryNdk "%ANDROID_NDK_HOME%"
call :TryNdk "%ANDROID_NDK_ROOT%"
call :TryNdk "%UNITY_ANDROID_NDK_ROOT%"

if not defined BONE_PARSER_CLIB_ANDROID_NDK (
    call :TryLatestNdkUnder "%LOCAL_ANDROID_NDK_PARENT_DIR%"
)

if not defined BONE_PARSER_CLIB_ANDROID_NDK (
    call :TryLatestNdkUnder "%LOCAL_USER_ANDROID_NDK_PARENT_DIR%"
)

if not defined BONE_PARSER_CLIB_ANDROID_NDK (
    call :TryUnityHubNdk "%LOCAL_UNITY_HUB_EDITOR_DIR%"
)

if not defined BONE_PARSER_CLIB_ANDROID_NDK (
    echo Android NDK was not found. Set ANDROID_NDK_HOME, ANDROID_NDK_ROOT, or UNITY_ANDROID_NDK_ROOT.
    exit /b 1
)

set "ANDROID_NDK_HOME=%BONE_PARSER_CLIB_ANDROID_NDK%"
echo Android NDK: %BONE_PARSER_CLIB_ANDROID_NDK%
exit /b 0

:ResolveStrip
set "BONE_PARSER_CLIB_ANDROID_STRIP="
if /I not "%STRIP_ANDROID_UNITY_SO%"=="1" exit /b 0

call :TryStrip "%LOCAL_ANDROID_STRIP_EXE%"
call :TryStrip "%BONE_PARSER_CLIB_ANDROID_NDK%\toolchains\llvm\prebuilt\windows-x86_64\bin\llvm-strip.exe"

if not defined BONE_PARSER_CLIB_ANDROID_STRIP (
    echo Android llvm-strip was not found. Set LOCAL_ANDROID_STRIP_EXE near the top of this script.
    exit /b 1
)

echo Android strip: %BONE_PARSER_CLIB_ANDROID_STRIP%
exit /b 0

:TryNdk
if "%~1"=="" exit /b 0
if exist "%~1\build\cmake\android.toolchain.cmake" (
    set "BONE_PARSER_CLIB_ANDROID_NDK=%~1"
)
exit /b 0

:TryStrip
if "%~1"=="" exit /b 0
if exist "%~1" (
    set "BONE_PARSER_CLIB_ANDROID_STRIP=%~1"
)
exit /b 0

:TryLatestNdkUnder
if "%~1"=="" exit /b 0
if not exist "%~1" exit /b 0
for /f "delims=" %%D in ('dir /b /ad /o-n "%~1" 2^>nul') do (
    if not defined BONE_PARSER_CLIB_ANDROID_NDK (
        call :TryNdk "%~1\%%D"
    )
)
exit /b 0

:TryUnityHubNdk
if "%~1"=="" exit /b 0
if not exist "%~1" exit /b 0
for /f "delims=" %%F in ('dir /s /b "%~1\*\Editor\Data\PlaybackEngines\AndroidPlayer\NDK\build\cmake\android.toolchain.cmake" 2^>nul') do (
    if not defined BONE_PARSER_CLIB_ANDROID_NDK (
        for %%N in ("%%~dpF..\..") do set "BONE_PARSER_CLIB_ANDROID_NDK=%%~fN"
    )
)
exit /b 0

:BuildAbi
set "ABI=%~1"
set "BUILD_DIR=build\android-!ABI!-release"
echo Building Android ABI: !ABI!
"%CMAKE_EXE%" -S . -B "!BUILD_DIR!" -G "%CMAKE_GENERATOR%" ^
    -DCMAKE_TOOLCHAIN_FILE="!BONE_PARSER_CLIB_ANDROID_NDK!\build\cmake\android.toolchain.cmake" ^
    -DANDROID_ABI="!ABI!" ^
    -DANDROID_PLATFORM="%ANDROID_PLATFORM%" ^
    -DANDROID_STL=c++_static ^
    -DCMAKE_BUILD_TYPE=Release ^
    -DBONE_PARSER_CLIB_UNITY_ANDROID_LIBS_DIR="!UNITY_ANDROID_LIBS_DIR_FULL!" ^
    -DBONE_PARSER_CLIB_COPY_TO_UNITY=ON
if errorlevel 1 exit /b 1
"%CMAKE_EXE%" --build "!BUILD_DIR!"
if errorlevel 1 exit /b 1
if /I "%STRIP_ANDROID_UNITY_SO%"=="1" (
    call :StripUnityAndroidPlugin "!ABI!"
    if errorlevel 1 exit /b 1
)
exit /b 0

:StripUnityAndroidPlugin
set "ABI=%~1"
set "UNITY_PLUGIN_SO=%UNITY_ANDROID_LIBS_DIR_FULL%\%ABI%\libBoneParserCLib.so"
if not exist "%UNITY_PLUGIN_SO%" (
    echo Unity Android plugin was not found: %UNITY_PLUGIN_SO%
    exit /b 1
)
"%BONE_PARSER_CLIB_ANDROID_STRIP%" --strip-unneeded "%UNITY_PLUGIN_SO%"
if errorlevel 1 exit /b 1
for %%F in ("%UNITY_PLUGIN_SO%") do echo Stripped Unity Android plugin: %%~fF ^(%%~zF bytes^)
exit /b 0

:ok
popd
endlocal
exit /b 0

:fail
popd
endlocal
exit /b 1

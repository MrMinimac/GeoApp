@echo off
setlocal

REM ============================================================
REM Configuration
REM ============================================================

set PLUGIN_PROJECT=GeoCadPlugin
set APP_PROJECT=GeoAppWpf

set CONFIGURATION=Release
set TARGET=net8.0-windows
set RUNTIME=win-x64

set PLUGIN_BIN=%PLUGIN_PROJECT%\bin\%CONFIGURATION%\%TARGET%
set APP_OUTPUT=%APP_PROJECT%\publish
set OUTPUT=dist


REM ============================================================
REM Clean output
REM ============================================================

echo.
echo ========================================
echo Cleaning output
echo ========================================

if exist "%OUTPUT%" (
    rmdir /S /Q "%OUTPUT%"
)

if exist "%APP_OUTPUT%" (
    rmdir /S /Q "%APP_OUTPUT%"
)

mkdir "%OUTPUT%"


REM ============================================================
REM Build GeoCadPlugin
REM ============================================================

echo.
echo ========================================
echo Building %PLUGIN_PROJECT%
echo ========================================

dotnet build "%PLUGIN_PROJECT%\%PLUGIN_PROJECT%.csproj" ^
    -c %CONFIGURATION%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo PLUGIN BUILD FAILED!
    echo ========================================
    pause
    exit /b %ERRORLEVEL%
)


REM ============================================================
REM Merge GeoCadPlugin dependencies
REM ============================================================

echo.
echo ========================================
echo Merging %PLUGIN_PROJECT%
echo ========================================

ilrepack ^
    /out:"%OUTPUT%\GeoCadPlugin.dll" ^
    /lib:"%PLUGIN_BIN%" ^
    "%PLUGIN_BIN%\GeoCadPlugin.dll" ^
    "%PLUGIN_BIN%\BitMiracle.LibTiff.NET.dll" ^
    "%PLUGIN_BIN%\Clipper2Lib.dll" ^
    "%PLUGIN_BIN%\EPPlus.dll" ^
    "%PLUGIN_BIN%\EPPlus.Interfaces.dll" ^
    "%PLUGIN_BIN%\GeoAppCore.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.Configuration.Abstractions.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.Configuration.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.Configuration.FileExtensions.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.Configuration.Json.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.FileProviders.Abstractions.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.FileProviders.Physical.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.FileSystemGlobbing.dll" ^
    "%PLUGIN_BIN%\Microsoft.Extensions.Primitives.dll" ^
    "%PLUGIN_BIN%\Microsoft.IO.RecyclableMemoryStream.dll" ^
    "%PLUGIN_BIN%\Newtonsoft.Json.dll" ^
    "%PLUGIN_BIN%\System.Security.Cryptography.Pkcs.dll" ^
    "%PLUGIN_BIN%\System.Security.Cryptography.Xml.dll" ^
    /internalize

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo PLUGIN MERGE FAILED!
    echo ========================================
    pause
    exit /b %ERRORLEVEL%
)


REM ============================================================
REM Publish GeoAppWpf
REM ============================================================

echo.
echo ========================================
echo Publishing %APP_PROJECT%
echo ========================================

dotnet publish "%APP_PROJECT%\%APP_PROJECT%.csproj" ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "%APP_OUTPUT%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo APPLICATION BUILD FAILED!
    echo ========================================
    pause
    exit /b %ERRORLEVEL%
)


REM ============================================================
REM Copy application EXE to dist
REM ============================================================

echo.
echo ========================================
echo Copying application
echo ========================================

copy /Y "%APP_OUTPUT%\%APP_PROJECT%.exe" "%OUTPUT%\%APP_PROJECT%.exe" >nul

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo FAILED TO COPY EXE!
    pause
    exit /b %ERRORLEVEL%
)


REM ============================================================
REM Done
REM ============================================================

echo.
echo ========================================
echo BUILD SUCCESS
echo ========================================

echo.
echo Distribution:
echo.
echo %CD%\%OUTPUT%
echo.
echo Files:
echo   GeoAppWpf.exe
echo   GeoCadPlugin.dll
echo.

dir /B "%OUTPUT%"

echo.
echo ========================================
echo DONE
echo ========================================

pause
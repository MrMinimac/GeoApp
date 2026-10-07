@echo off
setlocal

set PROJECT=GeoCadPlugin
set CONFIGURATION=Release
set TARGET=net8.0-windows

set BIN=%PROJECT%\bin\%CONFIGURATION%\%TARGET%
set OUTPUT=dist

echo ========================================
echo Building %PROJECT%
echo ========================================

dotnet build "%PROJECT%\%PROJECT%.csproj" -c %CONFIGURATION%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Cleaning output
echo ========================================

if exist "%OUTPUT%" (
    rmdir /S /Q "%OUTPUT%"
)

mkdir "%OUTPUT%"

echo.
echo ========================================
echo Merging assemblies
echo ========================================

ilrepack ^
    /out:"%OUTPUT%\GeoCadPlugin.dll" ^
    /lib:"%BIN%" ^
    "%BIN%\GeoCadPlugin.dll" ^
    "%BIN%\BitMiracle.LibTiff.NET.dll" ^
    "%BIN%\Clipper2Lib.dll" ^
    "%BIN%\EPPlus.dll" ^
    "%BIN%\EPPlus.Interfaces.dll" ^
    "%BIN%\GeoAppCore.dll" ^
    "%BIN%\Microsoft.Extensions.Configuration.Abstractions.dll" ^
    "%BIN%\Microsoft.Extensions.Configuration.dll" ^
    "%BIN%\Microsoft.Extensions.Configuration.FileExtensions.dll" ^
    "%BIN%\Microsoft.Extensions.Configuration.Json.dll" ^
    "%BIN%\Microsoft.Extensions.FileProviders.Abstractions.dll" ^
    "%BIN%\Microsoft.Extensions.FileProviders.Physical.dll" ^
    "%BIN%\Microsoft.Extensions.FileSystemGlobbing.dll" ^
    "%BIN%\Microsoft.Extensions.Primitives.dll" ^
    "%BIN%\Microsoft.IO.RecyclableMemoryStream.dll" ^
    "%BIN%\Newtonsoft.Json.dll" ^
    "%BIN%\System.Security.Cryptography.Pkcs.dll" ^
    "%BIN%\System.Security.Cryptography.Xml.dll" ^
    /internalize

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo MERGE FAILED!
    echo ========================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo BUILD SUCCESS
echo ========================================
echo.
echo Result:
echo %CD%\%OUTPUT%\GeoCadPlugin.dll
echo.
echo AutoCAD DLLs were NOT merged:
echo   acmgd.dll
echo   acdbmgd.dll
echo   accoremgd.dll
echo.

pause
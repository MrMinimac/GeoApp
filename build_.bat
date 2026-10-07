@echo off
setlocal

set PROJECT=GeoAppWpf
set CONFIGURATION=Release
set RUNTIME=win-x64
set OUTPUT=publish

echo ========================================
echo Building %PROJECT%
echo ========================================

dotnet publish "%PROJECT%\%PROJECT%.csproj" ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "%OUTPUT%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo BUILD SUCCESS
echo ========================================
echo Output:
echo %CD%\%OUTPUT%\%PROJECT%.exe
echo.

pause
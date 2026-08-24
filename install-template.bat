@echo off
SETLOCAL
cls

echo =======================================================
echo           SIDC .NET Template Installer
echo =======================================================
echo.
echo Installing SIDC.Templates locally from source...
echo.

:: Run the .NET install command targeting the current directory (.)
dotnet new install .

echo.
echo =======================================================
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Template installed successfully!
    echo You can now use: dotnet new sidcapi -n ZARI
) else (
    echo [ERROR] Something went wrong during the installation.
    echo Please ensure you have the .NET SDK installed.
)
echo =======================================================
echo.

pause
ENDLOCAL
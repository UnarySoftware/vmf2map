@echo off
setlocal

rem ---------------------------------------------------------------------------
rem Drag one or more .vmf files onto this file. Each one is converted in place,
rem producing a .map next to the original .vmf.
rem
rem Every face gets one of the two materials below, picked by which way it faces.
rem Edit them to suit the map.
rem ---------------------------------------------------------------------------

set "WALL=unary.core . materials/dev/dev_measuregeneric01"
set "FLOOR=unary.core . materials/dev/dev_measuregeneric01b"
set "ANGLE=45"

rem Prefer an exe sitting next to this script (a published build), otherwise fall
rem back to the usual dotnet build output.
set "EXE=%~dp0vmf2map.exe"
if not exist "%EXE%" set "EXE=%~dp0bin\Debug\net10.0\vmf2map.exe"
if not exist "%EXE%" set "EXE=%~dp0bin\Release\net10.0\vmf2map.exe"

if not exist "%EXE%" (
    echo Could not find vmf2map.exe.
    echo Build it first:  dotnet build "%~dp0vmf2map.csproj"
    echo.
    pause
    exit /b 1
)

if "%~1"=="" (
    echo Drag one or more .vmf files onto this file to convert them.
    echo.
    pause
    exit /b 1
)

for %%F in (%*) do (
    echo.
    "%EXE%" "%%~fF" --wall "%WALL%" --floor "%FLOOR%" --angle %ANGLE%
)

echo.
pause

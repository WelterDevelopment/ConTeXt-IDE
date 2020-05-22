@echo off

setlocal

set LMTXSERVER=lmtx.contextgarden.net,lmtx.pragma-ade.com,lmtx.pragma-ade.nl
set LMTXINSTANCE=install-lmtx
set LMTXEXTRAS=

set OWNPATH=%~dp0

chdir /d "%OWNPATH%"

set PLATFORM=mswin

if defined ProgramFiles(x86)            set PLATFORM=win64
if "%PROCESSOR_ARCHITECTURE%"=="AMD64"  set PLATFORM=win64
if exist "%OWNPATH%tex\texmf-mswin\bin" set PLATFORM=mswin
if exist "%OWNPATH%tex\texmf-win64\bin" set PLATFORM=win64

set PATH=%OWNPATH%tex\texmf-%PLATFORM%\bin;%PATH%

"%OWNPATH%bin\mtxrun" --script "%OWNPATH%/bin/mtx-install.lua" --update --server=%LMTXSERVER% --instance=%LMTXINSTANCE% --extras="%LMTXEXTRAS%"

echo.
echo.
echo Updating installer:
echo.

copy /Y "%OWNPATH%tex\texmf-%PLATFORM%\bin\mtxrun.exe"                   "%OWNPATH%\bin\mtxrun.exe"
copy /Y "%OWNPATH%tex\texmf-context\scripts\context\lua\mtxrun.lua"      "%OWNPATH%\bin\mtxrun.lua"
copy /Y "%OWNPATH%tex\texmf-context\scripts\context\lua\mtx-install.lua" "%OWNPATH%\bin\mtx-install.lua"

echo.
echo When you want to use context, you need to initialize the tree with:
echo.
echo   %OWNPATH%setpath.bat
echo.
echo You can associate this command with a shortcut to the cmd prompt. Alternatively
echo you can add
echo.
echo   %OWNPATH%tex\texmf-%PLATFORM%\bin
echo.
echo to your PATH variable. If you run from an editor you can specify the full path
echo to mtxrun.exe:
echo.
echo.  %OWNPATH%tex\texmf-%PLATFORM%\bin\mtxrun.exe --autogenerate --script context --autopdf ...
echo.
echo The following settings were used:
echo.
echo   server   : %LMTXSERVER%
echo   instance : %LMTXINSTANCE%
echo   extras   : %LMTXEXTRAS%
echo   ownpath  : %OWNPATH%
echo   platform : %PLATFORM%
echo.

endlocal

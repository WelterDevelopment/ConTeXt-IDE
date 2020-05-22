@echo off

set OWNPATH=%~dp0

set PLATFORM=mswin

if defined ProgramFiles(x86)                    set PLATFORM=win64
if "%PROCESSOR_ARCHITECTURE%"=="AMD64"          set PLATFORM=win64
if exist "%OWNPATH%tex\texmf-mswin\bin\context" set PLATFORM=mswin
if exist "%OWNPATH%tex\texmf-win64\bin\context" set PLATFORM=win64

set PATH=%OWNPATH%tex\texmf-%PLATFORM%\bin;%PATH%

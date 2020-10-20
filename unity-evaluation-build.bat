REM The Unity Editor must be closed for this script to work

set UNITY_EDITOR_LOCATION=C:\Program Files\Unity\Hub\Editor\2018.4.12f1\Editor\Unity.exe
set UNITY_PROJECT_PATH=.\

REM Linux Build
set LINUX_TARGET_PATH=.\build\linux
set LINUX_TARGET_NAME=evaluation.x86_64

del /s /q "%LINUX_TARGET_PATH%\*.*"
"%UNITY_EDITOR_LOCATION%" -quit -batchmode -projectPath "%UNITY_PROJECT_PATH%" -buildLinux64Player "%LINUX_TARGET_PATH%\%LINUX_TARGET_NAME%"

REM Windows Build
set WINDOWS_TARGET_PATH=.\build\windows
set WINDOWS_TARGET_NAME=evaluation.exe

del /s /q "%WINDOWS_TARGET_PATH%\*.*"
"%UNITY_EDITOR_LOCATION%" -quit -batchmode -projectPath "%UNITY_PROJECT_PATH%" -buildWindows64Player "%WINDOWS_TARGET_PATH%\%WINDOWS_TARGET_NAME%"
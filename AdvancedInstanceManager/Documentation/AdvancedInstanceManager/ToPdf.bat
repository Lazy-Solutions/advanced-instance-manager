@echo off
setlocal EnableDelayedExpansion
rmdir /s/q temp
mkdir temp

for %%x in (
Home.md ^
QuickStart.md ^
InstanceManagerWindowGuide.md ^
InstanceManager.md ^
UnityInstance.md ^
Layout.md ^
ActionUtility.md ^
CommandUtility.md ^
CrossProcessEventUtility.md ^
GUIExt.md ^
InstanceUtility.md ^
ProgressUtility.md ^
WindowLayoutUtility.md
) do (

  copy %%x temp\%%x

  set currentParameter=%%x
  set currentParameter=!currentParameter:~0,-3!

  echo.# !currentParameter!>temp\%%x
  type %%x >>temp\%%x

  ECHO \newpage >> temp\%%x

)

echo Files copied to \temp
echo Converting to pdf

pandoc -s ^
temp\home.md ^
temp\quickstart.md ^
temp\InstanceManagerWindowGuide.md ^
temp\InstanceManager.md ^
temp\UnityInstance.md ^
temp\Layout.md ^
temp\ActionUtility.md ^
temp\CommandUtility.md ^
temp\CrossProcessEventUtility.md ^
temp\GUIExt.md ^
temp\InstanceUtility.md ^
temp\ProgressUtility.md ^
temp\WindowLayoutUtility.md ^
-o ..\Documentation.pdf

rmdir /s/q temp

echo done

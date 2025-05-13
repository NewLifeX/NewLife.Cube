@echo off

set name=StarWeb
set clover=..\..\Tools\clover.exe
if not exist "%clover%" (
    set clover=..\..\Doc\clover.exe
)

for %%f in (*.exe) do (
    rem 获取文件名（去掉扩展名）
    set "name=%%~nf"
    goto :found
)

:found
if defined name (
    del %name%.zip /f/q
    %clover% zip %name%.zip *.exe *.dll *.pdb appsettings.json *.runtimeconfig.json
) else (
    echo No exe file found in the current directory.
)

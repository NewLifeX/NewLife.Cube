xcode
IF ERRORLEVEL 1 (
    echo xcode 命令执行失败，正在安装 .NET 工具...
    dotnet tool install xcodetool -g --prerelease
)
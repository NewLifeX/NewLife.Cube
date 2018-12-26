// 自动选择最新的文件源
var di = ".".AsDirectory();
var srcs = new String[] { @"..\Bin", @"..\..\Bin", @"C:\X\DLL", @"C:\X\Bin", @"D:\X\Bin", @"..\..\..\X\Bin\net45" };
di.CopyIfNewer(srcs, "*.dll;*.exe;*.xml;*.pdb;*.cs");

di = "..\\DLL20".AsDirectory();
if (di.Exists)
{
    srcs = new String[] { @"..\Bin\netstandard2.0", @"..\..\Bin\netstandard2.0", @"..\..\..\X\Bin\netstandard2.0" };
    di.CopyIfNewer(srcs, "*.dll;*.exe;*.xml;*.pdb;*.cs");
}
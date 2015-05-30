"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" AsyncProxy.sln

cd Nuget
mkdir lib
mkdir lib\net45

copy ..\AsyncProxy\bin\Debug\AsyncProxy.* lib\net45

nuget pack AsyncProxy.nuspec

rmdir lib /S /Q

cd ..
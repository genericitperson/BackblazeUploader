# Building this project

To build this project you require a compatible version of visual studio, from there a build should simply work.

To generate our builds we use the commands below, these can be customised to meet your needs if necessary.

### Windows Exe including runtime (no dependencies)

```
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
```
This command results in several files in the bin\win-x64\publish folder. Only the .exe is required for distribution.

### Windows Exe without runtime (requires .Net Core 3.0)

```
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true --self-contained false
```

This command results in several files in the bin\win-x64\publish folder. Only the .exe is required for distribution.


# Building this project

To build this project you require a compatible version of visual studio, from there a build should simply work.

To generate our single file builds we use either of the below commands.

- For the self contained installer:

```
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true
```

- For a reduced size file:

```
dotnet publish -r win-x64 -c Release /p:PublishTrimmed=true
```

*Please note: You can combine the two /p options to get both a self contained and a reduced file size build*
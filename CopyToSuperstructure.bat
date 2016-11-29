cd /d %~dp0

robocopy .\out\run\debug ..\superstructure\out\run\Debug *.* 
robocopy .\out\run\release ..\superstructure\out\run\Release *.* 
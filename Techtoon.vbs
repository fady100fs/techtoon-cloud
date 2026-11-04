Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
sh.CurrentDirectory = scriptDir
sh.Run """C:\Users\fady1\AppData\Local\Programs\Python\Python311\pythonw.exe"" launcher.py", 0, False
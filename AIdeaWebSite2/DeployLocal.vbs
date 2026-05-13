' 双击：在「与 Site 同级」的目录下运行 DeployLocal.cmd（与手动双击 cmd 相同）
Option Explicit
Dim sh, fso, root
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
root = fso.GetParentFolderName(WScript.ScriptFullName)
sh.CurrentDirectory = root
If Not fso.FileExists(root & "\DeployLocal.cmd") Then
	MsgBox "未找到 DeployLocal.cmd，请把本文件与 DeployLocal.cmd 放在同一文件夹（AIdeaWebSite2 根目录）。", vbCritical, "DeployLocal"
	WScript.Quit 1
End If
sh.Run "cmd /c """ & root & "\DeployLocal.cmd""", 1, False

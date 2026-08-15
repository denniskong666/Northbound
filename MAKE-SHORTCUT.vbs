Option Explicit
' ============================================================
'  Create a desktop SHORTCUT (.lnk) named "推送GitHub.lnk"
'  that runs DESKTOP-PUSH-TO-GITHUB.bat in the project folder.
'
'  HOW TO USE THIS FILE:
'   Right-click MAKE-SHORTCUT.vbs -> Open with Command Script
'   OR just double-click it (Windows Script Host default).
'
'  After a second you will see the shortcut on your Desktop.
' ============================================================

Dim shell, fso, desktopPath, lnk, projectDir, batPath

Set shell = CreateObject("WScript.Shell")
Set fso   = CreateObject("Scripting.FileSystemObject")

projectDir = fso.GetParentFolderName(WScript.ScriptFullName)
batPath    = projectDir & "\DESKTOP-PUSH-TO-GITHUB.bat"
desktopPath = shell.SpecialFolders("Desktop")

If Not fso.FileExists(batPath) Then
    MsgBox "Cannot find " & batPath & vbCrLf & _
           "Put MAKE-SHORTCUT.vbs inside your Capstone folder and try again.", _
           16, "Shortcut not created"
    WScript.Quit 1
End If

Set lnk = shell.CreateShortcut(desktopPath & "\Push Northbound to GitHub.lnk")
lnk.TargetPath       = batPath
lnk.WorkingDirectory = projectDir
lnk.WindowStyle      = 1           ' normal window (never minimized)
lnk.Description      = "Double-click to push Northbound project code to GitHub"
lnk.Save

MsgBox "Shortcut created on Desktop:" & vbCrLf & _
       desktopPath & "\Push Northbound to GitHub.lnk" & vbCrLf & vbCrLf & _
       "Now just double-click that icon to push to GitHub.", _
       64, "Done"

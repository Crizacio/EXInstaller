﻿Imports Microsoft.Win32
Public Class Main
    Dim WithEvents DownloadInstructive As New Net.WebClient
    Dim DownloadInstructiveURI As Uri

    Dim WithEvents DownloadInstallPackage As New Net.WebClient
    Dim DownloadInstallPackageURI As Uri

    Private Sub Debugger_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        SetCurrentStatus("Inicializando...")
        TaskbarProgress.SetState(Me.Handle, TaskbarProgress.TaskbarStates.Indeterminate)
        StartParametros = Command() 'Lee argumentos
        If My.Computer.FileSystem.DirectoryExists(DIRCommons) = False Then
            My.Computer.FileSystem.CreateDirectory(DIRCommons)
        End If
        If My.Computer.FileSystem.DirectoryExists(DIRTemp) = False Then
            My.Computer.FileSystem.CreateDirectory(DIRTemp)
        End If
        ReadParameters()
    End Sub

    Sub ReadParameters() 'Lee los argumentos y aplica
        SetCurrentStatus("Leyendo parametros...")
        Try
            Dim parametro As String = Command()
            Dim Argumentos() As String = Command().Split(" ")

            For Each arg As String In Argumentos
                If arg.Contains("/Uninstall") Then
                    IsUninstall = True
                    UninstallIt()
                    parametro = parametro.Replace("/Uninstall", Nothing)
                ElseIf arg.Contains("/Assistant") Then 'Fue iniciado por el "post-instalador"
                    IsAssistant = True
                    AssistantMode()
                    parametro = parametro.Replace("/Assistant", Nothing)
                ElseIf arg.Contains("/Instructive~") Then
                    If CanOverwrite = True Then
                        parametro = parametro.Replace("/Instructive~", Nothing)
                        Dim Args As String() = parametro.Split(",")
                        AssemblyName = Args(0).Trim()
                        AssemblyVersion = Args(1).Trim()
                        InstructiveURL = Args(2).Trim()
                        IsInjected = False
                    End If
                End If
            Next

            If IsInjected = True Then
                LoadSTUB()
            End If

            If Application.ExecutablePath.Contains("uninstall.exe") Then
                If IsUninstall = True Then
                    Restart("/Uninstall")
                Else
                    Restart(Nothing)
                End If
            End If

            If InstructiveURL = Nothing Then
                MsgBox("No se encontro una pre-configuracion", MsgBoxStyle.Critical, "Instructivo")
                Complementos.Closing()
            End If

            CommonStart()
        Catch ex As Exception
            AddToLog("[ReadParameters@Main]Error: ", ex.Message, True)
            MsgBox("Error al leer los parametros", MsgBoxStyle.Critical, "Instalador")
            Complementos.Closing()
        End Try
    End Sub

    Sub UninstallIt()
        IsUninstall = True
        lblTitle.Text = "Desinstalando..."
        lblSubTitle.Text = "Espere mientras la desinstalación del programa se completa."
        Text = "Desinstalador"
    End Sub

    Sub Restart(ByVal parameter As String)
        Try
            If My.Computer.FileSystem.FileExists(DIRTemp & "\" & AssemblyName & "_Assistant.exe") = True Then
                My.Computer.FileSystem.DeleteFile(DIRTemp & "\" & AssemblyName & "_Assistant.exe")
            End If
            My.Computer.FileSystem.CopyFile(Application.ExecutablePath, DIRTemp & "\" & AssemblyName & "_Assistant.exe")
            Process.Start(DIRTemp & "\" & AssemblyName & "_Assistant.exe", parameter & " /Instructive~" & AssemblyName & "," & AssemblyVersion & "," & InstructiveURL)
            End
            'Complementos.Closing()
        Catch ex As Exception
            AddToLog("[Restart@Main]Error: ", ex.Message, True)
        End Try
    End Sub

    Sub AssistantMode()
        Dim StartBlinkForFocus = WindowsApi.FlashWindow(Process.GetCurrentProcess().MainWindowHandle, True, True, 3)
        lblTitle.Text = "Asistente"
        lblSubTitle.Text = "Este programa ya esta instalado, seleccione una opcion."
        lblStatus.Text = "Esperando..."
        ProgressBarStatus.Style = ProgressBarStyle.Marquee
        Text = "Asistente"
        Button1.Enabled = True
        Button2.Enabled = True
        Button1.Visible = True
        Button2.Visible = True
    End Sub

    Sub Reinstall()
        Dim StartBlinkForFocus = WindowsApi.FlashWindow(Process.GetCurrentProcess().MainWindowHandle, True, True, 3)
        lblTitle.Text = "Reinstalando..."
        lblSubTitle.Text = "Espere mientras el programa se reinstala..."
        lblStatus.Text = "Esperando..."
        ProgressBarStatus.Style = ProgressBarStyle.Marquee
        Text = "Reinstalador"
        Button1.Enabled = False
        Button2.Enabled = False
        Button1.Visible = False
        Button2.Visible = False
        InstallIt()
    End Sub

    Sub IsComponent()
        Dim StartBlinkForFocus = WindowsApi.FlashWindow(Process.GetCurrentProcess().MainWindowHandle, True, True, 3)
        lblTitle.Text = "Aplicando..."
        lblSubTitle.Text = "Espere mientras el componente se aplica..."
        lblStatus.Text = "Esperando..."
        ProgressBarStatus.Style = ProgressBarStyle.Marquee
        Text = "Aplicador"
    End Sub

    Sub CommonStart()
        SetCurrentStatus("Consultando la informacion del equipo...")
        Try
            Dim consultaSQLArquitectura As String = "SELECT * FROM Win32_Processor"
            Dim objArquitectura As New ManagementObjectSearcher(consultaSQLArquitectura)
            For Each info As ManagementObject In objArquitectura.Get()
                ArquitecturaSO = info.Properties("AddressWidth").Value.ToString()
            Next info
            If My.Computer.FileSystem.FileExists(InstructiveFilePath) = True Then
                My.Computer.FileSystem.DeleteFile(InstructiveFilePath)
            End If
        Catch ex As Exception
            AddToLog("[CommonStart@Main]Error: ", ex.Message, True)
        End Try
        GetInstructive()
    End Sub

    Sub SetCurrentStatus(ByVal Status As String)
        lblStatus.Text = Status
        AddToLog("Status: ", Status)
    End Sub

    Sub GetInstructive() 'Comenzamos el proceso de descarga del instructivo indicado
        Try
            SetCurrentStatus("Comenzo la descarga del instructivo...")
            Dim StartBlinkForFocus = WindowsApi.FlashWindow(Process.GetCurrentProcess().MainWindowHandle, True, True, 5)
            DownloadInstructiveURI = New Uri(InstructiveURL)
            DownloadInstructive.DownloadFileAsync(DownloadInstructiveURI, InstructiveFilePath)
        Catch ex As Exception
            AddToLog("[GetInstructive@Main]Error: ", ex.Message, True)
        End Try
    End Sub

    Sub LoadSTUB()
        If CanOverwrite = True Then
            SetCurrentStatus("Leyendo datos pre-cargados...")
            Try
                FileOpen(1, Application.ExecutablePath, OpenMode.Binary, OpenAccess.Read)
                Dim stubb As String = Space(LOF(1))
                Dim FileSplit = "|EXI|"
                FileGet(1, stubb)
                FileClose(1)
                Dim opt() As String = Split(stubb, FileSplit)
                AssemblyName = opt(1)
                AssemblyVersion = opt(2)
                InstructiveURL = opt(3)
            Catch ex As Exception
                AddToLog("[LoadSTUB@Debugger]Error: ", ex.Message, True)
            End Try
        End If
    End Sub

    Sub LoadInstructive() 'Leemos el instructivo descargado
        Try
            SetCurrentStatus("Leyendo el instructivo... 1/3")
            Instructive_Package_Status = GetIniValue("Package", "Status", InstructiveFilePath)
            Instructive_Package_AssemblyName = GetIniValue("Package", "AssemblyName", InstructiveFilePath)
            Instructive_Package_AssemblyVersion = GetIniValue("Package", "AssemblyVersion", InstructiveFilePath)
            Instructive_Package_Company = GetIniValue("Package", "Company", InstructiveFilePath)
            Instructive_Package_WebUrl = GetIniValue("Package", "WebUrl", InstructiveFilePath)
            Instructive_Package_PackageName = GetIniValue("Package", "PackageName", InstructiveFilePath)
            Instructive_Package_IsComponent = GetIniValue("Package", "IsComponent", InstructiveFilePath)
            Instructive_Package_InstallerVersion = GetIniValue("Package", "InstallerVersion", InstructiveFilePath)
            Instructive_Package_BitsArch = GetIniValue("Package", "BitsArch", InstructiveFilePath)
            SetCurrentStatus("Leyendo el instructivo... 2/3")
            Instructive_Installer_Status = GetIniValue("Installer", "Status", InstructiveFilePath)
            Instructive_Installer_EnableDowngrade = GetIniValue("Installer", "EnableDowngrade", InstructiveFilePath)
            Instructive_Installer_NeedRestart = GetIniValue("Installer", "NeedRestart", InstructiveFilePath)
            Instructive_Installer_NeedStartUp = GetIniValue("Installer", "NeedStartUp", InstructiveFilePath)
            Instructive_Installer_NeedElevateAccess = GetIniValue("Installer", "NeedElevateAccess", InstructiveFilePath)
            Instructive_Installer_InstallFolder = GetIniValue("Installer", "InstallFolder", InstructiveFilePath)
            Instructive_Installer_EULA = GetIniValue("Installer", "EULA", InstructiveFilePath)
            Instructive_Installer_Installer = GetIniValue("Installer", "Installer", InstructiveFilePath)
            Instructive_Installer_InstallPackage = GetIniValue("Installer", "InstallPackage", InstructiveFilePath)
            SetCurrentStatus("Leyendo el instructivo... 3/3")
            Instructive_HelpLinks_ChangeLogLink = GetIniValue("HelpLinks", "ChangeLogLink", InstructiveFilePath)
            Instructive_HelpLinks_UseGuide = GetIniValue("HelpLinks", "UseGuide", InstructiveFilePath)
            Instructive_HelpLinks_AppAbout = GetIniValue("HelpLinks", "AppAbout", InstructiveFilePath)
            Instructive_HelpLinks_Contact = GetIniValue("HelpLinks", "Contact", InstructiveFilePath)
        Catch ex As Exception
            AddToLog("[LoadInstructive(0)@Debugger]Error: ", ex.Message, True)
        End Try
        SetCurrentStatus("Esperando...")
        'DEFINICIONES PARA LA INSTALACION ----------
        Try
            WhereDoIInstall()
            If Instructive_Package_IsComponent = "False" Then 'Si es componente no se comprueba su previa existencia
                If IsUninstall = False Then
                    If My.Computer.FileSystem.FileExists(ExePackage) = True Then
                        If RegistradorInstalacion IsNot Nothing Then
                            AssistantMode()
                            Exit Sub
                        End If
                    End If
                Else
                    Uninstall()
                    Exit Sub
                End If
                InstallIt()
            Else
                IsComponent()
                InstallIt()
            End If
        Catch ex As Exception
            AddToLog("[LoadInstructive(1)@Debugger]Error: ", ex.Message, True)
        End Try
    End Sub

    Sub WhereDoIInstall()
        Try
            x32bits = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" & Instructive_Package_AssemblyName
            x64x32bits = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" & Instructive_Package_AssemblyName
            Dim UbicacionFinal As String
            Dim EsProgramFiles As Boolean = True
            If Instructive_Installer_InstallFolder.Contains("%username%") Then
                EsProgramFiles = False
                Instructive_Installer_InstallFolder = Instructive_Installer_InstallFolder.Replace("%username%", Environment.UserName)
            ElseIf Instructive_Installer_InstallFolder.Contains("%programfiles%") Then
                EsProgramFiles = True
                Instructive_Installer_InstallFolder = Instructive_Installer_InstallFolder.Replace("%programfiles%", Nothing)
            End If
            If ArquitecturaSO = "32" Then 'Si el PC es x32 (x86)
                UbicacionFinal = "C:\Program Files" & Instructive_Installer_InstallFolder
                If RegistradorInstalacion Is Nothing Then
                    Registry.LocalMachine.CreateSubKey(x32bits)
                End If
                RegistradorInstalacion = Registry.LocalMachine.OpenSubKey(x32bits, True)
                If Instructive_Package_BitsArch = "64" Then 'Si el PC es de x32 pero el programa es de x64
                    MsgBox("El programa a instalar requiere de un procesador de 64bits y no de 32bits", MsgBoxStyle.Critical, "No se puede instalar")
                    End
                End If
            ElseIf ArquitecturaSO = "64" Then 'Si el PC es x64
                If Instructive_Package_BitsArch = "32" Then 'Si el PC es de x64 pero el programa es de x32
                    UbicacionFinal = "C:\Program Files (x86)" & Instructive_Installer_InstallFolder
                    If RegistradorInstalacion Is Nothing Then
                        Registry.LocalMachine.CreateSubKey(x32bits)
                    End If
                    RegistradorInstalacion = Registry.LocalMachine.OpenSubKey(x32bits, True)
                Else 'Si el PC es de x64 y el programa tambien es de x64
                    UbicacionFinal = "C:\Program Files" & Instructive_Installer_InstallFolder
                    If RegistradorInstalacion Is Nothing Then
                        Registry.LocalMachine.CreateSubKey(x64x32bits)
                    End If
                    RegistradorInstalacion = Registry.LocalMachine.OpenSubKey(x64x32bits, True)
                End If
            Else 'Si nada aplica, se instala donde dios quiera
                UbicacionFinal = "C:\Program Files" & Instructive_Installer_InstallFolder
                If RegistradorInstalacion Is Nothing Then
                    Registry.LocalMachine.CreateSubKey(x32bits)
                End If
                RegistradorInstalacion = Registry.LocalMachine.OpenSubKey(x32bits, True)
            End If
            If EsProgramFiles = True Then
                InstallerPathBuilder = UbicacionFinal
            Else
                InstallerPathBuilder = Instructive_Installer_InstallFolder
            End If
            ExePackage = InstallerPathBuilder & "\" & Instructive_Package_PackageName & ".exe" 'Indicamos la ruta del ejecutable que se esta instalando
        Catch ex As Exception
            AddToLog("[WhereDoIInstall@Debugger]Error: ", ex.Message, True)
        End Try
    End Sub

    Sub InstallIt() 'Comenzamos el proceso de descarga del paquete indicado por el instructivo
        Try
            SetCurrentStatus("Descargando el paquete de instalacion...")
            ProgressBarStatus.Style = ProgressBarStyle.Blocks
            DownloadInstallPackageURI = New Uri(Instructive_Installer_InstallPackage)
            DownloadInstallPackage.DownloadFileAsync(DownloadInstallPackageURI, DIRTemp & "\" & AssemblyName & "_" & Instructive_Package_AssemblyVersion & ".zip")
        Catch ex As Exception
            AddToLog("[InstallIt@Main]Error: ", ex.Message, True)
            MsgBox("No se pudo descargar el instructivo", MsgBoxStyle.Critical, "Instructivo")
            Complementos.Closing()
        End Try
    End Sub

    Private Sub DownloadInstallPackage_DownloadFileCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.AsyncCompletedEventArgs) Handles DownloadInstallPackage.DownloadFileCompleted
        Install()
    End Sub

    Private Sub DownloadInstallPackage_DownloadProgressChanged(ByVal sender As Object, ByVal e As System.Net.DownloadProgressChangedEventArgs) Handles DownloadInstallPackage.DownloadProgressChanged
        Try
            Dim bytesIn As Double = Double.Parse(e.BytesReceived.ToString())
            Dim totalBytes As Double = Double.Parse(e.TotalBytesToReceive.ToString())
            lblStatusStatus.Text = CStr(e.ProgressPercentage & ("% ")) & FormatBytes(bytesIn) & "/" & FormatBytes(totalBytes)
        Catch
        End Try
        Try
            ProgressBarStatus.Value = e.ProgressPercentage
        Catch
        End Try
        Try
            PackageSize = e.TotalBytesToReceive
        Catch
        End Try
        Try
            TaskbarProgress.SetValue(Me.Handle, e.ProgressPercentage.ToString, 100)
        Catch
        End Try
    End Sub

    Private Sub DownloadInstructive_DownloadFileCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.AsyncCompletedEventArgs) Handles DownloadInstructive.DownloadFileCompleted
        LoadInstructive()
    End Sub

    Private Sub Desinstalar_Click(sender As Object, e As EventArgs) Handles Button2.Click
        UninstallIt()
        Uninstall()
    End Sub
    Private Sub Reinstalar_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Reinstall()
    End Sub
End Class
'Problemas
'   Por alguna razon cuando se llama al uninstall.exe desde la carpeta de instalacion y le das en Desinstalar, por alguna razon el programa se congela y no elimina por completo la ruta de instalacion
'       es algo realmente extraño.
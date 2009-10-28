Imports System
Imports Microsoft.VisualStudio.CommandBars
Imports Extensibility
Imports EnvDTE
Imports EnvDTE80
Imports System.IO
Imports System.Collections.Generic
Imports model
Imports System.Text.RegularExpressions
Imports System.Diagnostics

Public Class Connect

    Implements IDTExtensibility2
    Implements IDTCommandTarget

    Private _applicationObject As DTE2
	Private _addInInstance As AddIn

    '''<summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
    Public Sub New()

    End Sub

    '''<summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
    '''<param name='application'>Root object of the host application.</param>
    '''<param name='connectMode'>Describes how the Add-in is being loaded.</param>
    '''<param name='addInInst'>Object representing this Add-in.</param>
    '''<remarks></remarks>
    Public Sub OnConnection(ByVal application As Object _
        , ByVal connectMode As ext_ConnectMode _
        , ByVal addInInst As Object _
        , ByRef custom As Array) Implements IDTExtensibility2.OnConnection

        _applicationObject = CType(application, DTE2)
        _addInInstance = CType(addInInst, AddIn)

        If connectMode = ext_ConnectMode.ext_cm_UISetup Then
            Dim commands As Commands2 = CType(_applicationObject.Commands, Commands2)
            Dim toolsMenuName As String = "Database Project"

			'Place the commands on the Database Project context menu.
            Dim commandBars As CommandBars = CType(_applicationObject.CommandBars, CommandBars)
			Dim dbProjCommandBar As CommandBar = commandBars.Item("Database Project")

			Try
				AddCommand("add_migration", "Add Migration", "Creates a new database migration." _
				 , commands, _addInInstance, dbProjCommandBar)
			Catch ex As Exception
				MsgBox(ex.Message)
			End Try
			

			AddCommand("createdb", "Create DB", "Creates an instance of the database from scripts." _
			 , commands, _addInInstance, dbProjCommandBar)
            
        End If
	End Sub

	Private Sub AddCommand(ByVal name As String, ByVal text As String, ByVal desc As String, _
   ByVal cmds As Commands2, ByVal addIn As AddIn, ByVal cmdBar As CommandBar)
		'check to see if the command already exists
		Dim myCmd As Command = Nothing
		For Each c As CommandBarControl In cmdBar.Controls
			If TypeOf c Is Command AndAlso CType(c, Command).Name = name Then
				myCmd = CType(c, Command)
				Exit For
			End If
		Next

		If myCmd Is Nothing Then
			'add the command
			Dim command As Command = cmds.AddNamedCommand2( _
			 addIn, name, text, desc, False, 1, Nothing, _
			 CType(vsCommandStatus.vsCommandStatusSupported, Integer) _
			 + CType(vsCommandStatus.vsCommandStatusEnabled, Integer), _
			 vsCommandStyle.vsCommandStylePictAndText, _
			 vsCommandControlType.vsCommandControlTypeButton)

			'Find the appropriate command bar on the MenuBar command bar:
			command.AddControl(cmdBar, cmdBar.Controls.Count - 1)
		End If
	End Sub

	'''<summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
	'''<param name='disconnectMode'>Describes how the Add-in is being unloaded.</param>
	'''<param name='custom'>Array of parameters that are host application specific.</param>
	'''<remarks></remarks>"
	Public Sub OnDisconnection(ByVal disconnectMode As ext_DisconnectMode, ByRef custom As Array) Implements IDTExtensibility2.OnDisconnection
	End Sub

	'''<summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification that the collection of Add-ins has changed.</summary>
	'''<param name='custom'>Array of parameters that are host application specific.</param>
	'''<remarks></remarks>
	Public Sub OnAddInsUpdate(ByRef custom As Array) Implements IDTExtensibility2.OnAddInsUpdate
	End Sub

	'''<summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
	'''<param name='custom'>Array of parameters that are host application specific.</param>
	'''<remarks></remarks>
	Public Sub OnStartupComplete(ByRef custom As Array) Implements IDTExtensibility2.OnStartupComplete
		Dim a As String = ""
	End Sub

	'''<summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
	'''<param name='custom'>Array of parameters that are host application specific.</param>
	'''<remarks></remarks>
	Public Sub OnBeginShutdown(ByRef custom As Array) Implements IDTExtensibility2.OnBeginShutdown
	End Sub

	'''<summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
	'''<param name='commandName'>The name of the command to determine state for.</param>
	'''<param name='neededText'>Text that is needed for the command.</param>
	'''<param name='status'>The state of the command in the user interface.</param>
	'''<param name='commandText'>Text requested by the neededText parameter.</param>
	'''<remarks></remarks>
	Public Sub QueryStatus(ByVal commandName As String, ByVal neededText As vsCommandStatusTextWanted, ByRef status As vsCommandStatus, ByRef commandText As Object) Implements IDTCommandTarget.QueryStatus
		If neededText = vsCommandStatusTextWanted.vsCommandStatusTextWantedNone Then
			If commandName.Contains("vs_addin.Connect.") Then
				status = CType(vsCommandStatus.vsCommandStatusEnabled + vsCommandStatus.vsCommandStatusSupported, vsCommandStatus)
			Else
				status = vsCommandStatus.vsCommandStatusUnsupported
			End If
		End If
	End Sub

	'''<summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
	'''<param name='commandName'>The name of the command to execute.</param>
	'''<param name='executeOption'>Describes how the command should be run.</param>
	'''<param name='varIn'>Parameters passed from the caller to the command handler.</param>
	'''<param name='varOut'>Parameters passed from the command handler to the caller.</param>
	'''<param name='handled'>Informs the caller if the command was handled or not.</param>
	'''<remarks></remarks>
	Public Sub Exec(ByVal commandName As String, ByVal executeOption As vsCommandExecOption, ByRef varIn As Object, ByRef varOut As Object, ByRef handled As Boolean) Implements IDTCommandTarget.Exec
		handled = False
		If executeOption = vsCommandExecOption.vsCommandExecOptionDoDefault Then
			Try
				Dim slnExplorer As UIHierarchy = CType(_applicationObject.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object, UIHierarchy)
				Dim projectName As String = CType(slnExplorer.SelectedItems(0), UIHierarchyItem).Name

				Dim proj As Project
				For Each p As Project In _applicationObject.Solution.Projects
					If projectName = p.Name Then
						proj = p
						Exit For
					End If
				Next

				Dim slnFile As New FileInfo(_applicationObject.Solution.FullName)
				Dim dbProj As New DbProject()
				dbProj.Load(slnFile.DirectoryName + "\" + proj.UniqueName)

				Select Case commandName
					Case "vs_addin.Connect.add_migration"
						CreateMigration(dbProj)
						handled = True
						Exit Sub
					Case "vs_addin.Connect.createdb"
						CreateDb(dbProj)
						handled = True
						Exit Sub

				End Select
			Catch ex As Exception
				OutputPane.OutputString(ex.Message + vbCrLf)
				OutputPane.OutputString(ex.StackTrace)
			End Try
		End If
	End Sub

	Private Sub CreateMigration(ByVal dbProj As DbProject)
		'get the previous version of the script dir from souce control
		OutputPane.OutputString(String.Format("Getting previous version of {0} source control...{1}" _
		 , dbProj.Name, vbCrLf))
		GetProjectFromSourceControl(dbProj, "temp_prev", 10000)

		'create a temp db for the previous version
		OutputPane.OutputString("Creating temp databases to compare..." + vbCrLf)
		Dim cnStrBldr As New SqlClient.SqlConnectionStringBuilder(dbProj.DefDBRef.ConnectStr)
		cnStrBldr.InitialCatalog = "temp_prev_" + dbProj.Name
		Dim tempPrev As New Database("temp_prev_" + dbProj.Name)
		tempPrev.Connection = cnStrBldr.ToString()
		tempPrev.Dir = dbProj.Dir + "\temp_prev"
		OutputPane.OutputString(String.Format("Creating {0} {1}...{2}", cnStrBldr.DataSource, cnStrBldr.InitialCatalog, vbCrLf))
		tempPrev.CreateFromDir(True)
		tempPrev.Load()

		'create a temp db for the current version
		Dim tempCurrent As New Database("temp_current_" + dbProj.Name)
		cnStrBldr.InitialCatalog = "temp_current_" + dbProj.Name
		tempCurrent.Connection = cnStrBldr.ToString()
		tempCurrent.Dir = dbProj.Dir
		OutputPane.OutputString(String.Format("Creating {0} {1}...{2}", cnStrBldr.DataSource, cnStrBldr.InitialCatalog, vbCrLf))
		tempCurrent.CreateFromDir(True)
		tempCurrent.Load()

		'compare the two temp dbs
		OutputPane.OutputString("Comaring temp databases..." + vbCrLf)
		Dim down As DatabaseDiff = tempPrev.Compare(tempCurrent)
		Dim up As DatabaseDiff = tempCurrent.Compare(tempPrev)

		If up.IsDiff Then
			'if they are different create the migration
			OutputPane.OutputString("Creating migration scripts..." + vbCrLf)
			CreateMigrationScripts(dbProj, up, down)
		Else
			'else let the user know no migration is needed
			OutputPane.OutputString("No migration needed" + vbCrLf)
		End If

		'clean up
		OutputPane.OutputString("Cleaning up..." + vbCrLf)
		For Each f As String In Directory.GetFiles(tempPrev.Dir, "*", SearchOption.AllDirectories)
			Dim fi As New FileInfo(f)
			fi.Attributes = FileAttributes.Normal
			fi.Delete()
		Next
		Directory.Delete(tempPrev.Dir, True)
		DBHelper.DropDb(tempPrev.Connection)
		DBHelper.DropDb(tempCurrent.Connection)

		OutputPane.OutputString("Process complete" + vbCrLf)
	End Sub

	Private Sub CreateDb(ByVal dbProj As DbProject)
		If dbProj.DefDBRef Is Nothing Then
			MsgBox("You must set a default database reference.")
			Exit Sub
		End If

		Dim cnStrBldr As New SqlClient.SqlConnectionStringBuilder(dbProj.DefDBRef.ConnectStr)
		If DBHelper.DbExists(dbProj.DefDBRef.ConnectStr) Then
			Dim rslt As MsgBoxResult = MsgBox( _
			 String.Format("{0} {1} already exists do you want to drop it?" _
			   , cnStrBldr.DataSource, cnStrBldr.InitialCatalog) _
			 , MsgBoxStyle.YesNo)

			If rslt = MsgBoxResult.No Then
				Exit Sub
			End If
		End If

		OutputPane.Clear()

		Dim db As New Database(cnStrBldr.InitialCatalog)
		db.Connection = dbProj.DefDBRef.ConnectStr
		db.Dir = dbProj.Dir
		Try
			db.CreateFromDir(True)
			WriteOutput(String.Format("{0} {1} created successfully.", cnStrBldr.DataSource, cnStrBldr.InitialCatalog))
		Catch ex As SqlFileException
			OutputPane.Activate()
			OutputPane.OutputTaskItemString(ex.Message + vbCrLf, vsTaskPriority.vsTaskPriorityHigh _
			 , vsTaskCategories.vsTaskCategoryMisc _
			 , vsTaskIcon.vsTaskIconShortcut _
			 , ex.FileName, ex.LineNumber, "", True)

			OutputPane.OutputString(String.Format("Failed to fully create {0} {1}. See previous errors.{2}" _
			 , cnStrBldr.DataSource, cnStrBldr.InitialCatalog, vbCrLf))

		Catch ex As DataFileException
			OutputPane.Activate()
			OutputPane.OutputTaskItemString(ex.Message + vbCrLf, vsTaskPriority.vsTaskPriorityHigh _
			 , vsTaskCategories.vsTaskCategoryMisc _
			 , vsTaskIcon.vsTaskIconShortcut _
			 , ex.FileName, ex.LineNumber, "", True)

			OutputPane.OutputString(String.Format("Failed to fully create {0} {1}. See previous errors.{2}" _
			 , cnStrBldr.DataSource, cnStrBldr.InitialCatalog, vbCrLf))

		End Try
		
	End Sub

	Private _owPane As OutputWindowPane = Nothing
	Public ReadOnly Property OutputPane() As OutputWindowPane
		Get
			If _owPane Is Nothing Then
				Dim win As Window = _applicationObject.Windows.Item(EnvDTE.Constants.vsWindowKindOutput)
				Dim ow As OutputWindow = win.Object
				_owPane = ow.OutputWindowPanes.Add("Schemanator")
			End If
			Return _owPane
		End Get
	End Property

	Private Sub GetProjectFromSourceControl(ByVal dbProj As DbProject, ByVal tempPath As String, ByVal timeout As Integer)
		Dim ssPath As String = "c:\program files\microsoft visual sourcesafe"
		Directory.CreateDirectory(dbProj.Dir + "\" + tempPath)
		Dim cmd As String = String.Format("/c ss.exe get ""{0}/*"" -GL""{1}\{2}"" -I-N -O- -R -W", dbProj.RealSccProjectName, dbProj.Dir, tempPath)
		OutputPane.OutputString(cmd + vbCrLf)
		Using p As New System.Diagnostics.Process()
			p.StartInfo.WorkingDirectory = dbProj.Path
			p.StartInfo.EnvironmentVariables("PATH") += ";" + ssPath
			If Not p.StartInfo.EnvironmentVariables.ContainsKey("SSDIR") Then
				p.StartInfo.EnvironmentVariables("SSDIR") = dbProj.RealSccAuxPath
			End If
			p.StartInfo.CreateNoWindow = True
			p.StartInfo.FileName = "cmd.exe"
			p.StartInfo.Arguments = cmd
			p.StartInfo.UseShellExecute = False
			p.StartInfo.RedirectStandardOutput = True
			p.StartInfo.RedirectStandardError = True
			p.StartInfo.RedirectStandardInput = True
			p.Start()
			p.WaitForExit(timeout)

			OutputPane.OutputString(p.StandardError.ReadToEnd())
			OutputPane.OutputString(p.StandardOutput.ReadToEnd())
		End Using
	End Sub

	Private Sub CreateMigrationScripts(ByVal dbProj As DbProject, ByVal up As DatabaseDiff, ByVal down As DatabaseDiff)
		If Not _applicationObject.SourceControl.IsItemCheckedOut(dbProj.FileName) Then
			_applicationObject.SourceControl.CheckOutItem(dbProj.FileName)
		End If

		Dim migrateDirPath As String = dbProj.Dir + "\migrate"
		Dim newDirName As String = Now.ToString("yyyyMMdd_hhmm")
		Dim newDirPath As String = migrateDirPath + "\" + newDirName

		If Not Directory.Exists(migrateDirPath) Then Directory.CreateDirectory(migrateDirPath)
		Directory.CreateDirectory(newDirPath)
		Directory.CreateDirectory(newDirPath + "\down")
		Directory.CreateDirectory(newDirPath + "\up")
		File.WriteAllText(newDirPath + "\down\1.sql", down.Script())
		File.WriteAllText(newDirPath + "\up\1.sql", up.Script())

		'HACK - can't find the API call to add a directory to a
		'database project so I'm editing the .dbp file directly."
		'the solution is closed before the project file is changed
		'then re-opened after to prevent prompting the user to reload
		'
		'the dbProj must be re-loaded after the solution is closed
		'to prevent loss of in memory changes.
		Dim slnFileName As String = _applicationObject.Solution.FullName
		_applicationObject.Solution.Close(True)
		dbProj.Load(dbProj.FileName)

		Dim migrate As DbFolder = dbProj.FindFolder("migrate")
		If migrate Is Nothing Then
			migrate = New DbFolder("migrate")
			dbProj.Folders.Add(migrate)
		End If

		Dim newMigrate As New DbFolder(newDirName)
		migrate.Folders.Add(newMigrate)
		newMigrate.Folders.Add(New DbFolder("down"))
		newMigrate.Folders(0).Files.Add("1.sql")
		newMigrate.Folders.Add(New DbFolder("up"))
		newMigrate.Folders(1).Files.Add("1.sql")

		dbProj.Save()
		_applicationObject.Solution.Open(slnFileName)
	End Sub

	Private Sub WriteOutput(ByVal text As String)
		OutputPane.Activate()
		OutputPane.OutputString(String.Concat(text, vbCrLf))
	End Sub
End Class

Imports System
'Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Lifetime
'Imports System.Runtime.Remoting.Channels
'Imports System.Runtime.Remoting.Channels.Tcp
'Imports System.Runtime.Serialization
'Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
'Imports System.Collections
Imports System.Reflection
'Imports System.Diagnostics
Imports System.Threading
Imports System.Text
Imports ICSharpCode.SharpZipLib.Zip

'
' Note: this project links in the #ZipLib library in binary form, and according to the license
' is freely redistributable under any licensing agreement.  See the #ZipLib license for detail
' on the binary linking exception. 
'
' #ZipLib has been developed by Mike Krueger (mike@icsharpcode.net)
' http:'icsharpcode.net/OpenSource/SharpZipLib/Default.aspx
'
Namespace Remoting.Common

    ''' <summary>
    ''' This server class is used to setup the Remoting server that will serve up the
    ''' assemblies as needed.  This class is in a common assembly so that it can be
    ''' referenced by the ExecRemote project and the ServiceOnRemoteMachine project.
    ''' </summary>
    Public Class Server
        Inherits MarshalByRefObject
        Implements IDisposable

#Region " Events "

        ''' <summary>
        ''' Event that is fired when the ServiceOnRemoteMachine declares that
        ''' the subprocess is done
        ''' </summary>
        Private m_isDone As New ManualResetEvent(False)

#End Region

#Region " Private variables "

        Private m_args As String() = New String() {}
        Private m_configContents As String = String.Empty
        Private m_ctrlEvents As Queue = Queue.Synchronized(New Queue(2))
        Private m_ctrlReady As New AutoResetEvent(False)
        Private m_debug As Integer = 0
        Private m_licenseFileContents As String = String.Empty
        Private m_numClients As Integer = 0
        Private m_outputFolder As String = String.Empty
        Private m_procExitCode As Integer = 0
        Private m_remotingRef As ObjRef = Nothing
        Private m_stderr As TextWriter = Console.[Error]
        Private m_stdin As TextReader = Console.[In]
        Private m_stdout As TextWriter = Console.Out
        Private m_unmanagedPath As String = String.Empty

#End Region

#Region " Properties "

        ''' <summary>
        ''' The arguments that will be passed to the Main() method of the entry
        ''' point of the assembly to run
        ''' </summary>
        Public Property Args() As String()
            Get
                Trace.WriteLineIf(DebugLevel > 0, String.Format("The client has requested the commmand line arguments: {0}", String.Join(" ", m_args)), "Debug")

                Return m_args
            End Get
            Set(ByVal value As String())
                If Not value Is Nothing Then
                    m_args = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' The configuration file contents used to erect an application domain (ie the contents of myApp.exe.config)
        ''' </summary>
        Public Property ConfigurationFileContents() As String
            Get
                Return m_configContents
            End Get
            Set(ByVal value As String)
                m_configContents = value
            End Set
        End Property

        ''' <summary>
        ''' The debugging level for this server
        ''' </summary>
        Public Property DebugLevel() As Integer
            Get
                Return m_debug
            End Get
            Set(ByVal value As Integer)
                m_debug = value
            End Set
        End Property

        ''' <summary>
        ''' The contents of the license file to use
        ''' </summary>
        Public Property LicenseFileContents() As String
            Get
                Return m_licenseFileContents
            End Get
            Set(ByVal value As String)
                m_licenseFileContents = value
            End Set
        End Property

        ''' <summary>
        ''' Designate the output folder for any modified files
        ''' </summary>
        Public Property OutputFolder() As String
            Get
                Return m_outputFolder
            End Get
            Set(ByVal value As String)
                If value = Nothing Then
                    m_outputFolder = Nothing
                Else
                    m_outputFolder = value.Trim()
                End If
            End Set
        End Property

        ''' <summary>
        ''' The exit code of the process on the remote machine.  This value is reported by
        ''' the ServiceOnRemoteMachine of the status of the remote process so that it may 
        ''' be used by the caller of this class.
        ''' </summary>
        Public Property RemoteProcessExitCode() As Integer

            Get
                Return m_procExitCode
            End Get
            Set(ByVal value As Integer)
                Trace.WriteLineIf(DebugLevel > 0, String.Format("RemoteProcessExitCode code was reported as {0}...", value), "Debug")
                m_procExitCode = value
            End Set
        End Property

        ''' <summary>
        ''' Retrieve StdErr from this process
        ''' </summary>
        Public Property StdErr() As TextWriter
            Get
                Return m_stderr
            End Get
            Set(ByVal value As TextWriter)
                m_stderr = value
            End Set
        End Property

        ''' <summary>
        ''' Retrieve StdIn from this process
        ''' </summary>
        Public Property StdIn() As TextReader
            Get
                Return m_stdin
            End Get
            Set(ByVal value As TextReader)
                m_stdin = value
            End Set
        End Property

        ''' <summary>
        ''' Retrieve StdOut from this process
        ''' </summary>
        Public Property StdOut() As TextWriter
            Get
                Return m_stdout
            End Get
            Set(ByVal value As TextWriter)
                m_stdout = value
            End Set
        End Property

        ''' <summary>
        ''' The path to use for the unmanaged dependencies
        ''' </summary>
        Public Property UnmanagedDependenciesPath() As String
            Get
                Return m_unmanagedPath
            End Get
            Set(ByVal value As String)
                m_unmanagedPath = value
            End Set
        End Property

        ''' <summary>
        ''' The Uri that can be used to access this object remotely
        ''' </summary>
        Public ReadOnly Property Uri() As String
            Get
                If Not m_remotingRef Is Nothing Then
                    Return m_remotingRef.URI
                Else
                    Return String.Empty
                End If
            End Get
        End Property

#End Region

#Region " Shared variables "

        Shared s_syncRoot As New Object()

#End Region

#Region " Functions "

        ''' <summary>
        ''' Method that is invoked when the ServiceOnRemoteMachine is done and the subprcess 
        ''' has exited.
        ''' </summary>
        ''' <returns>True if the process just finish, false if it had already finished</returns>
        Public Function Done() As Boolean

            Trace.WriteLineIf(DebugLevel > 0, String.Format("Server {0} has finished, exiting...", Me.Uri), "Debug")


            Dim nowHowMany As Integer = Interlocked.Decrement(m_numClients)


            If nowHowMany = 0 Then

                RemotingServices.Disconnect(Me)


                Return m_isDone.[Set]()
            End If

            Return False
        End Function

        ''' <summary>
        ''' Get the bytes for the specified assembly
        ''' </summary>
        ''' <param name="assemblyName">The full or partial name of the desired assembly</param>
        ''' <returns>The bytes of the assembly if it could be loaded, otherwise null</returns>
        Public Function GetAssembly(ByVal assemblyName As String) As Byte()
            Trace.WriteLineIf(DebugLevel > 0, String.Format("Received request for assembly {0}...", assemblyName), "Debug")

            Try
                'Dim assembly As Assembly = assembly.LoadWithPartialName(assemblyName)
                Dim assembly As Assembly = assembly.Load(assemblyName)

                Trace.WriteLineIf(DebugLevel > 0 AndAlso (Not assembly Is Nothing), String.Format("Successfully loaded assembly {0}...", assembly.FullName), "Debug")

                If assembly Is Nothing Then
                    Trace.WriteLine(String.Format("Unable to load the assembly {0}...", assemblyName), "Error")

                    Return Nothing
                End If

                Dim streams As FileStream() = assembly.GetFiles(True)

                Trace.WriteLineIf(DebugLevel > 0, String.Format("Found {0} files streams in assembly...", streams.Length), "Debug")

                Dim memStream As New MemoryStream()

                Dim numRead As Integer = 0
                Dim buff As Byte() = New Byte(256 * 1024) {}

                numRead = streams(0).Read(buff, 0, buff.Length)
                While numRead = buff.Length
                    memStream.Write(buff, 0, numRead)
                    numRead = streams(0).Read(buff, 0, buff.Length)
                End While

                If numRead > 0 Then
                    memStream.Write(buff, 0, numRead)
                End If

                Dim buffer As Byte() = memStream.GetBuffer()

                memStream.Close()
                streams(0).Close()

                Trace.WriteLineIf(DebugLevel > 0, String.Format("Copied assembly into {0} bytes...", buffer.Length), "Debug")

                Return buffer
            Catch exp As Exception
                Trace.WriteLine(String.Format("Unable to load assembly {0}: {1}" & Chr(10) & "{2}", assemblyName, exp.Message, exp.StackTrace), "Error")

                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Retrieve the DLL with the specified name on this machine, package up 
        ''' the bytes and send them over
        ''' </summary>
        ''' <param name="dllName">The name of the DLL to search for</param>
        ''' <returns>The bytes of the DLL if found, otherwise null</returns>
        Public Function GetDll(ByVal dllName As String) As Byte()
            If DebugLevel > 0 Then
                Trace.WriteLine(String.Format("A request was made for the DLL named {0}..", dllName), "Debug")
            End If


            Dim hModule As IntPtr = Common.Win32.GetModuleHandle(dllName)

            If hModule = IntPtr.Zero Then
                If DebugLevel > 0 Then
                    Trace.WriteLine(String.Format("The DLL named {0} was not found on this machine.", dllName), "Debug")
                End If

                Return Nothing
            End If

            Dim path As New StringBuilder(2048)


            Try
                Dim nameLength As UInteger = Common.Win32.GetModuleFileName(hModule, path, path.Length)


                path.Length = Convert.ToInt32(nameLength)
            Finally

                Common.Win32.FreeLibrary(hModule)
            End Try

            If DebugLevel > 0 Then
                Trace.WriteLine(String.Format("The DLL named {0} was found on this machine as {1}.", dllName, path.ToString()), "Debug")
            End If


            Dim stream As FileStream = Nothing

            Try
                stream = File.Open(path.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read)
            Catch exp As Exception
                Trace.WriteLine(String.Format("An error occured while attempting to open the file {0} : ", path.ToString()) + exp.Message + "" & Chr(10) & "" + exp.StackTrace, "Error")

                Return Nothing
            End Try

            Dim memStream As New MemoryStream()

            Dim numRead As Integer = 0
            Dim buff As Byte() = New Byte(256 * 1024) {}

            Try
                numRead = stream.Read(buff, 0, buff.Length)
                While numRead = buff.Length
                    memStream.Write(buff, 0, numRead)
                    numRead = stream.Read(buff, 0, buff.Length)
                End While
            Finally
                stream.Close()
            End Try

            ' Write the last chunk
            If numRead > 0 Then
                memStream.Write(buff, 0, numRead)
            End If

            Dim buffer As Byte() = memStream.GetBuffer()

            Return buffer

        End Function

        ''' <summary>
        ''' Get the time on the server relative to UTC
        ''' </summary>
        ''' <returns>The time in UTC</returns>
        Public Function GetTime() As DateTime
            Return DateTime.UtcNow
        End Function

        ''' <summary>
        ''' Get thre stream of the unmanaged resources
        ''' </summary>
        ''' <returns>The stream for the unmanaged resource, or null if none</returns>
        Public Function GetUnmanagedDependencies() As Stream

            If m_unmanagedPath = Nothing OrElse m_unmanagedPath = String.Empty OrElse File.Exists(m_unmanagedPath) = False Then
                Return Nothing
            End If

            Try
                Return File.Open(m_unmanagedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Catch exp As Exception
                Trace.WriteLine(exp.Message + "" & Chr(10) & "" + exp.StackTrace, "Error")
            End Try

            Return Nothing
        End Function

        Public Overloads Overrides Function InitializeLifetimeService() As Object
            Dim lease As ILease = DirectCast(MyBase.InitializeLifetimeService(), ILease)

            If Not lease Is Nothing AndAlso lease.CurrentState = LeaseState.Initial Then
                ' Set lease properties
                ' new TimeSpan( Timeout.Infinite ) ;
                ' lease.SponsorshipTimeout  = TimeSpan.Zero ; // new TimeSpan( Timeout.Infinite ) ;
                '				lease.RenewOnCallTime     = new TimeSpan( Timeout.Infinite ) ;
                lease.InitialLeaseTime = TimeSpan.Zero
            End If

            Return lease
        End Function

        ''' <summary>
        ''' Wait for the server to fire the event that declares that a CTRL event occured
        ''' </summary>
        ''' <returns>The CTRL signal</returns>
        Public Function WaitForCtrlEvent() As Win32.CtrlTypes
            SyncLock m_ctrlEvents.SyncRoot
                If m_ctrlEvents.Count > 0 Then
                    Return DirectCast(m_ctrlEvents.Dequeue(), Win32.CtrlTypes)
                End If
            End SyncLock

            ' Wait for an event 
            m_ctrlReady.WaitOne()

            Return DirectCast(m_ctrlEvents.Dequeue(), Win32.CtrlTypes)
        End Function

        ''' <summary>
        ''' Wait until the server is done serving up objects
        ''' </summary>
        ''' <param name="allowedtime">Allowed amount of time to run</param>
        ''' <returns>True if the server is done, false otherwise</returns>
        Public Function WaitUntilDone(ByVal allowedTime As TimeSpan) As Boolean
            Return m_isDone.WaitOne(allowedTime, False)
        End Function

#End Region

#Region " Subroutines "

        Protected Overrides Sub Finalize()
            Try
                Dispose(False)
            Finally
                MyBase.Finalize()
            End Try
        End Sub

        ''' <summary>
        ''' Compress the specified folder into the output file.
        ''' </summary>
        ''' <param name="folderToCompress">The folder to compress</param>
        ''' <param name="outputFile">The output file to place the zip contents in</param>
        ''' <param name="overwrite">True to overwrite any existing file, othwerwise false</param>
        ''' <param name="minModDate">The minimum modification date in order to include a file</param>
        ''' <param name="zipLevel">The zip compression level</param>
        Public Shared Sub CompressFolder(ByVal folderToCompress As String, ByVal outputFile As String, ByVal overwrite As Boolean, ByVal zipLevel As Integer, ByVal minModDate As DateTime)
            If Not overwrite AndAlso File.Exists(outputFile) Then
                Throw New ArgumentException(String.Format("The output file already exists: {0}", outputFile))
            End If

            If outputFile = Nothing Then
                Throw New ArgumentNullException("outputFile", "Output file is not allowed to be null")
            End If

            If folderToCompress = Nothing OrElse Directory.Exists(folderToCompress) = False Then
                Throw New ArgumentException(String.Format("The directory does not exist: {0}", folderToCompress))
            End If

            ' Compress all of the output files into a file to send back
            Dim fullOuputFile As String = Path.GetFullPath(outputFile).ToLower()

            Dim zipOutput As New ZipOutputStream(File.Open(fullOuputFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            zipOutput.SetLevel(zipLevel)

            Dim compressPath As String = Path.GetFullPath(folderToCompress)
            Dim folders As New Queue()
            Dim zipBuff As Byte() = New Byte(8 * 1024) {}

            folders.Enqueue(compressPath)

            Try
                While folders.Count > 0
                    Dim folder As String = TryCast(folders.Dequeue(), String)

                    If folder = Nothing OrElse folder = String.Empty Then
                        Continue While
                    End If

                    Try
                        ' Store all the subfolders
                        For Each subfolder As String In Directory.GetDirectories(folder)
                            folders.Enqueue(Path.GetFullPath(subfolder))
                        Next

                        ' Store the current folder in the zip file
                        If String.Compare(folder, compressPath, True) <> 0 Then
                            Dim folderEntry As New ZipEntry(ZipEntry.CleanName(folder.Substring(compressPath.Length)) + "/")

                            zipOutput.PutNextEntry(folderEntry)
                        End If

                        ' Store all of the files
                        For Each filePath As String In Directory.GetFiles(folder)
                            Dim fullFilePath As String = Path.Combine(folder, filePath)

                            ' Don't store the zip file we're creating
                            If fullOuputFile = fullFilePath.ToLower() Then
                                Continue For
                            End If

                            ' Don't store files that don't meet the minimum criteria
                            If File.GetLastWriteTimeUtc(fullFilePath) <= minModDate Then
                                Continue For
                            End If

                            Try
                                Dim stream As FileStream = File.Open(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

                                Try
                                    Dim fileEntry As New ZipEntry(ZipEntry.CleanName(fullFilePath.Substring(compressPath.Length)))

                                    zipOutput.PutNextEntry(fileEntry)

                                    Dim bytesRead As Integer = 0

                                    ' Compress the contents of the file
                                    Do
                                        bytesRead = stream.Read(zipBuff, 0, zipBuff.Length)

                                        If bytesRead > 0 Then
                                            zipOutput.Write(zipBuff, 0, zipBuff.Length)
                                        End If
                                    Loop While bytesRead > 0
                                Finally
                                    If Not stream Is Nothing Then
                                        stream.Close()
                                    End If
                                End Try
                            Catch exp As Exception
                                Trace.WriteLine(exp.Message + "" & Chr(10) & "" + exp.StackTrace, "Error")
                            End Try
                        Next
                    Catch exp As Exception
                        Trace.WriteLine(exp.Message + "" & Chr(10) & "" + exp.StackTrace, "Error")
                    End Try
                End While
            Finally
                If Not zipOutput Is Nothing Then
                    zipOutput.Finish()
                    zipOutput.Close()
                End If
            End Try

            Return
        End Sub

        ''' <summary>
        ''' The callback method for the control events from the console
        ''' </summary>
        ''' <param name="CtrlEvent">The CTRL event that occured</param>
        Public Sub ControlEvent(ByVal ctrlEvent As Win32.CtrlTypes)
            ' Add this event to the list of events
            m_ctrlEvents.Enqueue(ctrlEvent)

            m_ctrlReady.[Set]()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
            Dispose(True)
        End Sub

        Public Sub Dispose(ByVal disposing As Boolean)
            SyncLock Me
                If Not m_remotingRef Is Nothing Then
                    RemotingServices.Disconnect(Me)
                End If
                m_remotingRef = Nothing
            End SyncLock
        End Sub

        ''' <summary>
        ''' Note:  this main method is only used for debugging purposes and serves no other use
        ''' </summary>
        ''' <param name="args"></param>
        Public Shared Sub Main(ByVal args As String())
            Dim server As New Server()
            server.Start(1, Guid.NewGuid().ToString())
        End Sub

        ''' <summary>
        ''' Response method from the remote machine that the output contents are ready for processing
        ''' </summary>
        ''' <param name="reader">The stream to read the output contents from</param>
        Public Sub OutputContentsReady(ByVal reader As Stream)

            If reader Is Nothing OrElse m_outputFolder = Nothing Then
                Return
            End If

            If m_outputFolder <> String.Empty Then
                If Directory.Exists(m_outputFolder) = False Then
                    Directory.CreateDirectory(m_outputFolder)
                End If
            End If

            Dim input As New ZipInputStream(reader)

            Dim parentPath As String = Environment.CurrentDirectory

            If m_outputFolder <> Nothing AndAlso m_outputFolder <> String.Empty Then
                parentPath = Path.GetFullPath(m_outputFolder)
            End If


            If DebugLevel > 0 Then
                Trace.WriteLine(String.Format("Unloading output contents into folder {0}", m_outputFolder), "Debug")
            End If

            Dim buff As Byte() = New Byte(8 * 1024) {}

            Try
                Dim entry As ZipEntry = input.GetNextEntry()
                While Not entry Is Nothing

                    Try
                        If entry.IsDirectory Then

                            Dim fullPath As String = Path.Combine(parentPath, entry.Name)

                            If Directory.Exists(fullPath) = False AndAlso File.Exists(fullPath) = False Then
                                Try
                                    Directory.CreateDirectory(fullPath)
                                Catch exp As Exception
                                    Trace.WriteLine(String.Format("An error occured while attempting to " + "create directory {0}: {1}" & Chr(10) & "{2}", fullPath, exp.Message, exp.StackTrace), "Error")
                                End Try
                            End If
                        Else
                            Dim fullFilePath As String = Path.Combine(parentPath, entry.Name)

                            Try

                                Dim output As FileStream = File.Open(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)

                                Try
                                    Dim numRead As Integer = 0

                                    Do
                                        numRead = input.Read(buff, 0, buff.Length)

                                        If numRead > 0 Then
                                            output.Write(buff, 0, numRead)
                                        End If
                                    Loop While numRead > 0
                                Finally
                                    output.Close()
                                End Try
                            Catch exp As Exception
                                Trace.WriteLine(String.Format("An error occured while attempting to " + "create the file {0}: {1}" & Chr(10) & "{2}", fullFilePath, exp.Message, exp.StackTrace), "Error")
                            End Try
                        End If
                    Catch exp As Exception
                        Trace.WriteLine(exp.Message + "" & Chr(10) & "" + exp.StackTrace, "Error")
                    End Try
                    entry = input.GetNextEntry()
                End While
            Finally
                input.Close()
            End Try
        End Sub

        ''' <summary>
        ''' Register the remoting process and start listening for objects
        ''' </summary>
        ''' <param name="numberOfClientsToWaitFor">Number of clients to wait for the done flag</param>
        Public Sub Start(ByVal numberOfClientsToWaitFor As Integer, ByVal uniqueId As String)
            m_numClients = numberOfClientsToWaitFor

            If Not m_remotingRef Is Nothing Then
                RemotingServices.Disconnect(Me)
            End If

            m_remotingRef = Nothing

            m_isDone.Reset()

            If File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile) Then
                RemotingConfiguration.Configure(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, False)
            End If

            ' tcpUri,
            m_remotingRef = RemotingServices.Marshal(Me, String.Format("{0}/ExecRemote.rem", uniqueId), GetType(Server))

            ' Keep the Server running until the user presses enter
            Trace.WriteLineIf(DebugLevel > 0, String.Format("The server is up and running on uri {0}...", m_remotingRef.URI), "Debug")
        End Sub

#End Region

    End Class

End Namespace

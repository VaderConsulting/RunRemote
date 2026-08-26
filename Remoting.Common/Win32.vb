Imports System
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Remoting.Common

    ''' <summary>
    '''   This is basically a shell class for the Win32 API methods.  To the best of my 
    ''' knowledge, there is no way to create or delete services from the managed framework.
    ''' I really don't understand why this wasn't implemented in the ServiceController, and
    ''' if anyone has an explanation, please email me.
    ''' 
    '''   The best resource for using any of these functions is http://www.pinvoke.net .  
    ''' The gentleman that runs the site is quite dilligent and has even supplied a web
    ''' service plugin for Visual Studio that will look up PInvoke methods.  I've published
    ''' all of the classes and enumerations below to that site.
    ''' </summary>
    Public Class Win32

#Region " Delegates "

        ' A delegate type to be used as the handler routine for SetConsoleCtrlHandler.
        Public Delegate Function ConsoleCtrlDelegate(ByVal CtrlType As CtrlTypes) As Boolean

        Public Delegate Sub ControlEventDelegate(ByVal CtrlType As Win32.CtrlTypes)

#End Region

#Region " Enum's "

        ' An enumerated type for the control messages sent to the handler routine.
        Public Enum CtrlTypes
            CTRL_C_EVENT = 0
            CTRL_BREAK_EVENT
            CTRL_CLOSE_EVENT
            CTRL_LOGOFF_EVENT = 5
            CTRL_SHUTDOWN_EVENT
        End Enum

        Public Enum SC_ERROR_CONTROL As Integer
            SERVICE_ERROR_IGNORE = 0
            SERVICE_ERROR_NORMAL = 1
            SERVICE_ERROR_SEVERE = 2
            SERVICE_ERROR_CRITICAL = 3
        End Enum

        <Flags()> _
        Public Enum SC_MANAGER_ACCESS As UInteger
            CONNECT = 1
            CREATE_SERVICE = 2
            ENUMERATE_SERVICE = 4
            LOCK = 8
            QUERY_LOCK_STATUS = 16
            MODIFY_BOOT_CONFIG = 32
            STANDARD_RIGHTS_REQUIRED = 983040
            GENERIC_READ = 2147483648
            GENERIC_WRITE = 1073741824
            GENERIC_EXECUTE = 536870912
            GENERIC_ALL = 268435456
            ALL_ACCESS = STANDARD_RIGHTS_REQUIRED Or CONNECT Or CREATE_SERVICE Or ENUMERATE_SERVICE Or LOCK Or QUERY_LOCK_STATUS Or MODIFY_BOOT_CONFIG
        End Enum

        <Flags()> _
        Public Enum SC_SERVICE_TYPE As UInteger
            KERNEL_DRIVER = 1
            FILE_SYSTEM_DRIVER = 2
            ADAPTER = 4
            RECOGNIZER_DRIVER = 8
            DRIVER = (KERNEL_DRIVER Or FILE_SYSTEM_DRIVER Or RECOGNIZER_DRIVER)
            WIN32_OWN_PROCESS = 16
            WIN32_SHARE_PROCESS = 32
            WIN32 = (WIN32_OWN_PROCESS Or WIN32_SHARE_PROCESS)
            INTERACTIVE_PROCESS = 256
            TYPE_ALL = (Win32 Or ADAPTER Or DRIVER Or INTERACTIVE_PROCESS)
        End Enum

        Public Enum SC_START_TYPE As Integer
            SERVICE_BOOT_START = 0
            SERVICE_SYSTEM_START = 1
            SERVICE_AUTO_START = 2
            SERVICE_DEMAND_START = 3
            SERVICE_DISABLED = 4
        End Enum

        <Flags()> _
        Public Enum SERVICE_ACCEPT As UInteger
            [STOP] = 1
            PAUSE_CONTINUE = 2
            SHUTDOWN = 4
            PARAMCHANGE = 8
            NETBINDCHANGE = 16
            HARDWAREPROFILECHANGE = 32
            POWEREVENT = 64
            SESSIONCHANGE = 128
        End Enum

        <Flags()> _
        Public Enum SERVICE_CONTROL As UInteger
            [STOP] = 1
            PAUSE = 2
            [CONTINUE] = 3
            INTERROGATE = 4
            SHUTDOWN = 5
            PARAMCHANGE = 6
            NETBINDADD = 7
            NETBINDREMOVE = 8
            NETBINDENABLE = 9
            NETBINDDISABLE = 10
            DEVICEEVENT = 11
            HARDWAREPROFILECHANGE = 12
            POWEREVENT = 13
            SESSIONCHANGE = 14
        End Enum

        Public Enum SERVICE_STATE As UInteger
            SERVICE_STOPPED = 1
            SERVICE_START_PENDING = 2
            SERVICE_STOP_PENDING = 3
            SERVICE_RUNNING = 4
            SERVICE_CONTINUE_PENDING = 5
            SERVICE_PAUSE_PENDING = 6
            SERVICE_PAUSED = 7
        End Enum

#End Region

#Region " Functions "

        <DllImport("kernel32.dll", SetLastError:=True)> _
        Public Shared Function AllocConsole() As Boolean
        End Function

        <DllImport("advapi32")> _
         Public Shared Function CloseServiceHandle(ByVal hSCObject As IntPtr) As Boolean
        End Function

        <DllImport("advapi32")> _
        Public Shared Function ControlService(ByVal hService As IntPtr, ByVal dwControl As SERVICE_CONTROL, ByRef lpServiceStatus As SERVICE_STATUS) As Boolean
        End Function

        ' Make sure to double quote, and double escape
        ' Use null for no group
        ' Use null for no tag
        ' Use null for no dependencies
        ' Use DOMAIN\USER
        <DllImport("advapi32")> _
        Public Shared Function CreateService(ByVal hSCManager As IntPtr, ByVal lpServiceName As String, ByVal lpDisplayName As String, ByVal dwDesiredAccess As SC_MANAGER_ACCESS, ByVal dwServiceType As SC_SERVICE_TYPE, ByVal dwStartType As SC_START_TYPE, _
         ByVal dwErrorControl As SC_ERROR_CONTROL, ByVal lpBinaryPathName As String, ByVal lpLoadOrderGroup As String, ByVal lpdwTagId As IntPtr, ByVal lpDependencies As String, ByVal lpServiceStartName As String, _
         ByVal lpPassword As String) As IntPtr
        End Function

        <DllImport("advapi32")> _
        Public Shared Function DeleteService(ByVal hService As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll")> _
        Public Shared Function FreeLibrary(ByVal hModule As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)> _
        Public Shared Function GenerateConsoleCtrlEvent(ByVal dwCtrlEvent As CtrlTypes, ByVal dwProcessGroupId As Integer) As Boolean
        End Function

        <DllImport("kernel32.dll")> _
        Public Shared Function GetModuleFileName(<[In]()> ByVal hModule As IntPtr, <Out()> _
        ByVal lpFilename As StringBuilder, <MarshalAs(UnmanagedType.U4)> <[In]()> ByVal nSize As Integer) As UInteger
        End Function

        <DllImport("kernel32.dll")> _
        Public Shared Function GetModuleHandle(ByVal lpModuleName As String) As IntPtr
        End Function

        ' Null for current machine
        ' Null for active database
        <DllImport("advapi32")> _
        Public Shared Function OpenSCManager(ByVal lpMachineName As String, ByVal lpDatabaseName As String, ByVal dwDesiredAccess As SC_MANAGER_ACCESS) As IntPtr
        End Function

        <DllImport("advapi32")> _
        Public Shared Function OpenService(ByVal hSCManager As IntPtr, ByVal lpServiceName As String, ByVal dwDesiredAccess As SC_MANAGER_ACCESS) As IntPtr
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)> _
        Public Shared Function SetConsoleCtrlHandler(ByVal HandlerRoutine As ConsoleCtrlDelegate, ByVal Add As Boolean) As Boolean
        End Function

        <DllImport("advapi32")> _
        Public Shared Function StartService(ByVal hService As IntPtr, ByVal dwNumServiceArgs As Integer, ByVal lpServiceArgVectors As IntPtr) As Boolean
        End Function

#End Region

#Region " Structures "

        <StructLayout(LayoutKind.Sequential)> _
        Public Structure SERVICE_STATUS
            Private dwServiceType As SC_SERVICE_TYPE
            Private dwCurrentState As SERVICE_STATE
            Private dwControlsAccepted As SERVICE_ACCEPT
            Private dwWin32ExitCode As Integer
            Private dwServiceSpecificExitCode As Integer
            Private dwCheckPoint As UInteger
            Private dwWaitHint As UInteger
        End Structure

#End Region

    End Class

End Namespace



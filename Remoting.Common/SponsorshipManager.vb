Imports System
Imports System.Runtime.Remoting.Lifetime
Imports System.Runtime.Remoting
'Imports System.Runtime.Remoting.Proxies
Imports System.Runtime.Remoting.Channels
'Imports System.Collections
'Imports System.Diagnostics

Namespace Remoting.Common

    ''' <summary>
    ''' This code was derived from http://msdn.microsoft.com/msdnmag/issues/03/12/LeaseManager/default.aspx
    ''' as well as http://www.thinktecture.com/Resources/RemotingFAQ/GETMARSHALBYREFOBJECTSURL.html
    ''' </summary>
    Public Class SponsorshipManager
        Inherits MarshalByRefObject
        Implements ISponsor
        Implements IDisposable

        Private m_LeaseList As IList

        Public Sub New()
            m_LeaseList = New ArrayList()
        End Sub

        Protected Overrides Sub Finalize()
            Try
                Dispose(False)
            Finally
                MyBase.Finalize()
            End Try
        End Sub

        ''' <summary>
        ''' Get the Uri for the MarshalByRefObject
        ''' </summary>
        ''' <param name="obj">The non-null object to retrieve the reference for</param>
        ''' <returns>The Uri for the object if it is remoted, otherwise null</returns>
        Public Shared Function GetURLForObject(ByVal obj As MarshalByRefObject) As String
            If obj Is Nothing Then
                Return Nothing
            End If
            ' trying for CAOs
            Dim o As ObjRef = RemotingServices.GetObjRefForProxy(obj)
            If Not o Is Nothing Then
                For Each data As Object In o.ChannelInfo.ChannelData
                    Dim ds As ChannelDataStore = TryCast(data, ChannelDataStore)
                    If Not ds Is Nothing Then
                        Return ds.ChannelUris(0) + o.URI
                    End If
                Next
            Else
                ' either SAO or not remote!
                Dim URL As String = RemotingServices.GetObjectUri(obj)
                Return URL
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' This method will be called by the remoting services to determine if the object is still alive
        ''' </summary>
        ''' <param name="lease"></param>
        ''' <returns></returns>
        Public Function Renewal(ByVal lease As ILease) As TimeSpan Implements ISponsor.Renewal
            Debug.Assert(lease.CurrentState = LeaseState.Active)
            Return lease.InitialLeaseTime
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
            Dispose(True)
        End Sub

        Public Sub Dispose(ByVal disposing As Boolean)
            UnregisterAll()
        End Sub

        Public Sub OnExit(ByVal sender As Object, ByVal e As EventArgs)
            UnregisterAll()
        End Sub

        ''' <summary>
        ''' Register an object to be sponsored by this manager
        ''' </summary>
        ''' <param name="obj">The object to be sponsored</param>
        Public Sub Register(ByVal obj As MarshalByRefObject)
            ' The line below is simply used for debugging purposes
            Dim uriForObj As String = GetURLForObject(obj)
            Dim lease As ILease = DirectCast(RemotingServices.GetLifetimeService(obj), ILease)
            If Not lease Is Nothing Then
                If lease.CurrentState = LeaseState.Active Then
                    lease.Register(Me)
                    SyncLock Me
                        m_LeaseList.Add(lease)
                    End SyncLock
                End If
            End If
        End Sub

        Public Sub Unregister(ByVal obj As MarshalByRefObject)
            Dim lease As ILease = DirectCast(RemotingServices.GetLifetimeService(obj), ILease)
            Debug.Assert(lease.CurrentState = LeaseState.Active)
            lease.Unregister(Me)
            SyncLock Me
                m_LeaseList.Remove(lease)
            End SyncLock
        End Sub

        Public Sub UnregisterAll()
            SyncLock Me
                Dim index As Integer = m_LeaseList.Count - 1
                While m_LeaseList.Count > 0
                    Dim lease As ILease = DirectCast(m_LeaseList(index), ILease)
                    lease.Unregister(Me)
                    m_LeaseList.RemoveAt(index)
                    System.Math.Max(System.Threading.Interlocked.Decrement(index), index + 1)
                End While
            End SyncLock
        End Sub

    End Class

End Namespace

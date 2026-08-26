Imports System

Namespace Remoting.Common

    ''' <summary>Implements a basic command-line switch by taking the
    ''' switching name and the associated description.</summary>
    ''' <remark>Only currently is implemented for properties, so all
    ''' auto-switching variables should have a get/set method supplied.</remark>
    <AttributeUsage(AttributeTargets.Property)> _
    Public Class CommandLineSwitchAttribute
        Inherits System.Attribute

#Region " Private Variables "

        Private m_name As String = ""
        Private m_description As String = ""

#End Region

#Region " Public Properties "

        ''' <summary>Accessor for retrieving the switch-name for an associated
        ''' property.</summary>
        Public ReadOnly Property Name() As String
            Get
                Return m_name
            End Get
        End Property

        ''' <summary>Accessor for retrieving the description for a switch of
        ''' an associated property.</summary>
        Public ReadOnly Property Description() As String
            Get
                Return m_description
            End Get
        End Property

#End Region

    End Class

    ''' <summary>
    ''' This class implements an alias attribute to work in conjunction
    ''' with the <see cref="CommandLineSwitchAttribute">CommandLineSwitchAttribute</see>
    ''' attribute.  If the CommandLineSwitchAttribute exists, then this attribute
    ''' defines an alias for it.
    ''' </summary>
    <AttributeUsage(AttributeTargets.[Property])> _
    Public Class CommandLineAliasAttribute
        Inherits System.Attribute

#Region " Private Variables "

        Protected m_Alias As String = ""

#End Region

#Region " Public Properties "

        Public ReadOnly Property [Alias]() As String
            Get
                Return m_Alias
            End Get
        End Property

#End Region

#Region " Constructors "

        Public Sub New(ByVal [alias] As String)
            m_Alias = [alias]
        End Sub

#End Region

    End Class

End Namespace



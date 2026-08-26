Imports System
Imports System.Text.RegularExpressions

Namespace Remoting.Common
    ''' <summary>Implementation of a command-line parsing class.  Is capable of
    ''' having switches registered with it directly or can examine a registered
    ''' class for any properties with the appropriate attributes appended to
    ''' them.</summary>
    ''' 
    Public Class Parser

        ''' <summary>A simple internal class for passing back to the caller
        ''' some information about the switch.  The internals/implementation
        ''' of this class has privileged access to the contents of the
        ''' SwitchRecord class.</summary>
        Public Class SwitchInfo

#Region " Private Variables "

            Private m_Switch As Object = Nothing

#End Region

#Region " Public Properties "

            Public ReadOnly Property Name() As String
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Name
                End Get
            End Property

            Public ReadOnly Property Description() As String
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Description
                End Get
            End Property

            Public ReadOnly Property Aliases() As String()
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Aliases
                End Get
            End Property

            Public ReadOnly Property Type() As System.Type
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Type
                End Get
            End Property

            Public ReadOnly Property Value() As Object
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Value
                End Get
            End Property

            Public ReadOnly Property InternalValue() As Object
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).InternalValue
                End Get
            End Property

            Public ReadOnly Property IsEnum() As Boolean
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Type.IsEnum
                End Get
            End Property

            Public ReadOnly Property Enumerations() As String()
                Get
                    Return (TryCast(m_Switch, SwitchRecord)).Enumerations
                End Get
            End Property

#End Region

            ''' <summary>
            ''' Constructor for the SwitchInfo class.  Note, in order to hide to the outside world
            ''' information not necessary to know, the constructor takes a System.Object (aka
            ''' object) as it's registering type.  If the type isn't of the correct type, an exception
            ''' is thrown.
            ''' </summary>
            ''' <param name="rec">The SwitchRecord for which this class store information.</param>
            ''' <exception cref="ArgumentException">Thrown if the rec parameter is not of
            ''' the type SwitchRecord.</exception>
            Public Sub New(ByVal rec As Object)
                If TypeOf rec Is SwitchRecord Then
                    m_Switch = rec
                Else
                    Throw New ArgumentException()
                End If
            End Sub

        End Class

        ''' <summary>
        ''' The SwitchRecord is stored within the parser's collection of registered
        ''' switches.  This class is private to the outside world.
        ''' </summary>
        Private Class SwitchRecord

#Region " Private Variables "

            Private m_name As String = ""
            Private m_description As String = ""
            Private m_value As Object = Nothing
            Private m_switchType As System.Type = GetType(Boolean)
            Private m_Aliases As System.Collections.ArrayList = Nothing
            Private m_Pattern As String = ""

            ' The following advanced functions allow for callbacks to be
            ' made to manipulate the associated data type.
            Private m_SetMethod As System.Reflection.MethodInfo = Nothing
            Private m_GetMethod As System.Reflection.MethodInfo = Nothing
            Private m_PropertyOwner As Object = Nothing

#End Region

#Region " Private Utility Functions "

            Private Sub Initialize(ByVal name As String, ByVal description As String)
                m_name = name
                m_description = description

                BuildPattern()
            End Sub

            Private Sub BuildPattern()
                Dim matchString As String = Name

                If Not Aliases Is Nothing AndAlso Aliases.Length > 0 Then
                    For Each s As String In Aliases
                        matchString += "|" + s
                    Next
                End If

                Dim strPatternStart As String = "(\s|^)""?(?<match>(-{1,2}|/)("
                Dim strPatternEnd As String
                ' To be defined below.
                ' The common suffix ensures that the switches are followed by
                ' a white-space OR the end of the string.  This will stop
                ' switches such as /help matching /helpme
                '
                Dim strCommonSuffix As String = "(?=(\s|$))"

                If Type = GetType(Boolean) Then
                    strPatternEnd = ")""?(?<value>(\+|-){0,1}))"
                ElseIf Type = GetType(String) Then
                    strPatternEnd = ")(?::|""?\s+))((""(?<value>.+?)"")|(?<value>\S+))"
                ElseIf Type = GetType(Integer) Then
                    strPatternEnd = ")(?::|""?\s+))((?<value>(-|\+)[0-9]+)|(?<value>[0-9]+))"
                ElseIf Type.IsEnum Then
                    '				else if ( Type == typeof(string) )
                    '					strPatternEnd = @")(?::|\s+))((?:"")(?<value>.+)(?:"")|(?<value>\S+))";
                    ' TODO: Suggested fix
                    Dim enumNames As String() = Enumerations
                    Dim e_str As String = enumNames(0)
                    Dim e As Integer = 1
                    While e < enumNames.Length
                        e_str += "|" + enumNames(e)
                        System.Math.Max(System.Threading.Interlocked.Increment(e), e - 1)
                    End While


                    strPatternEnd = ")(?::|\s+))(?<value>" + e_str + ")"
                Else
                    Throw New System.ArgumentException()
                End If

                ' Set the internal regular expression pattern.
                m_Pattern = strPatternStart + matchString + strPatternEnd + strCommonSuffix
            End Sub
#End Region

#Region " Public Properties "

            Public ReadOnly Property Value() As Object
                Get
                    If ReadValue <> Nothing Then
                        Return ReadValue
                    Else
                        Return m_value
                    End If
                End Get
            End Property

            Public ReadOnly Property InternalValue() As Object
                Get
                    Return m_value
                End Get
            End Property

            Public Property Name() As String
                Get
                    Return m_name
                End Get
                Set(ByVal value As String)
                    m_name = value
                End Set
            End Property

            Public Property Description() As String
                Get
                    Return m_description
                End Get
                Set(ByVal value As String)
                    m_description = value
                End Set
            End Property

            Public ReadOnly Property Type() As System.Type
                Get
                    Return m_switchType
                End Get
            End Property

            Public ReadOnly Property Aliases() As String()
                Get
                    Return IIf((Not m_Aliases Is Nothing), DirectCast(m_Aliases.ToArray(GetType(String)), String()), Nothing)
                End Get
            End Property

            Public ReadOnly Property Pattern() As String
                Get
                    Return m_Pattern
                End Get
            End Property

            Public WriteOnly Property SetMethod() As System.Reflection.MethodInfo
                Set(ByVal value As System.Reflection.MethodInfo)
                    m_SetMethod = value
                End Set
            End Property

            Public WriteOnly Property GetMethod() As System.Reflection.MethodInfo
                Set(ByVal value As System.Reflection.MethodInfo)
                    m_GetMethod = value
                End Set
            End Property

            Public WriteOnly Property PropertyOwner() As Object
                Set(ByVal value As Object)
                    m_PropertyOwner = value
                End Set
            End Property

            Public ReadOnly Property ReadValue() As Object
                Get
                    Dim o As Object = Nothing
                    If Not m_PropertyOwner Is Nothing AndAlso (Not m_GetMethod Is Nothing) Then
                        o = m_GetMethod.Invoke(m_PropertyOwner, Nothing)
                    End If
                    Return o
                End Get
            End Property

            Public ReadOnly Property Enumerations() As String()
                Get
                    If m_switchType.IsEnum Then
                        Return System.[Enum].GetNames(m_switchType)
                    Else
                        Return Nothing
                    End If
                End Get
            End Property

#End Region

#Region " Constructors "

            Public Sub New(ByVal name As String, ByVal description As String)
                Initialize(name, description)
            End Sub

            Public Sub New(ByVal name As String, ByVal description As String, ByVal type As System.Type)
                If type = GetType(Boolean) OrElse type = GetType(String) OrElse type = GetType(Integer) OrElse type.IsEnum Then
                    m_switchType = type
                    Initialize(name, description)
                Else
                    Throw New ArgumentException("Currently only Ints, Bool and Strings are supported")
                End If
            End Sub

#End Region

#Region " Public Methods "

            Public Sub AddAlias(ByVal [alias] As String)
                If m_Aliases Is Nothing Then
                    m_Aliases = New System.Collections.ArrayList()
                End If
                m_Aliases.Add([alias])

                BuildPattern()
            End Sub

            Public Sub Notify(ByVal value As Object)
                If Not m_PropertyOwner Is Nothing AndAlso Not m_SetMethod Is Nothing Then
                    Dim parameters As Object() = New Object(1) {}
                    parameters(0) = value
                    m_SetMethod.Invoke(m_PropertyOwner, parameters)
                End If
                m_value = value
            End Sub

#End Region

        End Class

#Region " Private Variables "

        Private m_commandLine As String = ""
        Private m_workingString As String = ""
        Private m_applicationName As String = ""
        Private m_splitParameters As String() = Nothing
        Private m_switches As System.Collections.ArrayList = Nothing

#End Region

#Region " Private Utility Functions "

        Private Sub ExtractApplicationName()
            '			Regex r = new Regex(@"^(?<commandLine>("".+""|(\S)+))(?<remainder>.+)",
            '				RegexOptions.ExplicitCapture);
            Dim r As New Regex("^((""(?<commandLine>.+?)"")|(?<commandLine>\S+))(?<remainder>.+)", RegexOptions.ExplicitCapture)
            Dim m As Match = r.Match(m_commandLine)
            If Not m Is Nothing AndAlso Not m.Groups("commandLine") Is Nothing Then
                m_applicationName = m.Groups("commandLine").Value
                m_workingString = m.Groups("remainder").Value
            End If
        End Sub

        Private Sub SplitParameters()
            ' Populate the split parameters array with the remaining parameters.
            ' Note that if quotes are used, the quotes are removed.
            ' e.g.   one two three "four five six"
            '						0 - one
            '						1 - two
            '						2 - three
            '						3 - four five six
            ' (e.g. 3 is not in quotes).
            Dim r As New Regex("((\s*(""(?<param>.+?)""|(?<param>\S+))))", RegexOptions.ExplicitCapture)
            Dim m As MatchCollection = r.Matches(m_workingString)

            If Not m Is Nothing Then
                m_splitParameters = New String(m.Count) {}
                Dim i As Integer = 0
                While i < m.Count
                    m_splitParameters(i) = m(i).Groups("param").Value
                    System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                End While
            End If
        End Sub

        Private Sub HandleSwitches()
            If Not m_switches Is Nothing Then
                For Each s As SwitchRecord In m_switches
                    Dim r As New Regex(s.Pattern, RegexOptions.ExplicitCapture Or RegexOptions.IgnoreCase)
                    Dim m As MatchCollection = r.Matches(m_workingString)
                    If Not m Is Nothing Then
                        Dim i As Integer = 0
                        While i < m.Count
                            Dim value As String = Nothing
                            If Not m(i).Groups Is Nothing AndAlso Not m(i).Groups("value") Is Nothing Then
                                value = m(i).Groups("value").Value
                            End If

                            If s.Type = GetType(Boolean) Then
                                Dim state As Boolean = True
                                ' The value string may indicate what value we want.
                                If Not m(i).Groups Is Nothing AndAlso Not m(i).Groups("value") Is Nothing Then
                                    Select Case value
                                        Case "+"
                                            state = True
                                            Exit Select
                                        Case "-"
                                            state = False
                                            Exit Select
                                        Case ""
                                            If s.ReadValue <> Nothing Then
                                                state = Not DirectCast(s.ReadValue, Boolean)
                                            End If
                                            Exit Select
                                        Case Else
                                            Exit Select
                                    End Select
                                End If
                                s.Notify(state)
                                Exit While
                            ElseIf s.Type = GetType(String) Then
                                s.Notify(value)
                            ElseIf s.Type = GetType(Integer) Then
                                s.Notify(Integer.Parse(value))
                            ElseIf s.Type.IsEnum Then
                                s.Notify(System.[Enum].Parse(s.Type, value, True))
                            End If
                            System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                        End While
                    End If

                    m_workingString = r.Replace(m_workingString, " ")
                Next
            End If
        End Sub

#End Region

#Region " Public Properties "

        Public ReadOnly Property ApplicationName() As String
            Get
                Return m_applicationName
            End Get
        End Property

        Public ReadOnly Property Parameters() As String()
            Get
                Return m_splitParameters
            End Get
        End Property

        Public ReadOnly Property Switches() As SwitchInfo()
            Get
                If m_switches Is Nothing Then
                    Return Nothing
                Else
                    Dim si As SwitchInfo() = New SwitchInfo(m_switches.Count) {}
                    Dim i As Integer = 0
                    While i < m_switches.Count
                        si(i) = New SwitchInfo(m_switches(i))
                        System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                    End While
                    Return si
                End If
            End Get
        End Property

        Default Public ReadOnly Property Item(ByVal name As String) As Object
            Get
                If Not m_switches Is Nothing Then
                    Dim i As Integer = 0
                    While i < m_switches.Count
                        If String.Compare((TryCast(m_switches(i), SwitchRecord)).Name, name, True) = 0 Then
                            Return (TryCast(m_switches(i), SwitchRecord)).Value
                        End If
                        System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                    End While
                End If
                Return Nothing
            End Get
        End Property

        ''' <summary>This function returns a list of the unhandled switches
        ''' that the parser has seen, but not processed.</summary>
        ''' <remark>The unhandled switches are not removed from the remainder
        ''' of the command-line.</remark>
        Public ReadOnly Property UnhandledSwitches() As String()
            Get
                Dim switchPattern As String = "(\s|^)(?<match>(-{1,2}|/)(.+?))(?=(\s|$))"
                Dim r As New Regex(switchPattern, RegexOptions.ExplicitCapture Or RegexOptions.IgnoreCase)
                Dim m As MatchCollection = r.Matches(m_workingString)

                If Not m Is Nothing Then
                    Dim unhandled As String() = New String(m.Count) {}
                    Dim i As Integer = 0
                    While i < m.Count
                        unhandled(i) = m(i).Groups("match").Value
                        System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                    End While
                    Return unhandled
                Else
                    Return Nothing
                End If
            End Get
        End Property

#End Region

#Region " Public Methods "

        Public Sub AddSwitch(ByVal name As String, ByVal description As String)
            If m_switches Is Nothing Then
                m_switches = New System.Collections.ArrayList()
            End If

            Dim rec As New SwitchRecord(name, description)
            m_switches.Add(rec)
        End Sub

        Public Sub AddSwitch(ByVal names As String(), ByVal description As String)
            If m_switches Is Nothing Then
                m_switches = New System.Collections.ArrayList()
            End If
            Dim rec As New SwitchRecord(names(0), description)
            Dim s As Integer = 1
            While s < names.Length
                rec.AddAlias(names(s))
                System.Math.Max(System.Threading.Interlocked.Increment(s), s - 1)
            End While
            m_switches.Add(rec)
        End Sub

        Public Function Parse() As Boolean
            ExtractApplicationName()

            ' Remove switches and associated info.
            HandleSwitches()

            ' Split parameters.
            SplitParameters()

            Return True
        End Function

        Public Function InternalValue(ByVal name As String) As Object
            If Not m_switches Is Nothing Then
                Dim i As Integer = 0
                While i < m_switches.Count
                    If String.Compare((TryCast(m_switches(i), SwitchRecord)).Name, name, True) = 0 Then
                        Return (TryCast(m_switches(i), SwitchRecord)).InternalValue
                    End If
                    System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
                End While
            End If
            Return Nothing
        End Function

#End Region

#Region " Constructors "

        Public Sub New(ByVal commandLine As String)
            m_commandLine = commandLine
        End Sub

        Public Sub New(ByVal commandLine As String, ByVal classForAutoAttributes As Object)
            m_commandLine = commandLine

            Dim type As Type = classForAutoAttributes.[GetType]()
            Dim members As System.Reflection.MemberInfo() = type.GetMembers()

            Dim i As Integer = 0
            While i < members.Length
                Dim attributes As Object() = members(i).GetCustomAttributes(False)
                If attributes.Length > 0 Then
                    Dim rec As SwitchRecord = Nothing

                    For Each attribute As Attribute In attributes
                        If TypeOf attribute Is CommandLineSwitchAttribute Then
                            Dim switchAttrib As CommandLineSwitchAttribute = DirectCast(attribute, CommandLineSwitchAttribute)

                            ' Get the property information.  We're only handling
                            ' properties at the moment!
                            If TypeOf members(i) Is System.Reflection.PropertyInfo Then
                                Dim pi As System.Reflection.PropertyInfo = DirectCast(members(i), System.Reflection.PropertyInfo)

                                rec = New SwitchRecord(switchAttrib.Name, switchAttrib.Description, pi.PropertyType)

                                ' Map in the Get/Set methods.
                                rec.SetMethod = pi.GetSetMethod()
                                rec.GetMethod = pi.GetGetMethod()
                                rec.PropertyOwner = classForAutoAttributes

                                ' Can only handle a single switch for each property
                                ' (otherwise the parsing of aliases gets silly...)
                                Exit For
                            End If
                        End If
                    Next

                    ' See if any aliases are required.  We can only do this after
                    ' a switch has been registered and the framework doesn't make
                    ' any guarantees about the order of attributes, so we have to
                    ' walk the collection a second time.
                    If Not rec Is Nothing Then
                        For Each attribute As Attribute In attributes
                            If TypeOf attribute Is CommandLineAliasAttribute Then
                                Dim aliasAttrib As CommandLineAliasAttribute = DirectCast(attribute, CommandLineAliasAttribute)
                                rec.AddAlias(aliasAttrib.[Alias])
                            End If
                        Next
                    End If

                    ' Assuming we have a switch record (that may or may not have
                    ' aliases), add it to the collection of switches.
                    If Not rec Is Nothing Then
                        If m_switches Is Nothing Then
                            m_switches = New System.Collections.ArrayList()
                        End If
                        m_switches.Add(rec)
                    End If
                End If
                System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
            End While
        End Sub

#End Region

    End Class

End Namespace
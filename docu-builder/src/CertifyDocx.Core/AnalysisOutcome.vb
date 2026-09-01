Imports System.Collections.Generic

Friend Class AnalysisOutcome

    Public Property Success As Boolean
    Public ReadOnly Property Errors As List(Of String)
    Public ReadOnly Property Warnings As List(Of String)
    Public ReadOnly Property Variables As List(Of VariableInfo)
    Public ReadOnly Property Groups As List(Of DetectedGroup)

    Public Sub New()
        Success = True
        Errors = New List(Of String)
        Warnings = New List(Of String)
        Variables = New List(Of VariableInfo)
        Groups = New List(Of DetectedGroup)
    End Sub

End Class

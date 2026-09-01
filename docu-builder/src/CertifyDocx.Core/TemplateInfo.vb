Imports System.Collections.Generic

Public Class TemplateInfo

    Public ReadOnly Property Variables As IReadOnlyList(Of VariableInfo)
    Public ReadOnly Property RowGroups As IReadOnlyList(Of RowGroupInfo)
    Public ReadOnly Property Warnings As IReadOnlyList(Of String)

    Public Sub New(variables As IReadOnlyList(Of VariableInfo),
                   rowGroups As IReadOnlyList(Of RowGroupInfo),
                   warnings As IReadOnlyList(Of String))
        If variables Is Nothing Then
            Me.Variables = Array.Empty(Of VariableInfo)()
        Else
            Me.Variables = variables
        End If
        If rowGroups Is Nothing Then
            Me.RowGroups = Array.Empty(Of RowGroupInfo)()
        Else
            Me.RowGroups = rowGroups
        End If
        If warnings Is Nothing Then
            Me.Warnings = Array.Empty(Of String)()
        Else
            Me.Warnings = warnings
        End If
    End Sub

End Class

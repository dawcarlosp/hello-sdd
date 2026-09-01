Imports System.Collections.Generic

Public Class FillData

    Public ReadOnly Property SimpleValues As IReadOnlyDictionary(Of String, String)
    Public ReadOnly Property RowValues As IReadOnlyList(Of RowGroupValues)

    Public Sub New(simpleValues As IReadOnlyDictionary(Of String, String),
                   rowValues As IReadOnlyList(Of RowGroupValues))
        If simpleValues Is Nothing Then
            Me.SimpleValues = New Dictionary(Of String, String)()
        Else
            Me.SimpleValues = simpleValues
        End If
        If rowValues Is Nothing Then
            Me.RowValues = Array.Empty(Of RowGroupValues)()
        Else
            Me.RowValues = rowValues
        End If
    End Sub

End Class

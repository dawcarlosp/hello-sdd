Imports System.Collections.Generic

Public Class RowGroupValues

    Public ReadOnly Property RowGroupId As Integer
    Public ReadOnly Property Rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, String))

    Public Sub New(rowGroupId As Integer, rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, String)))
        Me.RowGroupId = rowGroupId
        If rows Is Nothing Then
            Me.Rows = Array.Empty(Of IReadOnlyDictionary(Of String, String))()
        Else
            Me.Rows = rows
        End If
    End Sub

End Class

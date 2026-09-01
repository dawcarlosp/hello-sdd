Imports System.Collections.Generic

Public Class RowGroupInfo

    Public ReadOnly Property Id As Integer
    Public ReadOnly Property TableIndex As Integer
    Public ReadOnly Property RowIndex As Integer
    Public ReadOnly Property Variables As IReadOnlyList(Of String)

    Public Sub New(id As Integer, tableIndex As Integer, rowIndex As Integer, variables As IReadOnlyList(Of String))
        Me.Id = id
        Me.TableIndex = tableIndex
        Me.RowIndex = rowIndex
        If variables Is Nothing Then
            Me.Variables = Array.Empty(Of String)()
        Else
            Me.Variables = variables
        End If
    End Sub

End Class

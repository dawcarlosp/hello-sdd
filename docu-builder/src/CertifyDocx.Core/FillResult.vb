Imports System.Collections.Generic

Public Class FillResult

    Public ReadOnly Property Success As Boolean
    Public ReadOnly Property Document As Byte()
    Public ReadOnly Property Errors As IReadOnlyList(Of String)

    Private Sub New(success As Boolean, document As Byte(), errors As IReadOnlyList(Of String))
        Me.Success = success
        Me.Document = document
        If errors Is Nothing Then
            Me.Errors = Array.Empty(Of String)()
        Else
            Me.Errors = errors
        End If
    End Sub

    Public Shared Function Ok(document As Byte()) As FillResult
        Return New FillResult(True, document, Array.Empty(Of String)())
    End Function

    Public Shared Function Fail(errors As IReadOnlyList(Of String)) As FillResult
        Return New FillResult(False, Nothing, errors)
    End Function

End Class

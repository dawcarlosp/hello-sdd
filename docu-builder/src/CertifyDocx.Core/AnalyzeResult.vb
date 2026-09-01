Imports System.Collections.Generic

Public Class AnalyzeResult

    Public ReadOnly Property Success As Boolean
    Public ReadOnly Property Template As TemplateInfo
    Public ReadOnly Property Errors As IReadOnlyList(Of String)

    Private Sub New(success As Boolean, template As TemplateInfo, errors As IReadOnlyList(Of String))
        Me.Success = success
        Me.Template = template
        If errors Is Nothing Then
            Me.Errors = Array.Empty(Of String)()
        Else
            Me.Errors = errors
        End If
    End Sub

    Public Shared Function Ok(template As TemplateInfo) As AnalyzeResult
        Return New AnalyzeResult(True, template, Array.Empty(Of String)())
    End Function

    Public Shared Function Fail(errors As IReadOnlyList(Of String)) As AnalyzeResult
        Return New AnalyzeResult(False, Nothing, errors)
    End Function

End Class

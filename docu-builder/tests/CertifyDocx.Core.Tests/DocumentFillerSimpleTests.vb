Imports System.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class DocumentFillerSimpleTests

    Private Shared Function SimpleTemplate() As Byte()
        Return BuildDocx( _
            PlainParagraph("Certificado para $$nombre$$") & _
            PlainParagraph("Fecha: $$fecha$$"))
    End Function

    Private Shared Function GradesTemplate() As Byte()
        Dim dataRow As String = TableRow( _
            TableCell(PlainParagraph("$$asignatura$$")), _
            TableCell(PlainParagraph("$$nota$$")))
        Return BuildDocx(PlainParagraph("Notas de $$nombre$$") & Table(dataRow))
    End Function

    Private Shared Function RowData(ParamArray rows As IReadOnlyDictionary(Of String, String)()) As RowGroupValues
        Return New RowGroupValues(0, rows.ToList())
    End Function

    <Fact>
    Sub Fill_SubstitutesSimpleVariables()
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {
                {"nombre", "Ana Pérez"},
                {"fecha", "31/08/2026"}
            }, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(SimpleTemplate(), data)

        Assert.True(result.Success)
        Dim text As String = ReadMainPartText(result.Document)
        Assert.Contains("Certificado para Ana Pérez", text)
        Assert.Contains("Fecha: 31/08/2026", text)
        Assert.DoesNotContain("$$", text)
    End Sub

    <Fact>
    Sub Fill_MissingValueIsRejected()
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}}, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(SimpleTemplate(), data)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("fecha"))
    End Sub

    <Fact>
    Sub Fill_EmptyValueIsRejected()
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {
                {"nombre", "Ana"},
                {"fecha", ""}
            }, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(SimpleTemplate(), data)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("fecha"))
    End Sub

    <Fact>
    Sub Fill_UnknownRowGroupIsRejected()
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {
                {"nombre", "Ana"},
                {"fecha", "hoy"}
            },
            {New RowGroupValues(99, Nothing)})

        Dim result As FillResult = DocumentFiller.Fill(SimpleTemplate(), data)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("99"))
    End Sub

    <Fact>
    Sub Fill_MissingRowGroupDataIsRejected()
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}}, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.False(result.Success)
        Assert.Single(result.Errors)
    End Sub

    <Fact>
    Sub Fill_MissingRowValueIsRejected()
        Dim incompleteRow As New Dictionary(Of String, String) From {{"asignatura", "Matemáticas"}}
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}}, {RowData(incompleteRow)})

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("nota"))
    End Sub

    <Fact>
    Sub Fill_MoreThanMaxRowsIsRejected()
        Dim rows As New List(Of IReadOnlyDictionary(Of String, String))
        For i As Integer = 1 To 101
            rows.Add(New Dictionary(Of String, String) From {
                {"asignatura", "A"},
                {"nota", "5"}
            })
        Next
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}},
            {New RowGroupValues(0, rows)})

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("100"))
    End Sub

End Class

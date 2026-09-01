Imports CertifyDocx.Core
Imports Xunit

Public Class TemplateAnalyzerSyntaxErrorTests

    <Fact>
    Sub Analyze_UnclosedMarkerReportsParagraphNumber()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("párrafo uno") & _
            PlainParagraph("Hola $$nombre"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.False(result.Success)
        Dim errorMessage As String = Assert.Single(result.Errors)
        Assert.Contains("Párrafo 2", errorMessage)
    End Sub

    <Fact>
    Sub Analyze_AccumulatesErrorsFromSeveralParagraphs()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("precio 5$") & _
            PlainParagraph("intermedio") & _
            PlainParagraph("$$$$ mal"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.False(result.Success)
        Assert.Equal(2, result.Errors.Count)
        Assert.Contains("Párrafo 1", result.Errors(0))
        Assert.Contains("Párrafo 3", result.Errors(1))
    End Sub

    <Fact>
    Sub Analyze_ParagraphsInsideTablesCountInOrder()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("primero") & _
            Table(TableRow(TableCell(PlainParagraph("$ suelto")))) & _
            PlainParagraph("$$cerrado$$"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.False(result.Success)
        Assert.Contains("Párrafo 2", Assert.Single(result.Errors))
    End Sub

End Class

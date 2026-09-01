Imports CertifyDocx.Core
Imports Xunit

Public Class TemplateAnalyzerTableTests

    Private Shared Function GradesTable() As String
        Dim headerRow As String = TableRow( _
            TableCell(PlainParagraph("Asignatura")), _
            TableCell(PlainParagraph("Nota")))
        Dim dataRow As String = TableRow( _
            TableCell(PlainParagraph("$$asignatura$$")), _
            TableCell(PlainParagraph("$$nota$$")))
        Return Table(headerRow, dataRow)
    End Function

    <Fact>
    Sub Analyze_DetectsRowGroupInTable()
        Dim docx As Byte() = BuildDocx(PlainParagraph("Certificado de $$nombre$$") & GradesTable())

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Equal(3, result.Template.Variables.Count)
        Assert.Equal(VariableKind.Simple, result.Template.Variables(0).Kind)
        Assert.Equal(VariableKind.Row, result.Template.Variables(1).Kind)
        Assert.Equal(VariableKind.Row, result.Template.Variables(2).Kind)
        Assert.Equal(0, result.Template.Variables(1).RowGroupId)

        Dim group As RowGroupInfo = Assert.Single(result.Template.RowGroups)
        Assert.Equal(0, group.Id)
        Assert.Equal(0, group.TableIndex)
        Assert.Equal(1, group.RowIndex)
        Assert.Equal(New String() {"asignatura", "nota"}, group.Variables)
    End Sub

    <Fact>
    Sub Analyze_SameNameInTwoRowsBecomesTwoGroups()
        Dim firstTable As String = Table(TableRow(TableCell(PlainParagraph("$$valor$$"))))
        Dim secondTable As String = Table(TableRow(TableCell(PlainParagraph("$$valor$$"))))
        Dim docx As Byte() = BuildDocx(firstTable & secondTable)

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Equal(2, result.Template.RowGroups.Count)
        Assert.Equal(0, result.Template.RowGroups(0).Id)
        Assert.Equal(0, result.Template.RowGroups(0).TableIndex)
        Assert.Equal(1, result.Template.RowGroups(1).Id)
        Assert.Equal(1, result.Template.RowGroups(1).TableIndex)
        Assert.Equal(2, result.Template.Variables.Count)
        Assert.Equal(0, result.Template.Variables(0).RowGroupId)
        Assert.Equal(1, result.Template.Variables(1).RowGroupId)
    End Sub

    <Fact>
    Sub Analyze_SameNameRepeatedInsideOneRowIsNotDuplicated()
        Dim row As String = TableRow( _
            TableCell(PlainParagraph("$$valor$$")), _
            TableCell(PlainParagraph("$$valor$$ otra vez")))
        Dim docx As Byte() = BuildDocx(Table(row))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Equal(New String() {"valor"}, result.Template.RowGroups(0).Variables)
    End Sub

    <Fact>
    Sub Analyze_NameUsedAsSimpleAndRowIsRejected()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("Texto $$valor$$") & _
            Table(TableRow(TableCell(PlainParagraph("$$valor$$")))))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.False(result.Success)
        Assert.Contains(result.Errors, Function(e As String) e.Contains("valor"))
    End Sub

End Class

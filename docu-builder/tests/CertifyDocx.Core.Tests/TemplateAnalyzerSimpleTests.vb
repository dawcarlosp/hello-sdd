Imports CertifyDocx.Core
Imports Xunit

Public Class TemplateAnalyzerSimpleTests

    <Fact>
    Sub Analyze_DetectsSimpleVariable()
        Dim docx As Byte() = BuildDocx(PlainParagraph("Hola $$nombre$$, bienvenido"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Dim variable As VariableInfo = Assert.Single(result.Template.Variables)
        Assert.Equal("nombre", variable.Name)
        Assert.Equal(VariableKind.Simple, variable.Kind)
        Assert.Empty(result.Template.RowGroups)
        Assert.Empty(result.Errors)
    End Sub

    <Fact>
    Sub Analyze_TwoOccurrencesBecomeOneVariable()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("$$nombre$$ inicia") & PlainParagraph("adiós $$nombre$$"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Single(result.Template.Variables)
        Assert.Equal("nombre", result.Template.Variables(0).Name)
    End Sub

    <Fact>
    Sub Analyze_IsCaseSensitive()
        Dim docx As Byte() = BuildDocx( _
            PlainParagraph("$$Nombre$$ y $$nombre$$"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Equal(2, result.Template.Variables.Count)
        Assert.Equal("Nombre", result.Template.Variables(0).Name)
        Assert.Equal("nombre", result.Template.Variables(1).Name)
    End Sub

    <Fact>
    Sub Analyze_WithoutVariablesSucceedsWithWarning()
        Dim docx As Byte() = BuildDocx(PlainParagraph("sin variables"))

        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze(docx)

        Assert.True(result.Success)
        Assert.Empty(result.Template.Variables)
        Assert.Single(result.Template.Warnings)
    End Sub

    <Fact>
    Sub Analyze_RejectsInvalidDocx()
        Dim result As AnalyzeResult = TemplateAnalyzer.Analyze({9, 9, 9})

        Assert.False(result.Success)
        Assert.Equal("El archivo no es un documento .docx válido.", result.Errors(0))
    End Sub

End Class

Imports CertifyDocx.Core
Imports Xunit

Public Class ContractTests

    <Fact>
    Sub Limits_ExposesRf6Constants()
        Assert.Equal(10 * 1024 * 1024, Limits.MaxTemplateBytes)
        Assert.Equal(100, Limits.MaxRowsPerGroup)
    End Sub

    <Fact>
    Sub VariableInfo_ExposesConstructorValues()
        Dim simple As New VariableInfo("nombre", VariableKind.Simple, 0)
        Assert.Equal("nombre", simple.Name)
        Assert.Equal(VariableKind.Simple, simple.Kind)
        Assert.Equal(0, simple.RowGroupId)

        Dim row As New VariableInfo("asignatura", VariableKind.Row, 2)
        Assert.Equal("asignatura", row.Name)
        Assert.Equal(VariableKind.Row, row.Kind)
        Assert.Equal(2, row.RowGroupId)
    End Sub

    <Fact>
    Sub RowGroupInfo_ExposesConstructorValues()
        Dim variables As New List(Of String) From {"asignatura", "nota"}
        Dim group As New RowGroupInfo(0, 1, 2, variables)
        Assert.Equal(0, group.Id)
        Assert.Equal(1, group.TableIndex)
        Assert.Equal(2, group.RowIndex)
        Assert.Equal(New String() {"asignatura", "nota"}, group.Variables)
    End Sub

    <Fact>
    Sub RowGroupInfo_NullVariablesBecomesEmptyList()
        Dim group As New RowGroupInfo(0, 0, 0, Nothing)
        Assert.NotNull(group.Variables)
        Assert.Empty(group.Variables)
    End Sub

    <Fact>
    Sub TemplateInfo_ExposesConstructorValues()
        Dim variables As IReadOnlyList(Of VariableInfo) = {
            New VariableInfo("nombre", VariableKind.Simple, 0),
            New VariableInfo("asignatura", VariableKind.Row, 0)
        }
        Dim rowGroups As IReadOnlyList(Of RowGroupInfo) = {
            New RowGroupInfo(0, 0, 2, New String() {"asignatura"})
        }
        Dim warnings As IReadOnlyList(Of String) = {"aviso de prueba"}

        Dim template As New TemplateInfo(variables, rowGroups, warnings)

        Assert.Equal(2, template.Variables.Count)
        Assert.Equal("nombre", template.Variables(0).Name)
        Assert.Single(template.RowGroups)
        Assert.Equal("aviso de prueba", template.Warnings(0))
    End Sub

    <Fact>
    Sub TemplateInfo_NullCollectionsBecomeEmptyLists()
        Dim template As New TemplateInfo(Nothing, Nothing, Nothing)
        Assert.NotNull(template.Variables)
        Assert.NotNull(template.RowGroups)
        Assert.NotNull(template.Warnings)
        Assert.Empty(template.Variables)
        Assert.Empty(template.RowGroups)
        Assert.Empty(template.Warnings)
    End Sub

    <Fact>
    Sub AnalyzeResult_OkHasTemplateAndNoErrors()
        Dim template As New TemplateInfo(Nothing, Nothing, Nothing)
        Dim result As AnalyzeResult = AnalyzeResult.Ok(template)
        Assert.True(result.Success)
        Assert.Same(template, result.Template)
        Assert.NotNull(result.Errors)
        Assert.Empty(result.Errors)
    End Sub

    <Fact>
    Sub AnalyzeResult_FailHasErrorsAndNoTemplate()
        Dim errors As IReadOnlyList(Of String) = {"Marcador $$ sin cerrar en el párrafo 3."}
        Dim result As AnalyzeResult = AnalyzeResult.Fail(errors)
        Assert.False(result.Success)
        Assert.Null(result.Template)
        Assert.Equal(New String() {"Marcador $$ sin cerrar en el párrafo 3."}, result.Errors)
    End Sub

    <Fact>
    Sub RowGroupValues_ExposesConstructorValues()
        Dim row As IReadOnlyDictionary(Of String, String) =
            New Dictionary(Of String, String) From {
                {"asignatura", "Matemáticas"},
                {"nota", "9,5"}
            }
        Dim groupValues As New RowGroupValues(0, {row})
        Assert.Equal(0, groupValues.RowGroupId)
        Assert.Single(groupValues.Rows)
        Assert.Equal("Matemáticas", groupValues.Rows(0)("asignatura"))
    End Sub

    <Fact>
    Sub RowGroupValues_NullRowsBecomesEmptyList()
        Dim groupValues As New RowGroupValues(0, Nothing)
        Assert.NotNull(groupValues.Rows)
        Assert.Empty(groupValues.Rows)
    End Sub

    <Fact>
    Sub FillData_ExposesConstructorValues()
        Dim simpleValues As IReadOnlyDictionary(Of String, String) =
            New Dictionary(Of String, String) From {{"nombre", "Ana Pérez"}}
        Dim rowValues As IReadOnlyList(Of RowGroupValues) = {New RowGroupValues(0, Nothing)}

        Dim fillData As New FillData(simpleValues, rowValues)

        Assert.Equal("Ana Pérez", fillData.SimpleValues("nombre"))
        Assert.Single(fillData.RowValues)
        Assert.Equal(0, fillData.RowValues(0).RowGroupId)
    End Sub

    <Fact>
    Sub FillData_NullCollectionsBecomeEmpty()
        Dim fillData As New FillData(Nothing, Nothing)
        Assert.NotNull(fillData.SimpleValues)
        Assert.NotNull(fillData.RowValues)
        Assert.Empty(fillData.SimpleValues)
        Assert.Empty(fillData.RowValues)
    End Sub

    <Fact>
    Sub FillResult_OkHasDocumentAndNoErrors()
        Dim document As Byte() = {1, 2, 3}
        Dim result As FillResult = FillResult.Ok(document)
        Assert.True(result.Success)
        Assert.Same(document, result.Document)
        Assert.NotNull(result.Errors)
        Assert.Empty(result.Errors)
    End Sub

    <Fact>
    Sub FillResult_FailHasErrorsAndNoDocument()
        Dim errors As IReadOnlyList(Of String) = {"Falta el valor de la variable «nombre»."}
        Dim result As FillResult = FillResult.Fail(errors)
        Assert.False(result.Success)
        Assert.Null(result.Document)
        Assert.Equal(New String() {"Falta el valor de la variable «nombre»."}, result.Errors)
    End Sub

    <Fact>
    Sub VariableKind_HasExactlySimpleAndRow()
        Dim names As String() = [Enum].GetNames(GetType(VariableKind))
        Assert.Equal(2, names.Length)
        Assert.Contains("Simple", names)
        Assert.Contains("Row", names)
    End Sub

End Class

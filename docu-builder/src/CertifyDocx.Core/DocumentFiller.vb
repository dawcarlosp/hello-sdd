Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq

Public Class DocumentFiller

    Public Shared Function Fill(docx As Byte(), data As FillData) As FillResult
        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing
        If Not DocxPackage.TryReadDocumentXml(docx, document, errorMessage) Then
            Return FillResult.Fail(New String() {errorMessage})
        End If

        Dim outcome As AnalysisOutcome = TemplateAnalysis.Run(document)
        If Not outcome.Success Then
            Return FillResult.Fail(outcome.Errors)
        End If

        Dim validationErrors As List(Of String) = ValidateFill(outcome, data)
        If validationErrors.Count > 0 Then
            Return FillResult.Fail(validationErrors)
        End If

        If outcome.Variables.Count = 0 Then
            Return FillResult.Ok(docx)
        End If

        Dim body As XElement = document.Root.Element(WordNamespaces.W.GetName("body"))
        For Each paragraph As XElement In body.Descendants(WordNamespaces.W.GetName("p"))
            Dim insideRow As Boolean = paragraph.Ancestors(WordNamespaces.W.GetName("tr")).Any()
            If Not insideRow Then
                ParagraphReplacer.Replace(paragraph, data.SimpleValues)
            End If
        Next

        Dim valuesByGroup As New Dictionary(Of Integer, RowGroupValues)
        For Each groupValues As RowGroupValues In data.RowValues
            If Not valuesByGroup.ContainsKey(groupValues.RowGroupId) Then
                valuesByGroup.Add(groupValues.RowGroupId, groupValues)
            End If
        Next

        For Each group As DetectedGroup In outcome.Groups
            Dim groupValues As RowGroupValues = valuesByGroup(group.Id)
            ApplyRows(group.TemplateRow, groupValues.Rows)
        Next

        Return FillResult.Ok(DocxWriter.Rewrite(docx, document))
    End Function

    Private Shared Sub ApplyRows(templateRow As XElement, rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, String)))
        If rows.Count = 0 Then
            templateRow.Remove()
            Return
        End If

        For Each row As IReadOnlyDictionary(Of String, String) In rows
            Dim clone As XElement = New XElement(templateRow)
            For Each paragraph As XElement In clone.Descendants(WordNamespaces.W.GetName("p"))
                ParagraphReplacer.Replace(paragraph, row)
            Next
            templateRow.AddBeforeSelf(clone)
        Next
        templateRow.Remove()
    End Sub

    Private Shared Function ValidateFill(outcome As AnalysisOutcome, data As FillData) As List(Of String)
        Dim errors As New List(Of String)

        For Each variable As VariableInfo In outcome.Variables
            If variable.Kind = VariableKind.Simple Then
                Dim value As String = Nothing
                If Not data.SimpleValues.TryGetValue(variable.Name, value) OrElse String.IsNullOrEmpty(value) Then
                    errors.Add(String.Format("Falta el valor de la variable «{0}».", variable.Name))
                End If
            End If
        Next

        Dim groupsById As New Dictionary(Of Integer, DetectedGroup)
        For Each group As DetectedGroup In outcome.Groups
            groupsById.Add(group.Id, group)
        Next

        Dim valuesByGroup As New Dictionary(Of Integer, RowGroupValues)
        For Each groupValues As RowGroupValues In data.RowValues
            If Not groupsById.ContainsKey(groupValues.RowGroupId) Then
                errors.Add(String.Format("El grupo de filas {0} no existe en la plantilla.", groupValues.RowGroupId))
            ElseIf Not valuesByGroup.ContainsKey(groupValues.RowGroupId) Then
                valuesByGroup.Add(groupValues.RowGroupId, groupValues)
            End If
        Next

        For Each group As DetectedGroup In outcome.Groups
            Dim label As String = String.Format("tabla {0}, fila {1}", group.TableIndex + 1, group.RowIndex + 1)
            Dim groupValues As RowGroupValues = Nothing
            If Not valuesByGroup.TryGetValue(group.Id, groupValues) Then
                errors.Add(String.Format("Faltan las filas de la {0}.", label))
                Continue For
            End If
            If groupValues.Rows.Count > Limits.MaxRowsPerGroup Then
                errors.Add(String.Format("La {0} supera el máximo de {1} filas.", label, Limits.MaxRowsPerGroup))
                Continue For
            End If
            For Each row As IReadOnlyDictionary(Of String, String) In groupValues.Rows
                For Each variableName As String In group.VariableNames
                    Dim value As String = Nothing
                    If Not row.TryGetValue(variableName, value) OrElse String.IsNullOrEmpty(value) Then
                        errors.Add(String.Format("Falta el valor de la variable «{0}».", variableName))
                    End If
                Next
            Next
        Next

        Return errors
    End Function

End Class

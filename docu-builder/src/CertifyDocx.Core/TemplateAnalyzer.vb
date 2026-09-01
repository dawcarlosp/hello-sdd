Imports System.Collections.Generic
Imports System.Xml.Linq

Public Class TemplateAnalyzer

    Public Shared Function Analyze(docx As Byte()) As AnalyzeResult
        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing
        If Not DocxPackage.TryReadDocumentXml(docx, document, errorMessage) Then
            Return AnalyzeResult.Fail(New String() {errorMessage})
        End If

        Dim outcome As AnalysisOutcome = TemplateAnalysis.Run(document)
        If Not outcome.Success Then
            Return AnalyzeResult.Fail(outcome.Errors)
        End If

        Dim rowGroups As New List(Of RowGroupInfo)
        For Each group As DetectedGroup In outcome.Groups
            rowGroups.Add(New RowGroupInfo(group.Id, group.TableIndex, group.RowIndex, group.VariableNames))
        Next

        Dim template As New TemplateInfo(outcome.Variables, rowGroups, outcome.Warnings)
        Return AnalyzeResult.Ok(template)
    End Function

End Class

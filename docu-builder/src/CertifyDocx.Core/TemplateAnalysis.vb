Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq

Friend Module TemplateAnalysis

    Friend Const NoVariablesWarning As String =
        "La plantilla no contiene variables; el documento generado será idéntico a la plantilla."
    Friend Const ConflictMessageFormat As String =
        "«{0}» se usa como variable simple y de tabla; usa nombres distintos."
    Friend Const ParagraphErrorFormat As String =
        "Párrafo {0}: {1}"

    Function Run(document As XDocument) As AnalysisOutcome
        Dim outcome As New AnalysisOutcome()
        Dim body As XElement = document.Root.Element(WordNamespaces.W.GetName("body"))
        If body Is Nothing Then
            outcome.Success = False
            outcome.Errors.Add(DocxPackage.InvalidDocxMessage)
            Return outcome
        End If

        Dim tables As List(Of XElement) = body.Descendants(WordNamespaces.W.GetName("tbl")).ToList()
        Dim simpleNames As New HashSet(Of String)
        Dim rowNames As New HashSet(Of String)
        Dim conflicts As New HashSet(Of String)
        Dim groupsByRow As New Dictionary(Of XElement, DetectedGroup)

        Dim paragraphNumber As Integer = 0
        For Each paragraph As XElement In body.Descendants(WordNamespaces.W.GetName("p"))
            paragraphNumber += 1
            Dim map As ParagraphTextMap = ParagraphTextMap.Build(paragraph)
            Dim scan As MarkerScanResult = MarkerScanner.Scan(map.FullText)
            For Each errorMessage As String In scan.Errors
                outcome.Errors.Add(String.Format(ParagraphErrorFormat, paragraphNumber, errorMessage))
            Next

            Dim row As XElement = paragraph.Ancestors(WordNamespaces.W.GetName("tr")).FirstOrDefault()
            For Each marker As Marker In scan.Markers
                If row Is Nothing Then
                    RegisterSimpleVariable(outcome, simpleNames, rowNames, conflicts, marker.Name)
                Else
                    RegisterRowVariable(outcome, groupsByRow, tables, row, simpleNames, rowNames, conflicts, marker.Name)
                End If
            Next
        Next

        For Each conflict As String In conflicts
            outcome.Errors.Add(String.Format(ConflictMessageFormat, conflict))
        Next

        If outcome.Errors.Count > 0 Then
            outcome.Success = False
        ElseIf outcome.Variables.Count = 0 Then
            outcome.Warnings.Add(NoVariablesWarning)
        End If

        Return outcome
    End Function

    Private Sub RegisterSimpleVariable(outcome As AnalysisOutcome,
                                       simpleNames As HashSet(Of String),
                                       rowNames As HashSet(Of String),
                                       conflicts As HashSet(Of String),
                                       name As String)
        If rowNames.Contains(name) Then
            conflicts.Add(name)
            Return
        End If
        If simpleNames.Contains(name) Then
            Return
        End If
        simpleNames.Add(name)
        outcome.Variables.Add(New VariableInfo(name, VariableKind.Simple, 0))
    End Sub

    Private Sub RegisterRowVariable(outcome As AnalysisOutcome,
                                    groupsByRow As Dictionary(Of XElement, DetectedGroup),
                                    tables As List(Of XElement),
                                    row As XElement,
                                    simpleNames As HashSet(Of String),
                                    rowNames As HashSet(Of String),
                                    conflicts As HashSet(Of String),
                                    name As String)
        If simpleNames.Contains(name) Then
            conflicts.Add(name)
            Return
        End If

        Dim group As DetectedGroup = Nothing
        If Not groupsByRow.TryGetValue(row, group) Then
            group = CreateGroup(outcome, groupsByRow, tables, row)
        End If

        If group.VariableNames.Contains(name) Then
            Return
        End If
        group.VariableNames.Add(name)
        rowNames.Add(name)
        outcome.Variables.Add(New VariableInfo(name, VariableKind.Row, group.Id))
    End Sub

    Private Function CreateGroup(outcome As AnalysisOutcome,
                                 groupsByRow As Dictionary(Of XElement, DetectedGroup),
                                 tables As List(Of XElement),
                                 row As XElement) As DetectedGroup
        Dim table As XElement = row.Ancestors(WordNamespaces.W.GetName("tbl")).FirstOrDefault()
        Dim tableIndex As Integer = tables.IndexOf(table)
        Dim rowIndex As Integer = 0
        If table IsNot Nothing Then
            rowIndex = table.Elements(WordNamespaces.W.GetName("tr")).ToList().IndexOf(row)
        End If

        Dim group As New DetectedGroup(outcome.Groups.Count, tableIndex, rowIndex, row)
        outcome.Groups.Add(group)
        groupsByRow.Add(row, group)
        Return group
    End Function

End Module

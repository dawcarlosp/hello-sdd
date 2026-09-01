Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq

Friend Module ParagraphReplacer

    Function Replace(paragraph As XElement, values As IReadOnlyDictionary(Of String, String)) As Boolean
        Dim map As ParagraphTextMap = ParagraphTextMap.Build(paragraph)
        Dim scan As MarkerScanResult = MarkerScanner.Scan(map.FullText)
        If Not scan.IsValid OrElse scan.Markers.Count = 0 Then
            Return False
        End If

        Dim replaced As Boolean = False
        Dim index As Integer = scan.Markers.Count - 1
        While index >= 0
            Dim marker As Marker = scan.Markers(index)
            Dim value As String = Nothing
            If values.TryGetValue(marker.Name, value) Then
                RewriteMarker(map, marker, value)
                replaced = True
            End If
            index -= 1
        End While

        Return replaced
    End Function

    Private Sub RewriteMarker(map As ParagraphTextMap, marker As Marker, value As String)
        Dim startLocation As (ElementIndex As Integer, Offset As Integer) = map.Locate(marker.Start)
        Dim endLocation As (ElementIndex As Integer, Offset As Integer) = map.Locate(marker.End - 1)

        Dim elementIndex As Integer = startLocation.ElementIndex
        While elementIndex <= endLocation.ElementIndex
            Dim element As XElement = map.TextElements(elementIndex)
            Dim elementText As String = element.Value
            Dim elementStart As Integer = map.StartOfElement(elementIndex)

            Dim keepStart As Integer = Math.Max(marker.Start - elementStart, 0)
            Dim keepEnd As Integer = Math.Min(marker.End - elementStart, elementText.Length)

            Dim newText As String = elementText.Substring(0, keepStart)
            If elementIndex = startLocation.ElementIndex Then
                newText &= value
            End If
            newText &= elementText.Substring(keepEnd)

            element.Value = newText
            If newText.Length > 0 AndAlso (newText(0) = " "c OrElse newText(newText.Length - 1) = " "c) Then
                element.SetAttributeValue(WordNamespaces.Xml.GetName("space"), "preserve")
            End If

            elementIndex += 1
        End While
    End Sub

End Module

Imports System.Collections.Generic
Imports System.Xml.Linq

Friend Class ParagraphTextMap

    Private ReadOnly elementStarts As Integer()

    Public ReadOnly Property TextElements As IReadOnlyList(Of XElement)
    Public ReadOnly Property FullText As String

    Private Sub New(textElements As IReadOnlyList(Of XElement), fullText As String, elementStarts As Integer())
        Me.TextElements = textElements
        Me.FullText = fullText
        Me.elementStarts = elementStarts
    End Sub

    Public Shared Function Build(paragraph As XElement) As ParagraphTextMap
        Dim textElements As New List(Of XElement)
        Dim builder As New System.Text.StringBuilder()
        Dim starts As New List(Of Integer)

        For Each element As XElement In paragraph.Descendants(WordNamespaces.W.GetName("t"))
            textElements.Add(element)
            starts.Add(builder.Length)
            builder.Append(element.Value)
        Next

        Return New ParagraphTextMap(textElements, builder.ToString(), starts.ToArray())
    End Function

    Public Function StartOfElement(elementIndex As Integer) As Integer
        Return elementStarts(elementIndex)
    End Function

    Public Function Locate(globalIndex As Integer) As (ElementIndex As Integer, Offset As Integer)
        Dim elementIndex As Integer = elementStarts.Length - 1
        While elementIndex > 0 AndAlso elementStarts(elementIndex) > globalIndex
            elementIndex -= 1
        End While
        Return (elementIndex, globalIndex - elementStarts(elementIndex))
    End Function

End Class

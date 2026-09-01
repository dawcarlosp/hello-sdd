Imports System.Xml.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class ParagraphTextMapTests

    Private Const W As String = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

    Private Shared Function ParseParagraph(innerXml As String) As XElement
        Dim xml As String = "<w:p xmlns:w=""" & W & """>" & innerXml & "</w:p>"
        Return XElement.Parse(xml)
    End Function

    Private Shared Function ThreeRunParagraph() As XElement
        Dim innerXml As String = "<w:r><w:t>HOLA </w:t></w:r>" & _
            "<w:r><w:t>nom</w:t></w:r>" & _
            "<w:r><w:t>bre</w:t></w:r>"
        Return ParseParagraph(innerXml)
    End Function

    <Fact>
    Sub Build_ConcatenatesAllTextElementsInOrder()
        Dim map As ParagraphTextMap = ParagraphTextMap.Build(ThreeRunParagraph())

        Assert.Equal("HOLA nombre", map.FullText)
        Assert.Equal(3, map.TextElements.Count)
    End Sub

    <Fact>
    Sub Build_EmptyParagraphHasEmptyText()
        Dim map As ParagraphTextMap = ParagraphTextMap.Build(ParseParagraph(""))

        Assert.Equal("", map.FullText)
        Assert.Empty(map.TextElements)
    End Sub

    <Fact>
    Sub Locate_MapsGlobalIndexToElementAndOffset()
        Dim map As ParagraphTextMap = ParagraphTextMap.Build(ThreeRunParagraph())

        Assert.Equal((0, 0), map.Locate(0))
        Assert.Equal((0, 4), map.Locate(4))
        Assert.Equal((1, 0), map.Locate(5))
        Assert.Equal((1, 1), map.Locate(6))
        Assert.Equal((2, 0), map.Locate(8))
        Assert.Equal((2, 2), map.Locate(10))
    End Sub

    <Fact>
    Sub StartOfElement_ReturnsGlobalStartOfEachElement()
        Dim map As ParagraphTextMap = ParagraphTextMap.Build(ThreeRunParagraph())

        Assert.Equal(0, map.StartOfElement(0))
        Assert.Equal(5, map.StartOfElement(1))
        Assert.Equal(8, map.StartOfElement(2))
    End Sub

End Class

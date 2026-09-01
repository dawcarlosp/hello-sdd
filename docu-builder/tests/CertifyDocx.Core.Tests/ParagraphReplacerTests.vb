Imports System.Linq
Imports System.Xml.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class ParagraphReplacerTests

    Private Const W As String = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

    Private Shared Function ParseParagraph(innerXml As String) As XElement
        Dim xml As String = "<w:p xmlns:w=""" & W & """>" & innerXml & "</w:p>"
        Return XElement.Parse(xml)
    End Function

    Private Shared Function FullTextOf(paragraph As XElement) As String
        Return ParagraphTextMap.Build(paragraph).FullText
    End Function

    <Fact>
    Sub Replace_SingleRunMarker()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:t>Hola $$nombre$$</w:t></w:r>")
        Dim values As New Dictionary(Of String, String) From {{"nombre", "Ana"}}

        Dim replaced As Boolean = ParagraphReplacer.Replace(paragraph, values)

        Assert.True(replaced)
        Assert.Equal("Hola Ana", FullTextOf(paragraph))
    End Sub

    <Fact>
    Sub Replace_MarkerSplitAcrossThreeRuns()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:t>Pre $$no</w:t></w:r>" & _
            "<w:r><w:t>mbre</w:t></w:r>" & _
            "<w:r><w:t>$$ post</w:t></w:r>")
        Dim values As New Dictionary(Of String, String) From {{"nombre", "Ana"}}

        Dim replaced As Boolean = ParagraphReplacer.Replace(paragraph, values)

        Assert.True(replaced)
        Assert.Equal("Pre Ana post", FullTextOf(paragraph))
    End Sub

    <Fact>
    Sub Replace_KeepsFormattingOfFirstAffectedRun()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:rPr><w:b/></w:rPr><w:t>$$nombre$$</w:t></w:r>")
        Dim values As New Dictionary(Of String, String) From {{"nombre", "Ana"}}

        ParagraphReplacer.Replace(paragraph, values)

        Dim run As XElement = paragraph.Descendants(WordNamespaces.W.GetName("r")).First()
        Assert.NotNull(run.Element(WordNamespaces.W.GetName("rPr")))
        Assert.NotNull(run.Element(WordNamespaces.W.GetName("rPr")).Element(WordNamespaces.W.GetName("b")))
        Assert.Equal("Ana", FullTextOf(paragraph))
    End Sub

    <Fact>
    Sub Replace_TwoMarkersInOneParagraph()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:t>$$saludo$$ y $$despedida$$</w:t></w:r>")
        Dim values As New Dictionary(Of String, String) From {
            {"saludo", "hola"},
            {"despedida", "adiós"}
        }

        ParagraphReplacer.Replace(paragraph, values)

        Assert.Equal("hola y adiós", FullTextOf(paragraph))
    End Sub

    <Fact>
    Sub Replace_ValueWithXmlSpecialCharacters()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:t>$$nombre$$</w:t></w:r>")
        Dim values As New Dictionary(Of String, String) From {{"nombre", "A&B <C>"}}

        ParagraphReplacer.Replace(paragraph, values)

        Assert.Equal("A&B <C>", FullTextOf(paragraph))
    End Sub

    <Fact>
    Sub Replace_UnknownVariableIsLeftUntouched()
        Dim paragraph As XElement = ParseParagraph( _
            "<w:r><w:t>Hola $$nombre$$</w:t></w:r>")
        Dim values As New Dictionary(Of String, String)

        Dim replaced As Boolean = ParagraphReplacer.Replace(paragraph, values)

        Assert.False(replaced)
        Assert.Equal("Hola $$nombre$$", FullTextOf(paragraph))
    End Sub

End Class

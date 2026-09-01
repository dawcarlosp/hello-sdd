Imports System.IO
Imports System.IO.Compression
Imports System.Xml.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class DocumentFillerZipTests

    <Fact>
    Sub Fill_OutputIsAValidDocx()
        Dim docx As Byte() = BuildDocx(PlainParagraph("Hola $$nombre$$"))
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}}, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(docx, data)

        Assert.True(result.Success)
        Using stream As New MemoryStream(result.Document, False)
            Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                Assert.NotNull(zip.GetEntry("word/document.xml"))
                Using entryStream As Stream = zip.GetEntry("word/document.xml").Open()
                    Dim parsed As XDocument = XDocument.Load(entryStream)
                    Assert.Equal("document", parsed.Root.Name.LocalName)
                End Using
            End Using
        End Using
    End Sub

    <Fact>
    Sub Fill_PreservesOtherEntriesUnchanged()
        Dim docx As Byte() = BuildDocxWithExtraEntry( _
            PlainParagraph("Hola $$nombre$$"), "word/styles.xml", "<styles>contenido</styles>")
        Dim data As New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}}, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(docx, data)

        Assert.True(result.Success)
        Using stream As New MemoryStream(result.Document, False)
            Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                Dim entry As ZipArchiveEntry = zip.GetEntry("word/styles.xml")
                Assert.NotNull(entry)
                Using entryStream As Stream = entry.Open()
                    Using reader As New StreamReader(entryStream)
                        Assert.Equal("<styles>contenido</styles>", reader.ReadToEnd())
                    End Using
                End Using
            End Using
        End Using
    End Sub

    <Fact>
    Sub Fill_TemplateWithoutVariablesReturnsIdenticalBytes()
        Dim docx As Byte() = BuildDocx(PlainParagraph("sin variables"))
        Dim data As New FillData(Nothing, Nothing)

        Dim result As FillResult = DocumentFiller.Fill(docx, data)

        Assert.True(result.Success)
        Assert.Equal(docx, result.Document)
    End Sub

End Class

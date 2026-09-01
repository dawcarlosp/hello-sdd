Imports System.IO
Imports System.IO.Compression
Imports System.Xml.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class DocxPackageTests

    <Fact>
    Sub TryReadDocumentXml_RejectsNonZipBytes()
        Dim garbage As Byte() = {1, 2, 3, 4, 5}
        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing

        Dim ok As Boolean = DocxPackage.TryReadDocumentXml(garbage, document, errorMessage)

        Assert.False(ok)
        Assert.Null(document)
        Assert.Equal("El archivo no es un documento .docx válido.", errorMessage)
    End Sub

    <Fact>
    Sub TryReadDocumentXml_RejectsEmptyBytes()
        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing

        Dim ok As Boolean = DocxPackage.TryReadDocumentXml(Array.Empty(Of Byte)(), document, errorMessage)

        Assert.False(ok)
        Assert.Equal("El archivo no es un documento .docx válido.", errorMessage)
    End Sub

    <Fact>
    Sub TryReadDocumentXml_RejectsZipWithoutMainPart()
        Dim zipBytes As Byte()
        Using output As New MemoryStream()
            Using zip As New ZipArchive(output, ZipArchiveMode.Create, leaveOpen:=True)
                Dim entry As ZipArchiveEntry = zip.CreateEntry("otro.xml")
                Using stream As Stream = entry.Open()
                    stream.WriteByte(65)
                End Using
            End Using
            zipBytes = output.ToArray()
        End Using

        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing

        Dim ok As Boolean = DocxPackage.TryReadDocumentXml(zipBytes, document, errorMessage)

        Assert.False(ok)
        Assert.Equal("El archivo no es un documento .docx válido.", errorMessage)
    End Sub

    <Fact>
    Sub TryReadDocumentXml_ReadsMainPartOfValidDocx()
        Dim docx As Byte() = BuildDocx(PlainParagraph("Hola"))

        Dim document As XDocument = Nothing
        Dim errorMessage As String = Nothing

        Dim ok As Boolean = DocxPackage.TryReadDocumentXml(docx, document, errorMessage)

        Assert.True(ok)
        Assert.Null(errorMessage)
        Assert.NotNull(document)
        Assert.Equal("document", document.Root.Name.LocalName)
        Assert.Equal("http://schemas.openxmlformats.org/wordprocessingml/2006/main", document.Root.Name.NamespaceName)
    End Sub

End Class

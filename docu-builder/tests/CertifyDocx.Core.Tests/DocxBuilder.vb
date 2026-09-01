Imports System.IO
Imports System.IO.Compression
Imports System.Text

Friend Module DocxBuilder

    Private Const WXmlNamespace As String = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

    Function BuildDocx(bodyInnerXml As String) As Byte()
        Return BuildDocxEntries(bodyInnerXml, Nothing)
    End Function

    Function BuildDocxWithExtraEntry(bodyInnerXml As String, extraEntryName As String, extraEntryContent As String) As Byte()
        Return BuildDocxEntries(bodyInnerXml, New KeyValuePair(Of String, String)(extraEntryName, extraEntryContent))
    End Function

    Private Function BuildDocxEntries(bodyInnerXml As String, extraEntry As KeyValuePair(Of String, String)?) As Byte()
        Using output As New MemoryStream()
            Using zip As New ZipArchive(output, ZipArchiveMode.Create, leaveOpen:=True)
                WriteEntry(zip, "[Content_Types].xml", ContentTypesXml())
                WriteEntry(zip, "_rels/.rels", RootRelsXml())
                WriteEntry(zip, "word/document.xml", DocumentXml(bodyInnerXml))
                If extraEntry.HasValue Then
                    WriteEntry(zip, extraEntry.Value.Key, extraEntry.Value.Value)
                End If
            End Using
            Return output.ToArray()
        End Using
    End Function

    Private Sub WriteEntry(zip As ZipArchive, entryName As String, content As String)
        Dim entry As ZipArchiveEntry = zip.CreateEntry(entryName)
        Using stream As Stream = entry.Open()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(content)
            stream.Write(bytes, 0, bytes.Length)
        End Using
    End Sub

    Private Function ContentTypesXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
               "<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
               "<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>" &
               "<Default Extension=""xml"" ContentType=""application/xml""/>" &
               "<Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>" &
               "</Types>"
    End Function

    Private Function RootRelsXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
               "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
               "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>" &
               "</Relationships>"
    End Function

    Private Function DocumentXml(bodyInnerXml As String) As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
               "<w:document xmlns:w=""" & WXmlNamespace & """>" &
               "<w:body>" & bodyInnerXml & "</w:body>" &
               "</w:document>"
    End Function

    Function Paragraph(innerXml As String) As String
        Return "<w:p>" & innerXml & "</w:p>"
    End Function

    Function PlainParagraph(text As String) As String
        Return Paragraph(Run(text))
    End Function

    Function Run(text As String) As String
        Return "<w:r><w:t xml:space=""preserve"">" & EscapeXml(text) & "</w:t></w:r>"
    End Function

    Function BoldRun(text As String) As String
        Return "<w:r><w:rPr><w:b/></w:rPr><w:t xml:space=""preserve"">" & EscapeXml(text) & "</w:t></w:r>"
    End Function

    Function Table(ParamArray rowsXml As String()) As String
        Return "<w:tbl>" & String.Concat(rowsXml) & "</w:tbl>"
    End Function

    Function TableRow(ParamArray cellsXml As String()) As String
        Return "<w:tr>" & String.Concat(cellsXml) & "</w:tr>"
    End Function

    Function TableCell(innerXml As String) As String
        Return "<w:tc>" & innerXml & "</w:tc>"
    End Function

    Function ReadMainPartText(docx As Byte()) As String
        Using stream As New MemoryStream(docx, False)
            Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                Dim entry As ZipArchiveEntry = zip.GetEntry("word/document.xml")
                Using entryStream As Stream = entry.Open()
                    Using reader As New StreamReader(entryStream, Encoding.UTF8)
                        Return reader.ReadToEnd()
                    End Using
                End Using
            End Using
        End Using
    End Function

    Private Function EscapeXml(text As String) As String
        Return System.Security.SecurityElement.Escape(text)
    End Function

End Module

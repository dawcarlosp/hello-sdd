Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Xml
Imports System.Xml.Linq

Friend Module DocxWriter

    Function Rewrite(originalDocx As Byte(), document As XDocument) As Byte()
        Using output As New MemoryStream()
            Using outputZip As New ZipArchive(output, ZipArchiveMode.Create, leaveOpen:=True)
                Using inputStream As New MemoryStream(originalDocx, False)
                    Using inputZip As New ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen:=True)
                        For Each entry As ZipArchiveEntry In inputZip.Entries
                            Dim newEntry As ZipArchiveEntry = outputZip.CreateEntry(entry.FullName)
                            Using target As Stream = newEntry.Open()
                                If entry.FullName = DocxPackage.MainPartName Then
                                    WriteDocumentXml(document, target)
                                Else
                                    Using source As Stream = entry.Open()
                                        source.CopyTo(target)
                                    End Using
                                End If
                            End Using
                        Next
                    End Using
                End Using
            End Using
            Return output.ToArray()
        End Using
    End Function

    Private Sub WriteDocumentXml(document As XDocument, target As Stream)
        Dim settings As New XmlWriterSettings()
        settings.Encoding = New UTF8Encoding(False)
        settings.OmitXmlDeclaration = False
        Using writer As XmlWriter = XmlWriter.Create(target, settings)
            document.Save(writer)
        End Using
    End Sub

End Module

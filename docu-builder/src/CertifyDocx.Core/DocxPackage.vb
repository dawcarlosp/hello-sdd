Imports System.IO
Imports System.IO.Compression
Imports System.Xml
Imports System.Xml.Linq

Friend Module DocxPackage

    Friend Const MainPartName As String = "word/document.xml"
    Friend Const InvalidDocxMessage As String = "El archivo no es un documento .docx válido."

    Function TryReadDocumentXml(docx As Byte(), ByRef document As XDocument, ByRef errorMessage As String) As Boolean
        document = Nothing
        errorMessage = Nothing
        Try
            Using stream As New MemoryStream(docx, False)
                Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                    Dim entry As ZipArchiveEntry = zip.GetEntry(MainPartName)
                    If entry Is Nothing Then
                        errorMessage = InvalidDocxMessage
                        Return False
                    End If
                    Using entryStream As Stream = entry.Open()
                        document = XDocument.Load(entryStream)
                    End Using
                End Using
            End Using
        Catch ex As Exception When TypeOf ex Is InvalidDataException OrElse TypeOf ex Is IOException OrElse TypeOf ex Is XmlException
            errorMessage = InvalidDocxMessage
            Return False
        End Try
        Return True
    End Function

End Module

Imports System.Linq
Imports CertifyDocx.Core
Imports Xunit

Public Class DocumentFillerTableTests

    Private Shared Function GradesTemplate() As Byte()
        Dim headerRow As String = TableRow( _
            TableCell(PlainParagraph("Asignatura")), _
            TableCell(PlainParagraph("Nota")))
        Dim dataRow As String = TableRow( _
            TableCell(PlainParagraph("$$asignatura$$")), _
            TableCell(PlainParagraph("$$nota$$")))
        Return BuildDocx(PlainParagraph("Notas de $$nombre$$") & Table(headerRow, dataRow))
    End Function

    Private Shared Function GradeRow(subject As String, grade As String) As IReadOnlyDictionary(Of String, String)
        Return New Dictionary(Of String, String) From {
            {"asignatura", subject},
            {"nota", grade}
        }
    End Function

    Private Shared Function BuildFillData(ParamArray rows As IReadOnlyDictionary(Of String, String)()) As FillData
        Return New FillData( _
            New Dictionary(Of String, String) From {{"nombre", "Ana"}},
            {New RowGroupValues(0, rows.ToList())})
    End Function

    Private Shared Function CountOccurrences(text As String, needle As String) As Integer
        Dim count As Integer = 0
        Dim index As Integer = text.IndexOf(needle, StringComparison.Ordinal)
        While index >= 0
            count += 1
            index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal)
        End While
        Return count
    End Function

    <Fact>
    Sub Fill_ZeroRowsRemovesTemplateRow()
        Dim data As FillData = BuildFillData()

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.True(result.Success)
        Dim text As String = ReadMainPartText(result.Document)
        Assert.Contains("Notas de Ana", text)
        Assert.Contains("Asignatura", text)
        Assert.DoesNotContain("$$", text)
        Assert.Equal(1, CountOccurrences(text, "<w:tr>"))
    End Sub

    <Fact>
    Sub Fill_OneRowReplacesTemplateRow()
        Dim data As FillData = BuildFillData(GradeRow("Matemáticas", "9,5"))

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.True(result.Success)
        Dim text As String = ReadMainPartText(result.Document)
        Assert.Contains("Matemáticas", text)
        Assert.Contains("9,5", text)
        Assert.DoesNotContain("$$", text)
        Assert.Equal(2, CountOccurrences(text, "<w:tr>"))
    End Sub

    <Fact>
    Sub Fill_FiveRowsCloneInOrder()
        Dim data As FillData = BuildFillData( _
            GradeRow("A", "1"), _
            GradeRow("B", "2"), _
            GradeRow("C", "3"), _
            GradeRow("D", "4"), _
            GradeRow("E", "5"))

        Dim result As FillResult = DocumentFiller.Fill(GradesTemplate(), data)

        Assert.True(result.Success)
        Dim text As String = ReadMainPartText(result.Document)
        Assert.Equal(6, CountOccurrences(text, "<w:tr>"))
        Assert.True(text.IndexOf(">A<", StringComparison.Ordinal) < text.IndexOf(">B<", StringComparison.Ordinal))
        Assert.True(text.IndexOf(">B<", StringComparison.Ordinal) < text.IndexOf(">C<", StringComparison.Ordinal))
        Assert.True(text.IndexOf(">C<", StringComparison.Ordinal) < text.IndexOf(">D<", StringComparison.Ordinal))
        Assert.True(text.IndexOf(">D<", StringComparison.Ordinal) < text.IndexOf(">E<", StringComparison.Ordinal))
    End Sub

    Private Shared Function SingleRowGroup(groupId As Integer, key As String, value As String) As RowGroupValues
        Dim row As IReadOnlyDictionary(Of String, String) = New Dictionary(Of String, String) From {{key, value}}
        Dim rows As New List(Of IReadOnlyDictionary(Of String, String))
        rows.Add(row)
        Return New RowGroupValues(groupId, rows)
    End Function

    <Fact>
    Sub Fill_TwoGroupsWithSameVariableNameAreIndependent()
        Dim firstTable As String = Table(TableRow(TableCell(PlainParagraph("$$valor$$"))))
        Dim secondTable As String = Table(TableRow(TableCell(PlainParagraph("$$valor$$"))))
        Dim docx As Byte() = BuildDocx(firstTable & secondTable)

        Dim data As New FillData(Nothing, {
            SingleRowGroup(0, "valor", "primero"),
            SingleRowGroup(1, "valor", "segundo")
        })

        Dim result As FillResult = DocumentFiller.Fill(docx, data)

        Assert.True(result.Success)
        Dim text As String = ReadMainPartText(result.Document)
        Assert.Contains("primero", text)
        Assert.Contains("segundo", text)
        Assert.True(text.IndexOf("primero", StringComparison.Ordinal) < text.IndexOf("segundo", StringComparison.Ordinal))
    End Sub

End Class

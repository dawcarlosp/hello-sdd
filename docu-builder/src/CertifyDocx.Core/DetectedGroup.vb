Imports System.Collections.Generic
Imports System.Xml.Linq

Friend Class DetectedGroup

    Public ReadOnly Property Id As Integer
    Public ReadOnly Property TableIndex As Integer
    Public ReadOnly Property RowIndex As Integer
    Public ReadOnly Property TemplateRow As XElement
    Public ReadOnly Property VariableNames As List(Of String)

    Public Sub New(id As Integer, tableIndex As Integer, rowIndex As Integer, templateRow As XElement)
        Me.Id = id
        Me.TableIndex = tableIndex
        Me.RowIndex = rowIndex
        Me.TemplateRow = templateRow
        Me.VariableNames = New List(Of String)
    End Sub

End Class

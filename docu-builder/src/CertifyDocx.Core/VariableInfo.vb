Public Class VariableInfo

    Public ReadOnly Property Name As String
    Public ReadOnly Property Kind As VariableKind
    Public ReadOnly Property RowGroupId As Integer

    Public Sub New(name As String, kind As VariableKind, rowGroupId As Integer)
        Me.Name = name
        Me.Kind = kind
        Me.RowGroupId = rowGroupId
    End Sub

End Class

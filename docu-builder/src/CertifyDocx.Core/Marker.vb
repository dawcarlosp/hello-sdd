Friend Class Marker

    Public ReadOnly Property Name As String
    Public ReadOnly Property Start As Integer
    Public ReadOnly Property [End] As Integer

    Public Sub New(name As String, start As Integer, endIndex As Integer)
        Me.Name = name
        Me.Start = start
        Me.[End] = endIndex
    End Sub

End Class

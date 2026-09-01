Imports System.Collections.Generic

Friend Class MarkerScanResult

    Public ReadOnly Property Markers As IReadOnlyList(Of Marker)
    Public ReadOnly Property Errors As IReadOnlyList(Of String)

    Public Sub New(markers As IReadOnlyList(Of Marker), errors As IReadOnlyList(Of String))
        Me.Markers = markers
        Me.Errors = errors
    End Sub

    Public ReadOnly Property IsValid As Boolean
        Get
            Return Errors.Count = 0
        End Get
    End Property

End Class

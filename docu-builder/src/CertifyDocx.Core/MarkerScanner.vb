Imports System.Collections.Generic

Friend Module MarkerScanner

    Friend Const DollarChar As Char = "$"c
    Friend Const LoneDollarMessage As String = "Marcador $ suelto (sin pareja $$)."
    Friend Const UnclosedMarkerMessage As String = "Marcador $$ sin cerrar."
    Friend Const EmptyNameMessage As String = "Variable con nombre vacío."

    Function Scan(text As String) As MarkerScanResult
        Dim markers As New List(Of Marker)
        Dim errors As New List(Of String)
        Dim openStart As Integer = -1
        Dim i As Integer = 0

        While i < text.Length
            If text(i) = DollarChar Then
                If i + 1 < text.Length AndAlso text(i + 1) = DollarChar Then
                    If openStart = -1 Then
                        openStart = i
                        i += 2
                    Else
                        Dim name As String = text.Substring(openStart + 2, i - (openStart + 2))
                        If name.Length = 0 Then
                            errors.Add(EmptyNameMessage)
                        Else
                            markers.Add(New Marker(name, openStart, i + 2))
                        End If
                        openStart = -1
                        i += 2
                    End If
                Else
                    errors.Add(LoneDollarMessage)
                    i += 1
                End If
            Else
                i += 1
            End If
        End While

        If openStart <> -1 Then
            errors.Add(UnclosedMarkerMessage)
        End If

        Return New MarkerScanResult(markers, errors)
    End Function

End Module

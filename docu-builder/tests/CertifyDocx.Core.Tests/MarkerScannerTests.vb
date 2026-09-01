Imports CertifyDocx.Core
Imports Xunit

Public Class MarkerScannerTests

    <Fact>
    Sub Scan_SingleVariable()
        Dim result As MarkerScanResult = MarkerScanner.Scan("Hola $$nombre$$")

        Assert.True(result.IsValid)
        Assert.Empty(result.Errors)
        Dim marker As Marker = Assert.Single(result.Markers)
        Assert.Equal("nombre", marker.Name)
        Assert.Equal(5, marker.Start)
        Assert.Equal(15, marker.End)
    End Sub

    <Fact>
    Sub Scan_TwoVariables()
        Dim result As MarkerScanResult = MarkerScanner.Scan("$$a$$ y $$b$$")

        Assert.True(result.IsValid)
        Assert.Equal(2, result.Markers.Count)
        Assert.Equal("a", result.Markers(0).Name)
        Assert.Equal("b", result.Markers(1).Name)
    End Sub

    <Fact>
    Sub Scan_NoVariablesIsValid()
        Dim result As MarkerScanResult = MarkerScanner.Scan("solo texto sin marcadores")

        Assert.True(result.IsValid)
        Assert.Empty(result.Errors)
        Assert.Empty(result.Markers)
    End Sub

    <Fact>
    Sub Scan_LoneDollarIsError()
        Dim result As MarkerScanResult = MarkerScanner.Scan("cuesta 5$ hoy")

        Assert.False(result.IsValid)
        Assert.Single(result.Errors)
    End Sub

    <Fact>
    Sub Scan_UnclosedMarkerIsError()
        Dim result As MarkerScanResult = MarkerScanner.Scan("Hola $$nombre")

        Assert.False(result.IsValid)
        Assert.Single(result.Errors)
    End Sub

    <Fact>
    Sub Scan_EmptyNameIsError()
        Dim result As MarkerScanResult = MarkerScanner.Scan("Hola $$$$")

        Assert.False(result.IsValid)
        Assert.Single(result.Errors)
    End Sub

    <Fact>
    Sub Scan_AdjacentVariables()
        Dim result As MarkerScanResult = MarkerScanner.Scan("$$a$$$$b$$")

        Assert.True(result.IsValid)
        Assert.Equal(2, result.Markers.Count)
        Assert.Equal("a", result.Markers(0).Name)
        Assert.Equal("b", result.Markers(1).Name)
    End Sub

    <Fact>
    Sub Scan_VariableNamePreservesCase()
        Dim result As MarkerScanResult = MarkerScanner.Scan("$$Nombre$$")

        Assert.True(result.IsValid)
        Assert.Equal("Nombre", Assert.Single(result.Markers).Name)
    End Sub

End Class

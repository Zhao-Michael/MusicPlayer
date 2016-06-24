Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Module Util

    Public Function chineseToHex(ByVal chinese As String) As String
        Dim bytes As Byte() = Encoding.GetEncoding("gb2312").GetBytes(chinese)
        Dim str As String = ""
        Dim i As Integer
        For i = 0 To bytes.Length - 1
            str = (str & "%" & String.Format("{0:X}", bytes(i)))
        Next i
        Return str
    End Function

    Function HaveChinese(str As String) As Boolean
        For Each x In str
            If AscW(x) > 127 Then
                Return True
            End If
        Next
        Return False
    End Function


    Function DownStringFromNetAsync(adress As String) As String

        Dim http As New WebClient()

        http.Encoding = System.Text.Encoding.GetEncoding("UTF-8")

        Return http.DownloadDataTaskAsync(adress).Result.ToString()

    End Function


    Function DownStringFromNet(adress As String) As String

        Dim http As New WebClient()

        http.Encoding = System.Text.Encoding.GetEncoding("UTF-8")

        Return http.DownloadData(adress).ToString()

    End Function



End Module

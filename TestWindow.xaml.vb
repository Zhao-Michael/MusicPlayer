Imports System
Imports System.IO
Imports System.Text.RegularExpressions

Public Class TestWindow

    'http://ws.itwusun.com/fsong/tt/id_36444942.html

    Private Sub btn1_Click(sender As Object, e As RoutedEventArgs)

        Dim s = "http://ws.itwusun.com/fsong/kg/id_a781642ae8a39c4146dedbde38ca7490.html"

        'File.WriteAllText("D:\\mp3.txt", Net.DownStringFromNet(s))

        Dim html = Net.DownStringFromNet(s)

        Dim result = Regex.Match(html, "mp3.+播放地址")

        Console.WriteLine(result.Value)






    End Sub


    Function hah() As String

    End Function

    Private Sub btn2_Click(sender As Object, e As RoutedEventArgs)

        'File.WriteAllText("D:\\2.txt", Net.Util.DownStringFromNet("http://ws.itwusun.com/search/song/taylor%20swift%20red%2022/p/2"))


        Dim method As Func(Of String)

        method = AddressOf hah


        Dim t = Task.Run(New Action(Sub()

                                        Threading.Thread.Sleep(2000)

                                        Console.WriteLine("Task Run...")

                                    End Sub))




    End Sub

    Private Sub btn3_Click(sender As Object, e As RoutedEventArgs)
        'File.WriteAllText("D:\\3.txt", Net.Util.DownStringFromNet("http://ws.itwusun.com/fsong/qq/id_5028625.html"))

        Dim s = "abc-list-iop"

        Dim arr = s.Split("list".ToCharArray())

    End Sub
End Class

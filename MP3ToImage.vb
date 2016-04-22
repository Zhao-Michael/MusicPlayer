Imports System.IO
Imports System.Windows.Media
Imports System.Drawing

Module MP3ToBitmapImage

    Function get_major_color(bitmap As Bitmap) As System.Windows.Media.Color

        '色调的总和
        Dim sum_hue = 0
        '色差的阈值
        Dim threshold = 30
        '计算色调总和
        For h = 0 To bitmap.Height - 1
            For w = 1 To bitmap.Width - 1
                Dim hue = bitmap.GetPixel(w, h).GetHue()
                sum_hue += hue
            Next
        Next


        Dim avg_hue = sum_hue / (bitmap.Width * bitmap.Height)

        '色差大于阈值的颜色值
        Dim rgbs As New List(Of System.Drawing.Color)()

        For h = 0 To bitmap.Height - 1
            For w = 1 To bitmap.Width - 1
                Dim color = bitmap.GetPixel(w, h)
                Dim hue = color.GetHue
                '如果色差大于阈值，则加入列表
                If Math.Abs(hue - avg_hue) > threshold Then
                    rgbs.Add(color)
                End If
            Next
        Next


        If (rgbs.Count = 0) Then
            Return System.Windows.Media.Colors.Red
        End If

        '计算列表中的颜色均值，结果即为该图片的主色调
        Dim sum_r = 0, sum_g = 0, sum_b = 0

        For Each m_rgb In rgbs
            sum_r += m_rgb.R
            sum_g += m_rgb.G
            sum_b += m_rgb.B
        Next

        Return System.Windows.Media.Color.FromArgb(255, sum_r / rgbs.Count, sum_g / rgbs.Count, sum_b / rgbs.Count)

    End Function


    Function GetMusicImage(ByVal path As String) As BitmapImage

        Dim temp = Mp3ToImage(path)
        If temp IsNot Nothing Then
            Return temp
        End If
        Return New BitmapImage(New Uri("Default_MusciImage.png", UriKind.RelativeOrAbsolute))
    End Function

    'Private returnnothingpic As String
    Private Function Mp3ToImage(ByVal path As String) As BitmapImage
        Try
            Dim btnACPI As Integer

            Dim btnStart As Integer

            Dim btnExit As Integer

            Dim sr As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

            Dim DAT(sr.Length - 1) As Byte

            sr.Read(DAT, 0, sr.Length)

            sr.Close()

            For i = 0 To UBound(DAT)

                If i + 3 <= UBound(DAT) Then

                    If Hex(DAT(i)) & Hex(DAT(i + 1)) & Hex(DAT(i + 2)) & Hex(DAT(i + 3)) = "41504943" And btnACPI = 0 Then

                        btnACPI = i + 3

                        btnExit = DAT(btnACPI + 1) * (16777216.0#)

                        btnExit = btnExit + DAT(btnACPI + 2) * (65536.0#)    '计算帧大小 

                        btnExit = btnExit + DAT(btnACPI + 3) * (256.0#)

                        btnExit = btnExit + DAT(btnACPI + 4) + 61440

                    End If

                End If

                If btnACPI <> 0 Then

                    If i + 3 <= UBound(DAT) Then

                        If Hex(DAT(i)) & Hex(DAT(i + 1)) = "FFD8" And btnStart = 0 Then

                            btnStart = i

                            Exit For

                        End If

                    End If

                End If
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                If i > 5000 Then sr.Close() : Return Nothing

            Next i

            Dim sDAT(btnExit - btnStart) As Byte

            For i = 0 To UBound(sDAT)

                sDAT(i) = DAT(i + btnStart)

            Next i

            If sDAT.Length <= 1 Then sr.Close() : Return Nothing

            Dim ms2 As New MemoryStream
            ms2.Write(sDAT, 0, sDAT.Length)

            Dim myBitmapImage As BitmapImage = New BitmapImage()

            ms2.Seek(0, SeekOrigin.Begin)
            myBitmapImage.BeginInit()
            myBitmapImage.StreamSource = ms2
            myBitmapImage.CacheOption = BitmapCacheOption.OnLoad
            myBitmapImage.EndInit()
            ms2.Close()

            Return myBitmapImage

        Catch ex As Exception
            Return Nothing

        End Try
    End Function



End Module

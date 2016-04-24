Imports System.Text.RegularExpressions
Imports System.Net
Imports System.Windows.Media.Animation
Imports System.IO

Public Class SearchLRC
    Dim IsCloseFormReal As Boolean = False

    Dim music As Music

    Public mainwin As MainWindow

    Sub New(_music As Music, mainform As MainWindow)

        mainwin = mainform

        InitializeComponent()

        Background = mainwin.nowcolor_rect.Fill

        music = _music
    End Sub

    Private Sub Grid_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.LeftButton = MouseButtonState.Pressed Then
            Me.DragMove()
        End If

    End Sub

    Private Sub GetBaiDuLrc(_title As String, _artist As String)

        Dim sw_ As New Stopwatch()

        sw_.Start()

        Dim http As New WebClient()

        http.Encoding = System.Text.Encoding.GetEncoding("UTF-8")

        Dim list As New List(Of LrcUrlInfo)

        If _artist <> "" Then _artist = "+" + _artist

        Dim lrc_uri As String = "http://music.baidu.com/search/lrc?key=" + _title.Replace(" ", "+") + _artist.Replace(" ", "+")
        '+ TextBox1.Text.Replace(" ", "+")

        Dim http_downstr = http.DownloadString(lrc_uri)

        Dim r = Regex.Matches(http_downstr, "(/data2/lrc/).{1,100}(\.lrc)")

        For Each x In r
            list.Add(New LrcUrlInfo() With {.url = "http://music.baidu.com" + x.ToString()})
        Next

        Dim _list As New List(Of LrcUrlInfo)

        For Each item In list
            Dim temp As New WebClient()
            temp.Encoding = System.Text.Encoding.GetEncoding("UTF-8")

            temp.DownloadStringAsync(New Uri(item.url))
            AddHandler temp.DownloadStringCompleted, New DownloadStringCompletedEventHandler(Sub(obj As Object, e1 As DownloadStringCompletedEventArgs)
                                                                                                 Try
                                                                                                     _list.Add(New LrcUrlInfo() With {.url = item.url, .content = e1.Result})

                                                                                                 Catch ex As Exception

                                                                                                 End Try

                                                                                             End Sub)
        Next

        Task.Factory.StartNew(Sub()
                                  While _list.Count < list.Count
                                      System.Threading.Thread.Sleep(100)
                                  End While

                                  GetRealLRC(_list, list, sw_)

                              End Sub)

    End Sub

    Sub GetRealLRC(_list As List(Of LrcUrlInfo), list As List(Of LrcUrlInfo), sw_ As Stopwatch)

        '获得全部歌词信息

        Me.Dispatcher.BeginInvoke(New Action(Sub()

                                                 sw_.Stop()

                                                 labelResult.Visibility = Windows.Visibility.Visible
                                                 labelResult.Text = sw_.ElapsedMilliseconds.ToString() + " ms 内找到了 " + _list.Count.ToString() + " 个歌词"

                                                 ellipse_Grid.Visibility = Windows.Visibility.Collapsed

                                                 Dim sb1 As Storyboard = FindResource("serarchsb1") : sb1.Stop()
                                                 Dim sb2 As Storyboard = FindResource("serarchsb2") : sb2.Stop()
                                                 Dim sb3 As Storyboard = FindResource("serarchsb3") : sb3.Stop()
                                                 Dim sb4 As Storyboard = FindResource("serarchsb4") : sb4.Stop()
                                                 Dim sb5 As Storyboard = FindResource("serarchsb5") : sb5.Stop()


                                                 For Each item In _list

                                                     Dim textbox As New ContentControl

                                                     textbox.Style = FindResource("LRCDisplay")

                                                     textbox.Content = item.content

                                                     textbox.Tag = item.url

                                                     wrappanel.Children.Add(textbox)

                                                 Next



                                             End Sub))


        Return
    End Sub


    Private Sub borderClose_MouseDown(sender As Object, e As MouseButtonEventArgs)
        Close()
    End Sub

    Private Sub SerachLRC_Loaded(sender As Object, e As RoutedEventArgs)
        Dim mywin As DependencyObject = VisualTreeHelper.GetChild(Me, 0)
        Dim maingrid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MainGrid")
        Dim sb As Storyboard = maingrid.FindResource("LoadWinAni")

        wrappanel.Children.Clear()


        labelResult.Text = ""

        sb.Begin()

    End Sub

    Private Sub SerachLRC_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles SerachLRC.Closing

        If IsCloseFormReal Then
            e.Cancel = False
            Return
        End If

        e.Cancel = True
        Dim mywin As DependencyObject = VisualTreeHelper.GetChild(Me, 0)
        Dim maingrid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MainGrid")
        Dim sb As Storyboard = maingrid.FindResource("UnLoadWinAni")
        AddHandler sb.Completed, AddressOf UnLoadWinAni_Completed
        sb.Begin()
    End Sub

    Private Sub UnLoadWinAni_Completed(sender As Object, e As EventArgs)

        IsCloseFormReal = True

        DialogResult = True

        Close()

    End Sub

    Dim isAniruning As Boolean = False

    Public Sub btnSerach_Click(sender As Object, e As RoutedEventArgs)
        wrappanel.Children.Clear()

        If textboxTitle.Text = "" OrElse textboxTitle.Text.Replace(" ", "") = "" Then Return

        labelResult.Text = ""

        Dim title = textboxTitle.Text.Trim()
        Dim artist = textboxArtist.Text.Trim()

        If artist.Contains("未知歌手") Then artist = ""

        Task.Factory.StartNew(Sub()

                                  GetBaiDuLrc(title, artist)

                              End Sub)

        ellipse_Grid.Visibility = Windows.Visibility.Visible
        labelResult.Visibility = Windows.Visibility.Hidden

        Dim sb1 As Storyboard = FindResource("serarchsb1")
        Dim sb2 As Storyboard = FindResource("serarchsb2")
        Dim sb3 As Storyboard = FindResource("serarchsb3")
        Dim sb4 As Storyboard = FindResource("serarchsb4")
        Dim sb5 As Storyboard = FindResource("serarchsb5")

        sb1.Begin()
        sb2.Begin()
        sb3.Begin()
        sb4.Begin()
        sb5.Begin()

        isAniruning = True
    End Sub

    Private Sub lrcBorder_MouseDown(sender As Object, e As MouseButtonEventArgs)
        Dim mywin As DependencyObject = VisualTreeHelper.GetChild(Me, 0)
        Dim maingrid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MainGrid")
        Dim sb As Storyboard = maingrid.FindResource("ShowMsgAni")

        Dim border As Grid = sender
        If border IsNot Nothing Then

            Dim content As ContentControl = border.TemplatedParent

            Dim uri As String = content.Tag.ToString()

            Dim lrc_uri As String = ""

            If music IsNot Nothing AndAlso music.MusicTitle = textboxTitle.Text AndAlso textboxArtist.Text = music.MusicAuthors Then

                lrc_uri = Path.GetDirectoryName(music.MusicLoc) + "\" + Path.GetFileNameWithoutExtension(music.MusicLoc) + ".lrc"

                If File.Exists(lrc_uri) Then
                    File.Delete(lrc_uri)
                End If


                My.Computer.Network.DownloadFile(uri, lrc_uri)

                Dim msg As TextBlock = maingrid.FindName("textMessage")

                msg.Text = "下载成功！" + vbCrLf + "位置： " + lrc_uri

                sb.Begin()

                If music.MusicLoc = mainwin.MusicPlayer.Source.OriginalString Then

                    mainwin.LoadLRC()

                End If



            Else

showdialog:

                Dim saveDialog As New Forms.SaveFileDialog

                saveDialog.Title = "请选择歌词保存位置"

                saveDialog.DefaultExt = "*.lrc"

                saveDialog.AddExtension = True

                saveDialog.InitialDirectory = My.Settings.MusicLoc

                saveDialog.FileName = textboxTitle.Text + " - " + textboxArtist.Text + ".lrc"

                If textboxArtist.Text = "" Then
                    saveDialog.FileName = textboxTitle.Text + ".lrc"
                End If

                If saveDialog.ShowDialog() = Forms.DialogResult.OK Then

                    If saveDialog.FileName = "" OrElse Not saveDialog.FileName.EndsWith(".lrc") OrElse Not saveDialog.FileName.EndsWith(".LRC") Then

                        If File.Exists(lrc_uri) Then
                            File.Delete(lrc_uri)
                        End If

                        My.Computer.Network.DownloadFile(uri, saveDialog.FileName)

                        Dim msg As TextBlock = maingrid.FindName("textMessage")

                        msg.Text = "下载成功！" + vbCrLf + "位置： " + saveDialog.FileName

                        sb.Begin()

                    Else
                        GoTo showdialog

                    End If


                End If






            End If




        End If






    End Sub



    Private Sub textbox_KeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then
            btnSerach_Click(Nothing, Nothing)
        End If

    End Sub
End Class

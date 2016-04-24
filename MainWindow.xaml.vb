Imports System.Text

#Const CoreAudioApi = True
#Const UseWndProc = True
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.IO
Imports Shell32
Imports Microsoft.Win32
Imports System.Windows.Threading
Imports System.Windows.Controls.Primitives
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.ComponentModel
Imports System.Net
Imports System.Text.RegularExpressions
#If CoreAudioApi Then
Imports CoreAudioApi
#End If
Imports System.Threading

Class MainWindow



#Region "之前的重写窗口消息"


#If UseWndProc Then

    '重写窗口呈现
    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        Dim hwndSource As HwndSource = PresentationSource.FromVisual(Me)
        If hwndSource IsNot Nothing Then
            hwndSource.AddHook(New HwndSourceHook(AddressOf WndProc))
        End If
    End Sub



    'Public Const WM_RBUTTONDOWN = &H204      '当鼠标右键在窗口客户区按下

    'Public Const WM_NCRBUTTONDOWN = &HA4
    'Public Const WM_SYSCOMMAND = &H112

    Const WM_NCRBUTTONUP = &HA5               '当鼠标右键在非窗口客户区松开

    '重写消息循环
    Protected Overridable Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        Select Case msg
            Case WM_NCRBUTTONUP

                Dim title_ContextMenu As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")

                title_ContextMenu.ContextMenu.IsOpen = True

                handled = True

            Case Else
                handled = False

        End Select

    End Function

#End If


#End Region


    '重写窗口布局变化
    Protected Overrides Function ArrangeOverride(arrangeBounds As Size) As Size
        mywin = VisualTreeHelper.GetChild(MainWindow, 0)
        Dim MainBorder As Border = LogicalTreeHelper.FindLogicalNode(mywin, "MainBorder")

        If WindowState = WindowState.Maximized Then
            MainBorder.Margin = New Thickness(7)
        Else
            MainBorder.Margin = New Thickness(0)
        End If

        Return MyBase.ArrangeOverride(arrangeBounds)
    End Function

    '==============================程序定义的变量==========================================

    Dim mywin As DependencyObject          '用于 查找 模块 控件

    Dim Isrndplay As Boolean = False

    Dim rndmusiclist As ArrayList          '随机 列表

    Dim IsUserChangeSystemVol As Boolean = True  '控制 调节 系统音量  

    Dim IsMouseDownOnSlider As Boolean = False  '控制调节slider进度

#If CoreAudioApi Then
    Dim sysvoldevice As New MMDeviceEnumerator  '获取系统音量
#End If

    Dim AllMusicList As New MusicList      '该list是主要的歌曲列表，显示在主程序的大的listbox里的数据

    Dim SearchMusicList As New MusicList   '搜索列表

    Public m_Lrc_List As New LrcData_List   '储存歌词的列表

    Dim Lrc_Map As Dictionary(Of TimeSpan, String)  '储存歌词的 Map

    Dim Thread_Lrc As Thread           '显示歌词的Timer

    Dim Thread_RefreshProgress As Thread   '刷新进度条的线程

    Dim Thread_LoadMusicInfo As Thread     '加载歌曲信息的线程

    Dim LoadForm As New LoadForm

    Dim currentPos As TimeSpan

    Public IsFirstRun As Boolean = True

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded

        LoadForm.Show()

        mylistbox.DisplayMemberPath = "MusicNameAndAuthors"
        mylistbox.ItemsSource = AllMusicList

        Thread_LoadMusicInfo = New Thread(New ThreadStart(AddressOf LoadMp3Info)) With {.IsBackground = True}
        Thread_LoadMusicInfo.SetApartmentState(ApartmentState.STA)
        Thread_LoadMusicInfo.Start()


        LoadLastSetting()


        ''歌词 Timer
        'Thread_Lrc = New Thread(AddressOf LrcThread_Tick)
        'Thread_Lrc.IsBackground = True
        'Thread_Lrc.Start()

        m_LrcPanel.mainwin = Me

        ''音量 Timer
        'VolTimer.Interval = TimeSpan.FromSeconds(0.1)
        'AddHandler VolTimer.Tick, AddressOf VolTimer_Tick


        '刷新 播放进度条
        Thread_RefreshProgress = New Thread(New ThreadStart(AddressOf RefreshProgressThread)) With {.IsBackground = True}
        Thread_RefreshProgress.Start()


    End Sub


#Region "加载函数"

    Sub LoadDefalutSetting()
        Title = "我的音乐播放器"
        MusicPlayer.Source = Nothing
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")
        MusicPlayButton.IsChecked = False

        nowcolor_rect.Fill = New SolidColorBrush(Color.FromArgb(255, 227, 20, 0))
        sliderProgress.Value = 0
        SliderWidth.Maximum = SystemParameters.PrimaryScreenWidth
        SliderHeight.Maximum = SystemParameters.PrimaryScreenHeight

        For Each x As Rectangle In Rect_Parent.Children
            AddHandler x.MouseDown, AddressOf Rect_MouseDown
        Next
        Dim s = CType(nowcolor_rect.Fill, SolidColorBrush)

        Dim _color = s.Color


        sliderR.Value = _color.R
        sliderG.Value = _color.G
        sliderB.Value = _color.B
        sliderA.Value = _color.A

        Resources("MainColor") = Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value)
        Resources("MainColorBrush") = New SolidColorBrush(Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value))

        My.Settings.Reset()

#If CoreAudioApi Then
        Try
            '关联系统声音到本程序
            Dim voldecice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
            If voldecice IsNot Nothing Then
                AddHandler voldecice.AudioEndpointVolume.OnVolumeNotification, AddressOf 获取系统主音量
            End If

            '初始化 音量面板系统音量 
            Dim sysvolchecked As CheckBox = LogicalTreeHelper.FindLogicalNode(mywin, "sysvolchecked")
            Dim systemvolslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "systemvolslider")

            Dim mmdevice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)

            sysvolchecked.IsChecked = mmdevice.AudioEndpointVolume.Mute
            systemvolslider.Value = mmdevice.AudioEndpointVolume.MasterVolumeLevelScalar
        Catch ex As Exception
        End Try
#End If

        '初始化 控件模板 的引用
        Dim m_Background_Image As Image = LogicalTreeHelper.FindLogicalNode(mywin, "m_BackgroundImage")

        m_Background_Image.Source = New BitmapImage((New Uri("Frozen.jpg", UriKind.Relative)))
    End Sub


    Sub LoadLastSetting()
        '载入音量
        Dim volslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "volslider")
        volslider.Value = My.Settings.Vol

        '载入上次播放的音乐
        If My.Settings.MusicLoc <> "" Then
            MusicPlayer.Source = New Uri(My.Settings.MusicLoc, UriKind.Absolute)
            MusicPlayer.Position = My.Settings.MusicPos
            Backgeffect.Value = My.Settings.Blur
            Backgopacity.Value = My.Settings.Opacity
            textMusicPos.Text = My.Settings.textMusicPos
            Title = My.Settings.MusicTitle
            sliderProgress.Maximum = My.Settings.MucisTotal

            If Double.IsNaN(My.Settings.MusicPercent) Then
                My.Settings.MusicPercent = 0
            End If
            sliderProgress.Value = My.Settings.MusicPercent * My.Settings.MucisTotal

        End If


        '程序主体颜色设置
        If My.Settings.mycolor.IsEmpty = False Then
            nowcolor_rect.Fill = New SolidColorBrush(Color.FromArgb(My.Settings.mycolor.A, My.Settings.mycolor.R, My.Settings.mycolor.G, My.Settings.mycolor.B))
        Else
            nowcolor_rect.Fill = New SolidColorBrush(Color.FromArgb(255, 227, 20, 0))
        End If

        SliderWidth.Maximum = SystemParameters.PrimaryScreenWidth
        SliderHeight.Maximum = SystemParameters.PrimaryScreenHeight

        For Each x As Rectangle In Rect_Parent.Children
            AddHandler x.MouseDown, AddressOf Rect_MouseDown
        Next
        Dim s = CType(nowcolor_rect.Fill, SolidColorBrush)

        Dim _color = s.Color


        sliderR.Value = _color.R
        sliderG.Value = _color.G
        sliderB.Value = _color.B
        sliderA.Value = _color.A

        Resources("MainColor") = Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value)
        Resources("MainColorBrush") = New SolidColorBrush(Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value))

#If CoreAudioApi Then

        Try
            '关联系统声音到本程序
            Dim voldecice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
            If voldecice IsNot Nothing Then
                AddHandler voldecice.AudioEndpointVolume.OnVolumeNotification, AddressOf 获取系统主音量
            End If

            '初始化 音量面板系统音量 
            Dim sysvolchecked As CheckBox = LogicalTreeHelper.FindLogicalNode(mywin, "sysvolchecked")
            Dim systemvolslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "systemvolslider")

            Dim mmdevice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)

            sysvolchecked.IsChecked = mmdevice.AudioEndpointVolume.Mute
            systemvolslider.Value = mmdevice.AudioEndpointVolume.MasterVolumeLevelScalar
        Catch ex As Exception
        End Try

#End If

        '初始化 控件模板 的引用

        Dim m_Background_Image As Image = LogicalTreeHelper.FindLogicalNode(mywin, "m_BackgroundImage")


        Dim sBackgroundImage As String = My.Settings.BackgroundImagePath
        If sBackgroundImage IsNot Nothing Then
            If File.Exists(sBackgroundImage) Then
                m_Background_Image.Source = New BitmapImage((New Uri(sBackgroundImage, UriKind.RelativeOrAbsolute)))
            Else
                m_Background_Image.Source = New BitmapImage((New Uri("Frozen.jpg", UriKind.Relative)))
            End If
        Else
            m_Background_Image.Source = New BitmapImage((New Uri("Frozen.jpg", UriKind.Relative)))
        End If

    End Sub


    Sub dll()
        Dim restart_bool As Boolean = False
        Dim resources As System.Resources.ResourceManager = My.Resources.ResourceManager
        If File.Exists(Environment.CurrentDirectory + "\Interop.Shell32.dll") = False Then
            If Environment.OSVersion.Version.Major >= 7 Then
                Dim b() As Byte = resources.GetObject("Interop.Shell32_Win8")
                Dim s As IO.Stream = File.Create(Environment.CurrentDirectory + "\Interop.Shell32.dll") '要保存的路径
                s.Write(b, 0, b.Length)
                s.Close()
            Else
                Dim b() As Byte = resources.GetObject("Interop_Shell32_Win7")
                Dim s As IO.Stream = File.Create(Environment.CurrentDirectory + "\Interop.Shell32.dll") '要保存的路径
                s.Write(b, 0, b.Length)
                s.Close()
            End If
            restart_bool = True
        Else
            Dim dllfile As New FileInfo(Environment.CurrentDirectory + "\Interop.Shell32.dll")
            If dllfile.Length = 36864 Then
                If Environment.OSVersion.Version.Major >= 7 Then
                    dllfile.Delete()
                    Dim b() As Byte = resources.GetObject("Interop_Shell32_Win8")
                    Dim s As IO.Stream = File.Create(Environment.CurrentDirectory + "\Interop.Shell32.dll") '要保存的路径
                    s.Write(b, 0, b.Length)
                    s.Close()
                    restart_bool = True
                End If
            ElseIf dllfile.Length = 38912 Then
                If Environment.OSVersion.Version.Major = 6 Then
                    dllfile.Delete()
                    Dim b() As Byte = resources.GetObject("Interop_Shell32_Win7")
                    Dim s As IO.Stream = File.Create(Environment.CurrentDirectory + "\Interop.Shell32.dll") '要保存的路径
                    s.Write(b, 0, b.Length)
                    s.Close()
                    restart_bool = True
                End If
            End If
        End If


        If File.Exists(Environment.CurrentDirectory + "\CoreAudioApi.dll") = False Then
            Dim b() As Byte = resources.GetObject("CoreAudioApi")
            Dim s As IO.Stream = File.Create(Environment.CurrentDirectory + "\CoreAudioApi.dll") '要保存的路径
            s.Write(b, 0, b.Length)
            s.Close()
            restart_bool = True
        End If

        If restart_bool = True Then System.Windows.Forms.Application.Restart() : Application.Current.Shutdown()

    End Sub

    'Dim OpenFile As New OpenFileDialog

    ''' <summary>
    ''' 加载命令行
    ''' </summary>
    Sub LoadCmdLine()
        If Environment.GetCommandLineArgs.Length >= 2 Then
            If Path.GetExtension(Environment.GetCommandLineArgs(1)).ToLower.Contains("mp3") Then
                If File.Exists(Environment.GetCommandLineArgs(1)) Then
                    Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")

                    Dim sh As New ShellClass
                    Dim dir As Folder = sh.NameSpace(Path.GetDirectoryName(Environment.GetCommandLineArgs(1)))
                    Dim item As FolderItem = dir.ParseName(Path.GetFileName(Environment.GetCommandLineArgs(1)))

                    If dir.GetDetailsOf(item, 21).Contains("-") Then
                        Title = dir.GetDetailsOf(item, 21)

                    Else
                        Title = dir.GetDetailsOf(item, 21) + " - " + dir.GetDetailsOf(item, 20)
                    End If

                    MusicImage.Source = GetMusicImage(Environment.GetCommandLineArgs(1))
                    If MusicImage Is Nothing Then
                        MusicImage.Source = New BitmapImage(New Uri("Default_MusciImage.png", UriKind.RelativeOrAbsolute))
                    End If

                    MusicPlayer.Source = New Uri(Environment.GetCommandLineArgs(1))
                    MusicPlayer.Play()
                    MusicPlayButton.ToolTip = "暂停"
                    MusicPlayButton.IsChecked = True
                    TaskBarPlayBtn.ImageSource = New BitmapImage(New Uri("pack://application:,,,/Resources/pause.ico"))
                End If
            End If
        End If

    End Sub


#End Region


#Region "线程"

    Dim sw As New Stopwatch()

    ''显示歌词线程
    'Sub LrcThread_Tick()
    '    ''''''''''''''显示 歌词相关''''''''''''''''''''''''
    '    While True

    '        Dim i As Integer = 0
    '        If LrcList.Count <> 0 Then
    '            For i = 1 To LrcList.Count
    '                If Mid(currentPos.ToString, 4, 5) = Mid(LrcList(i - 1), 1, 5) Then

    '                    Me.Dispatcher.Invoke(Sub()
    '                                             LrcTextBlock.Text = LrcList(i - 1).ToString.Split("]")(1)
    '                                             LrcTextBlock.InvalidateVisual()

    '                                         End Sub)
    '                End If
    '            Next
    '        End If

    '        Thread.Sleep(300)
    '    End While
    'End Sub


    Sub RefreshProgressThread()

        While True
            Dispatcher.Invoke(New Action(Sub()
                                             If MusicPlayer.Source IsNot Nothing And MusicPlayer.NaturalDuration.HasTimeSpan Then
                                                 If IsMouseDownOnSlider = False Then
                                                     sliderProgress.Value = MusicPlayer.Position.TotalSeconds
                                                 End If
                                                 sliderProgress.Maximum = MusicPlayer.NaturalDuration.TimeSpan.TotalSeconds
                                                 textMusicPos.Text = MusicPlayer.Position.Minutes.ToString() + ":" + MusicPlayer.Position.Seconds.ToString("00.#") + " / " + MusicPlayer.NaturalDuration.TimeSpan.Minutes.ToString() + ":" + MusicPlayer.NaturalDuration.TimeSpan.Seconds.ToString("00.#")
                                                 taskbar.ProgressValue = sliderProgress.Value / sliderProgress.Maximum
                                                 currentPos = MusicPlayer.Position
                                             End If
                                         End Sub))
            Thread.Sleep(1000)
        End While
    End Sub


    Private Sub LoadMp3Info()

        Dispatcher.Invoke(New Action(Sub()
                                         AllMusicList.Clear()
                                     End Sub))

        If My.Settings.MusicPaths Is Nothing OrElse My.Settings.MusicPaths = "" Then

            Dim FBDialog As New Forms.FolderBrowserDialog()
            FBDialog.ShowNewFolderButton = False
            If FBDialog.ShowDialog = Forms.DialogResult.OK Then


                My.Settings.MusicPaths = FBDialog.SelectedPath
                My.Settings.Save()
            End If

        End If


        If Directory.Exists(My.Settings.MusicPaths) Then
            For Each path As String In Directory.GetFiles(My.Settings.MusicPaths, "*.mp3", SearchOption.AllDirectories)
                Dispatcher.Invoke(New Action(Sub()
                                                 AllMusicList.Add(New Music() With {.MusicLoc = path})
                                             End Sub))
            Next
        End If


        Parallel.ForEach(AllMusicList, Sub(temp_music As Music)
                                           Dim dir As Folder = Nothing
                                           Dim item As FolderItem = Nothing
                                           Me.Dispatcher.Invoke(New Action(Sub()
                                                                               Dim sh As New ShellClass
                                                                               dir = sh.NameSpace(Path.GetDirectoryName(temp_music.MusicLoc))
                                                                               item = dir.ParseName(Path.GetFileName(temp_music.MusicLoc))
                                                                           End Sub))

                                           temp_music.MusicAuthors = dir.GetDetailsOf(item, 20)
                                           temp_music.MusicBitrate = dir.GetDetailsOf(item, 28)
                                           temp_music.MusicGenre = dir.GetDetailsOf(item, 16)
                                           temp_music.MusicLength = dir.GetDetailsOf(item, 27)
                                           temp_music.MusicSize = dir.GetDetailsOf(item, 1)
                                           temp_music.MusicAlbum = dir.GetDetailsOf(item, 14)
                                           temp_music.MusicYear = dir.GetDetailsOf(item, 15)
                                           temp_music.MusicTitle = dir.GetDetailsOf(item, 21)

                                           If temp_music.MusicTitle = "" Then temp_music.MusicTitle = Path.GetFileNameWithoutExtension(temp_music.MusicLoc)

                                           If temp_music.MusicTitle.Contains("-") Then
                                               temp_music.MusicNameAndAuthors = temp_music.MusicTitle
                                           Else
                                               temp_music.MusicNameAndAuthors = temp_music.MusicTitle + " - " + temp_music.MusicAuthors
                                           End If

                                       End Sub)


        For index = 0 To AllMusicList.Count - 1
            Dim temp As Int16 = index
            If AllMusicList(index).MusicLoc = My.Settings.MusicLoc Then
                Me.Dispatcher.Invoke(New Action(Sub()
                                                    playingitem.Text = (1 + temp).ToString()
                                                End Sub))
                Exit For
            End If

        Next

        Dispatcher.Invoke(New Action(Sub()


                                         在列表框中选中要播放的歌曲()

                                         Visibility = Visibility.Visible
                                         MusicPathTextBlock.Text = My.Settings.MusicPaths


                                         '重新加载就按歌名升序排序
                                         AllMusicList.Sort("MusicTitle", 1)

                                         mylistbox.ItemsSource = AllMusicList

                                         mylistbox.InvalidateArrange()

                                         If LoadForm IsNot Nothing Then
                                             LoadForm.Close()
                                         End If



                                         If IsFirstRun Then

                                             IsFirstRun = False

                                             Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2

                                             Top = (SystemParameters.PrimaryScreenHeight - ActualHeight) / 2

                                             WindowState = WindowState.Normal

                                             Me.Activate()

                                         End If

                                         IsFirstRun = False

                                     End Sub))

        Return
    End Sub

#End Region


#Region "单击颜色方块，改变主题颜色相关"

    Private Sub Rect_MouseDown(sender As Object, e As MouseButtonEventArgs)

        Me.Resources("MainColorBrush") = CType(sender, Rectangle).Fill

        Dim s As String = CType(sender, Rectangle).Fill.ToString

        Dim _color As SolidColorBrush = CType(sender, Rectangle).Fill

        Dim a = _color.Color.A
        Dim r = _color.Color.R
        Dim g = _color.Color.G
        Dim b = _color.Color.B

        Me.Resources("MainColor") = Color.FromArgb(a, r, g, b)
        Me.Resources("MainColorBrush") = New SolidColorBrush(Color.FromArgb(a, r, g, b))
        nowcolor_rect.Fill = New SolidColorBrush(Color.FromArgb(a, r, g, b))

        sliderR.Value = r
        sliderG.Value = g
        sliderB.Value = b
        sliderA.Value = a



    End Sub



    Private Sub sliderR_MouseMove(sender As Object, e As System.Windows.Input.MouseEventArgs) Handles sliderR.MouseMove, sliderG.MouseMove, sliderB.MouseMove, sliderA.MouseMove
        If e.LeftButton = MouseButtonState.Pressed Then
            Me.Resources("MainColor") = Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value)
            Me.Resources("MainColorBrush") = New SolidColorBrush(Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value))
        End If

    End Sub

    '重置背景色
    Private Sub ResetBackImage_MouseDown(sender As Object, e As MouseButtonEventArgs)
        Dim m_Background_Image As Image = LogicalTreeHelper.FindLogicalNode(mywin, "m_BackgroundImage")
        m_Background_Image.Source = New BitmapImage(New Uri("pack://application:,,,/Frozen.jpg", UriKind.RelativeOrAbsolute))
        My.Settings.BackgroundImagePath = "pack://application:,,,/Frozen.jpg"
    End Sub

    '改变背景图片
    Private Sub ChangeBackImg_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles ChangeBackImg.MouseDown


        Dim m_Background_Image As Image = LogicalTreeHelper.FindLogicalNode(mywin, "m_BackgroundImage")
        Dim open As New Microsoft.Win32.OpenFileDialog
        open.Title = "选择背景图片"
        open.CheckFileExists = True
        open.Multiselect = False
        open.Filter = "图片文件|*.jpg;*.png;*.bmp|所有文件|*.*"
        open.ShowDialog()
        If open.FileName = "" Then Exit Sub
        Dim backimg As New ImageBrush(New BitmapImage(New Uri(open.FileName)))
        backimg.Stretch = Stretch.UniformToFill
        m_Background_Image.Source = New BitmapImage(New Uri(open.FileName, UriKind.RelativeOrAbsolute))

        My.Settings.BackgroundImagePath = open.FileName
        My.Settings.Save()


    End Sub

#End Region



#Region "listbox右上角3的数字——操作"
    '左击最左边按钮 将listbox 使正在播放项 获焦 可视
    Private Sub playingitem_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles playingitem.MouseDown
        If e.ClickCount >= 1 And MusicPlayer.Source IsNot Nothing Then
            Dim n As Integer
            For Each x As Music In mylistbox.Items
                n += 1
                If MusicPlayer.Source.OriginalString = x.MusicLoc Then
                    mylistbox.ScrollIntoView(mylistbox.Items(n - 1))
                    mylistbox.SelectedItem = mylistbox.Items(n - 1)
                    playingitem.ToolTip = "正在播放第 " + (n).ToString + " 首歌：" + vbCrLf + x.MusicNameAndAuthors
                    Exit Sub
                End If
            Next
            '正在播放的项被删除之后：
            mylistbox.ScrollIntoView(mylistbox.Items(playingitem.Text - 1))
            mylistbox.SelectedItem = mylistbox.Items(playingitem.Text - 1)

        End If

    End Sub

    '左击中间按钮 将listbox 使选中项 获焦 可视
    Private Sub txtSelectedIndex_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles txtSelectedIndex.MouseDown

        If txtSelectedIndex.Text = "-1" Or txtSelectedIndex.Text = "" Then Exit Sub

        If e.ClickCount > 1 And MusicPlayer.Source IsNot Nothing Then
            Dim n As Integer
            For Each x As Music In mylistbox.Items
                n += 1
                If MusicPlayer.Source.OriginalString = x.MusicLoc Then
                    mylistbox.ScrollIntoView(mylistbox.Items(n - 1))
                    mylistbox.SelectedItem = mylistbox.Items(n - 1)
                    Exit Sub
                End If
            Next

        End If
        mylistbox.ScrollIntoView(mylistbox.SelectedItem)
    End Sub

    '右双击最右边数字重新ListBox
    Private Sub txtCount_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles txtCount.MouseDown
        mylistbox.InvalidateArrange()
    End Sub
#End Region


#Region "控制播放"

    Sub PlayMusic()
        If mylistbox.Items.Count = 0 Then
            MessageBox.Show("没有找任何歌曲呀！", "提示信息！")
            Return

        End If
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")

        Dim Title_Grid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")
        Dim temp_menuitem As System.Windows.Controls.MenuItem = Title_Grid.ContextMenu.Items(0)
        temp_menuitem.Header = "暂停"

        MusicPlayer.Play()

        If MusicPlayButton.IsChecked = True Then
            TaskBarPlayBtn.ImageSource = New BitmapImage(New Uri("pack://application:,,,/Resources/pause.ico"))
        Else
            TaskBarPlayBtn.ImageSource = New BitmapImage(New Uri("pack://application:,,,/Resources/play.ico"))
        End If
        taskbar.ProgressState = Windows.Shell.TaskbarItemProgressState.Normal
    End Sub


    Sub PlayMusic(ByVal path As String)
        If mylistbox.Items.Count = 0 Then
            MessageBox.Show("没有找任何歌曲呀！", "提示信息！")
            Return

        End If
        Dim Title_Grid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")
        Dim temp_menuitem As System.Windows.Controls.MenuItem = Title_Grid.ContextMenu.Items(0)
        temp_menuitem.Header = "暂停"

        MusicPlayer.Source = Nothing

        MusicPlayer.Source = New Uri(path, UriKind.RelativeOrAbsolute)



        MusicPlayer.Position = New TimeSpan(0)
        sliderProgress.Value = 0
        MusicPlayer.Play()

        在列表框中选中要播放的歌曲()



    End Sub


    Sub PauseMusic()
        Dim Title_Grid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")
        Dim temp_menuitem As System.Windows.Controls.MenuItem = Title_Grid.ContextMenu.Items(0)
        temp_menuitem.Header = "播放"

        MusicPlayer.Pause()
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")

        MusicPlayButton.ToolTip = "播放"
        taskbar.ProgressState = Windows.Shell.TaskbarItemProgressState.Paused

        TaskBarPlayBtn.ImageSource = New BitmapImage(New Uri("pack://application:,,,/Resources/play.ico"))


    End Sub



    '播放器打开音乐触发事件
    Private Sub MusicPlayer_MediaOpened(sender As Object, e As RoutedEventArgs) Handles MusicPlayer.MediaOpened

        If MusicPlayer.Source.OriginalString.Contains(My.Settings.MusicLoc) Then
            MusicPlayer.Position = My.Settings.MusicPos
        End If

        在列表框中选中要播放的歌曲()
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")

        Dim Title_Grid As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")
        Dim temp_menuitem As System.Windows.Controls.MenuItem = Title_Grid.ContextMenu.Items(0)
        temp_menuitem.Header = "暂停"

        MusicPlayButton.ToolTip = "暂停"
        MusicPlayButton.IsChecked = True

        taskbar.ProgressState = Windows.Shell.TaskbarItemProgressState.Normal
        TaskBarPlayBtn.ImageSource = New BitmapImage(New Uri("pack://application:,,,/Resources/pause.ico"))
        Dim locfromwin As Point = MusicImage.TranslatePoint(New Point(0, 0), Me)
        taskbar.ThumbnailClipMargin = New Thickness(locfromwin.X + 1, locfromwin.Y, ActualWidth - MusicImage.ActualWidth - locfromwin.X, ActualHeight - MusicImage.ActualHeight - locfromwin.Y + 1)
        MusicImage.Source = GetMusicImage(MusicPlayer.Source.OriginalString)


        Try

            Dim title_ContextMenu As Grid = LogicalTreeHelper.FindLogicalNode(mywin, "MusicIcon_Grid")

            Dim showlrc_menuitem As System.Windows.Controls.MenuItem = title_ContextMenu.ContextMenu.Items(5)

            If showlrc_menuitem.IsChecked Then

                Dim flag As Boolean = LoadLRC()

                m_LrcPanel.m_ItemControl.Visibility = Windows.Visibility.Visible
                MusicImage.Visibility = Windows.Visibility.Collapsed
                InfoGrid.Visibility = Windows.Visibility.Collapsed
            Else

                m_LrcPanel.m_ItemControl.Visibility = Windows.Visibility.Hidden
                MusicImage.Visibility = Windows.Visibility.Visible
                InfoGrid.Visibility = Windows.Visibility.Visible

            End If
        Catch ex As Exception
            m_LrcPanel.m_ItemControl.Visibility = Windows.Visibility.Hidden
            MusicImage.Visibility = Windows.Visibility.Visible
            InfoGrid.Visibility = Windows.Visibility.Visible

            m_LrcPanel.timer.Stop()
            If m_LrcPanel.list_height IsNot Nothing Then m_LrcPanel.list_height.Clear()
            If m_LrcPanel.m_list_lrc IsNot Nothing Then m_LrcPanel.m_list_lrc.Clear()
            m_Lrc_List.Clear()
        End Try





    End Sub

    '播放器 播放结束 
    Private Sub MusicPlayer_MediaEnded(sender As Object, e As EventArgs)

        LrcTextBlock.Text = ""

        If Isrndplay = True Then 随机播放音乐() : 在列表框中选中要播放的歌曲() : Exit Sub

        For index = 0 To AllMusicList.Count - 1
            If AllMusicList(index).MusicLoc = MusicPlayer.Source.OriginalString Then
                If index >= mylistbox.Items.Count - 1 Then PlayMusic(AllMusicList(0).MusicLoc) : Exit Sub
                PlayMusic(AllMusicList(index + 1).MusicLoc)
                Exit Sub
            End If
        Next

    End Sub


    Sub 在列表框中选中要播放的歌曲()
        If MusicPlayer.Source Is Nothing Then Return

        For Each x In AllMusicList
            If x.MusicLoc Like MusicPlayer.Source.OriginalString Then
                mylistbox.SelectedItem = x
                mylistbox.ScrollIntoView(mylistbox.SelectedItem)
                playingitem.Text = mylistbox.SelectedIndex + 1
                playingitem.ToolTip = "正在播放第 " + (mylistbox.SelectedIndex + 1).ToString + " 首歌：" + vbCrLf + x.MusicNameAndAuthors
                textTitle.Text = x.MusicTitle
                textSonger.Text = x.MusicAuthors
                Title = x.MusicNameAndAuthors

                Exit For
            End If
        Next

    End Sub


    Sub 随机播放音乐()
        '先把正在播放的歌曲从随机列表中删除
        Try
            rndmusiclist.Remove(MusicPlayer.Source.OriginalString)
        Catch ex As Exception

        End Try


        Dim musiccount As Integer = rndmusiclist.Count
        Dim x As Integer
        Dim i As Integer = 0
        If musiccount > 1 Then
            Dim rnd As New Random()
            x = rnd.Next(1, musiccount) - 1
            MusicImage.Source = GetMusicImage(rndmusiclist(x))
            If MusicImage Is Nothing Then
                MusicImage.Source = New BitmapImage(New Uri("Default_MusciImage.png", UriKind.RelativeOrAbsolute))
            End If
            MusicPlayer.Source = New Uri(rndmusiclist(x), UriKind.Absolute)
            MusicPlayer.Play()
        Else
            MusicImage.Source = GetMusicImage(rndmusiclist(0))
            If MusicImage Is Nothing Then
                MusicImage.Source = New BitmapImage(New Uri("Default_MusciImage.png", UriKind.RelativeOrAbsolute))
            End If
            MusicPlayer.Source = New Uri(rndmusiclist(0), UriKind.Absolute)
            MusicPlayer.Play()
            rndmusiclist.Clear()

            '将 所有歌曲列表 加载到 rndmusiclist
            For Each m As Music In AllMusicList
                rndmusiclist.Add(m.MusicLoc)
                If m.MusicLoc = MusicPlayer.Source.OriginalString Then
                    mylistbox.SelectedIndex = i
                End If
            Next

            rndmusiclist.Remove(MusicPlayer.Source.OriginalString)

            Exit Sub
        End If

        For Each m As Music In mylistbox.Items

            If m.MusicLoc = MusicPlayer.Source.OriginalString Then
                mylistbox.SelectedIndex = i : mylistbox.ScrollIntoView(mylistbox.SelectedItem) : Exit For
            End If
            i += 1
        Next
        '设置标题 歌曲
        For Each temp_music As Music In AllMusicList
            If MusicPlayer.Source.OriginalString = temp_music.MusicLoc Then
                Me.Title = temp_music.MusicNameAndAuthors
                textTitle.Text = temp_music.MusicTitle
                textSonger.Text = temp_music.MusicAuthors
                Exit For
            End If
        Next
        rndmusiclist.Remove(rndmusiclist(x))
    End Sub


    '播放音乐：左上角播放按钮；  标题栏 右击菜单 播放 
    Private Sub MusicPlayButton_Click(sender As Object, e As EventArgs)
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")

        If sender.ToString.Contains("Thum") Then
            MusicPlayButton.IsChecked = Not MusicPlayButton.IsChecked
        End If

        If AllMusicList.Count = 0 Then
            MessageBox.Show("没有找任何歌曲呀！", "提示信息！", MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        If MusicPlayer.Source Is Nothing Then

            If MusicPlayButton.IsChecked Then
                PlayMusic(AllMusicList(0).MusicLoc)
            Else
                PauseMusic()
            End If
        Else
            If MusicPlayButton.IsChecked Then
                PlayMusic()
            Else
                PauseMusic()
            End If

        End If

    End Sub


    '标题菜单栏的右键菜单 播放
    Private Sub MusicContextMenu_Play_Click(sender As Object, e As RoutedEventArgs)
        Dim MusicPlayButton As ToggleButton = LogicalTreeHelper.FindLogicalNode(mywin, "MusicPlayButton")
        MusicPlayButton.IsChecked = Not MusicPlayButton.IsChecked

        MusicPlayButton_Click(sender, Nothing)
    End Sub



    '左双击列表框项目 播放音乐
    Private Sub ListBoxItem_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If e.ClickCount >= 2 Then
            Dim mygrid As Grid = sender

            Dim lbi As ListBoxItem = mygrid.TemplatedParent
            Dim tempmusic As Music = CType(lbi.Content, Music)

            PlayMusic(tempmusic.MusicLoc)

            MusicPlayer.Position = New TimeSpan
            sliderProgress.Value = 0
        End If
    End Sub

    '标题栏 下一首 按钮
    Private Sub MusicnextButton_Click(sender As Object, e As EventArgs)
        If MusicImage.Source Is Nothing Then Exit Sub
        For index = 0 To AllMusicList.Count - 1
            If AllMusicList(index).MusicLoc = MusicPlayer.Source.OriginalString Then
                If index = AllMusicList.Count - 1 Then PlayMusic(AllMusicList.First.MusicLoc) : Exit Sub
                'PlayMusic(AllMusicList(index + 1).MusicLoc)
                MusicPlayer_MediaEnded(sender, e)
                Exit Sub
            End If
        Next
    End Sub

    '任务栏中 停止   按钮控制事件
    Private Sub TaskBarStopBtn_Click(sender As Object, e As EventArgs)
        MusicPlayer.Stop()
        PauseMusic()
    End Sub

    '任务栏中  上一首 
    Private Sub TaskBarPreviousBtn_Click(sender As Object, e As EventArgs)
        If MusicImage.Source Is Nothing Then Exit Sub
        For index = 0 To AllMusicList.Count - 1
            If AllMusicList(index).MusicLoc = MusicPlayer.Source.OriginalString Then
                If index = 0 Then PlayMusic(AllMusicList.Last.MusicLoc) : Exit Sub
                PlayMusic(AllMusicList(index - 1).MusicLoc)
                Exit Sub
            End If
        Next
    End Sub

#End Region


#Region "搜索音乐相关"

    '搜索文本框输入时触发
    Private Sub searchtextbox_TextChanged(sender As Object, e As TextChangedEventArgs) Handles SearchTextBox.TextChanged
        If SearchTextBox.Text = "" Then

            SetMainListAllMusic()

            Exit Sub
        End If

        Dim musicloc As String
        If MusicPlayer.Source Is Nothing Then
            musicloc = ""
        Else
            musicloc = MusicPlayer.Source.OriginalString
        End If


        Dim y As String = "*" & SearchTextBox.Text & "*"


        SearchMusicList.Clear()
        playingitem.Text = 0


        For Each x As Music In AllMusicList

            If LCase(x.MusicTitle) Like LCase(y) Or LCase(x.MusicAuthors) Like LCase(y) Then
                SearchMusicList.Add(x)
                If musicloc = x.MusicLoc Then


                    Title = x.MusicTitle
                    playingitem.ToolTip = "正在播放第 " + (SearchMusicList.Count).ToString + " 首歌：" + vbCrLf + x.MusicNameAndAuthors

                    playingitem.Text = SearchMusicList.Count
                End If
            End If
        Next
        mylistbox.ItemsSource = SearchMusicList
        mylistbox.DisplayMemberPath = "MusicTitle"

    End Sub

    '清除搜索结果
    Private Sub searchtextboxclosebutton_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)

        RemoveHandler SearchTextBox.TextChanged, AddressOf searchtextbox_TextChanged
        SearchTextBox.Text = ""

        AddHandler SearchTextBox.TextChanged, AddressOf searchtextbox_TextChanged

        SetMainListAllMusic()

    End Sub

    ''' <summary>
    ''' 将主列表框的DataContext设为所有歌曲
    ''' </summary>
    Sub SetMainListAllMusic()
        mylistbox.ItemsSource = AllMusicList
        mylistbox.DisplayMemberPath = "MusicNameAndAuthors"

        Dim i As Integer
        If MusicPlayer.Source IsNot Nothing Then
            For Each x As Music In AllMusicList
                i += 1
                If MusicPlayer.Source.OriginalString = x.MusicLoc Then

                    Title = x.MusicNameAndAuthors
                    playingitem.ToolTip = "正在播放第 " + i.ToString + " 首歌：" + vbCrLf + x.MusicNameAndAuthors

                    playingitem.Text = i

                    mylistbox.SelectedIndex = i - 1
                    mylistbox.ScrollIntoView(mylistbox.Items(i - 1))
                    Exit For
                End If
            Next

        End If
    End Sub

    '查找歌手的所有歌曲
    Private Sub HyperlinkSongerSearch_Click(sender As Object, e As RoutedEventArgs)
        Dim h As Hyperlink = sender
        Dim r As Run = h.Inlines.FirstInline
        SearchTextBox.Focus()
        SearchTextBox.Text = r.Text
    End Sub

#End Region

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles MyBase.Closing
        Thread_RefreshProgress.Abort()

        Dim volslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "volslider")
        Dim Backgroun_Image As Image = LogicalTreeHelper.FindLogicalNode(mywin, "m_BackgroundImage")

        My.Settings.mycolor = System.Drawing.Color.FromArgb(sliderA.Value, sliderR.Value, sliderG.Value, sliderB.Value)
        My.Settings.Vol = volslider.Value
        My.Settings.Blur = Backgeffect.Value
        If MusicPlayer.Source IsNot Nothing Then
            My.Settings.textMusicPos = textMusicPos.Text
            My.Settings.MusicLoc = MusicPlayer.Source.OriginalString
            My.Settings.MusicPos = MusicPlayer.Position
            My.Settings.MusicPercent = sliderProgress.Value / sliderProgress.Maximum
            If Double.IsNaN(My.Settings.MusicPercent) Then
                My.Settings.MusicPercent = 0
            End If
            My.Settings.Opacity = Backgopacity.Value
            My.Settings.MusicTitle = Title
            If MusicPlayer.NaturalDuration.HasTimeSpan Then
                My.Settings.MucisTotal = MusicPlayer.NaturalDuration.TimeSpan.TotalSeconds
            End If
        End If

        My.Settings.Save()
    End Sub


#Region "标题栏右键菜单 "

    '标题栏右键菜单 随机 播放歌曲 
    Private Sub RndPlayMusic_Click(sender As Object, e As RoutedEventArgs)
        Dim rndplay As System.Windows.Controls.MenuItem = sender
        If rndplay.IsChecked = False Then
            Isrndplay = True
            rndmusiclist = New ArrayList

            '将 所有歌曲列表 加载到 rndmusiclist
            For Each x As Music In AllMusicList
                rndmusiclist.Add(x.MusicLoc)
            Next

            If MusicPlayer.Source IsNot Nothing Then
                rndmusiclist.Remove(MusicPlayer.Source.OriginalString)
            End If
        Else
            Isrndplay = False

        End If
        rndplay.IsChecked = Not rndplay.IsChecked

    End Sub



    Private Sub 显示歌词_Click(sender As Object, e As RoutedEventArgs)
        Dim lrcshow As System.Windows.Controls.MenuItem = sender
        My.Settings.ShowLRC = lrcshow.IsChecked
        My.Settings.Save()
        If lrcshow.IsChecked = False Then
            m_LrcPanel.Visibility = Windows.Visibility.Collapsed
            MusicImage.Visibility = Windows.Visibility.Visible
            InfoGrid.Visibility = Windows.Visibility.Visible
        Else
            Dim flag As Boolean = LoadLRC()

            If flag Then
                m_LrcPanel.LrcPanelTooltip.Opacity = 0
            Else
                m_LrcPanel.LrcPanelTooltip.Opacity = 1
            End If

            m_LrcPanel.Visibility = Windows.Visibility.Visible
            MusicImage.Visibility = Windows.Visibility.Collapsed
            InfoGrid.Visibility = Windows.Visibility.Collapsed
            m_LrcPanel.InvalidateArrange()
        End If

    End Sub

#End Region


#Region "不会更改的代码"

#Region "窗体右上角按钮"
    Private Sub MinimizeWindowCommand_Executed(sender As Object, e As EventArgs)
        SystemCommands.MinimizeWindow(Me)
    End Sub

    Private Sub MaximizeWindowCommand_Executed(sender As Object, e As EventArgs)

        If Me.WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)
        ElseIf Me.WindowState = WindowState.Normal Then
            SystemCommands.MaximizeWindow(Me)
        End If

    End Sub

    Private Sub CloseWindowCommand_Executed(sender As Object, e As EventArgs)
        Close()
    End Sub

    Private Sub TitleGrid_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If e.ClickCount > 1 Then
            MaximizeWindowCommand_Executed(sender, e)
        End If
    End Sub

#End Region


#Region "列表项目 右键菜单"

    '主列表框 播放选中项目
    Private Sub MenuItemPlay_Click(sender As Object, e As RoutedEventArgs)
        Try
            PlayMusic(CType(mylistbox.SelectedItem, Music).MusicLoc)
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - MenuItemPlay_Click", 64, "出错了！")
        End Try
    End Sub


    '搜索歌词
    Private Sub MenuItemFindLrc(sender As Object, e As RoutedEventArgs)

        Dim music As Music = CType(mylistbox.SelectedItem, Music)

        Dim searchForm = New SearchLRC(music, Me)

        searchForm.textboxTitle.Text = music.MusicTitle

        searchForm.textboxArtist.Text = music.MusicAuthors

        searchForm.btnSerach_Click(Nothing, Nothing)

        searchForm.ShowDialog()

    End Sub

    '打开歌曲文件位置
    Private Sub MenuItemMusicLocation_Click(sender As Object, e As EventArgs)

        Try
            If sender.ToString.Contains("MenuItem") Then
                Process.Start("explorer.exe", "/select," & CType(mylistbox.SelectedItem, Music).MusicLoc)
            ElseIf sender.ToString.Contains("Hyperlink") Then
                If File.Exists(Info_SongLoc.Text) = True Then
                    Process.Start("explorer.exe", "/select," & Info_SongLoc.Text)
                End If
            End If

        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - MenuItemMusicLocation_Click", 64, "出错了！")
        End Try

    End Sub
    '打开歌曲文件属性对话框
    Private Sub MenuItemProperty_Click(sender As Object, e As RoutedEventArgs)
        Try
            SHFileCtrl.ShowPropertie(Process.GetCurrentProcess.Handle, CType(mylistbox.SelectedItem, Music).MusicLoc)
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - MenuItemProperty_Click", 64, "出错了！")
        End Try

    End Sub

    '按歌曲标题排序
    Private Sub MenuItem_SortClick1(sender As Object, e As MouseButtonEventArgs)
        AllMusicList.Sort("MusicTitle", 1)

        mylistbox.ItemsSource = AllMusicList

        mylistbox.InvalidateArrange()
    End Sub


    Private Sub MenuItem_SortClick2(sender As Object, e As MouseButtonEventArgs)
        AllMusicList.Sort("MusicTitle", -1)

        mylistbox.ItemsSource = AllMusicList

        mylistbox.InvalidateArrange()
    End Sub

    '升序
    Private Sub MenuItem_SortClick3(sender As Object, e As MouseButtonEventArgs)
        AllMusicList.Sort("MusicAuthors", 1)

        mylistbox.ItemsSource = AllMusicList

        mylistbox.InvalidateArrange()
    End Sub
    '降序
    Private Sub MenuItem_SortClick4(sender As Object, e As MouseButtonEventArgs)
        AllMusicList.Sort("MusicAuthors", -1)

        mylistbox.ItemsSource = AllMusicList

        mylistbox.InvalidateArrange()
    End Sub

    '搜索列表框
    Private Sub SearchPlay_ItemClick(sender As Object, e As RoutedEventArgs)
        Dim TempSearchTextBox As DependencyObject = VisualTreeHelper.GetChild(SearchTextBox, 0)
        Dim searchlistbox As System.Windows.Controls.ListBox = LogicalTreeHelper.FindLogicalNode(TempSearchTextBox, "searchlistbox")
        Try
            PlayMusic(CType(searchlistbox.SelectedItem, Music).MusicLoc)
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - MenuItemPlay_Click", 64, "出错了！")
        End Try
    End Sub

    '打开歌曲文件位置
    Private Sub SearchLocation_ItemClick(sender As Object, e As RoutedEventArgs)
        Dim TempSearchTextBox As DependencyObject = VisualTreeHelper.GetChild(SearchTextBox, 0)
        Dim searchlistbox As System.Windows.Controls.ListBox = LogicalTreeHelper.FindLogicalNode(TempSearchTextBox, "searchlistbox")

        Try
            If sender.ToString.Contains("MenuItem") Then
                Process.Start("explorer.exe", "/select," & CType(searchlistbox.SelectedItem, Music).MusicLoc)
            End If
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - SearchItemLocation_Click", 64, "出错了！")
        End Try
    End Sub
    '打开歌曲文件属性对话框
    Private Sub SearchProperty_ItemClick(sender As Object, e As RoutedEventArgs)
        Dim TempSearchTextBox As DependencyObject = VisualTreeHelper.GetChild(SearchTextBox, 0)
        Dim searchlistbox As System.Windows.Controls.ListBox = LogicalTreeHelper.FindLogicalNode(TempSearchTextBox, "searchlistbox")
        Try
            SHFileCtrl.ShowPropertie(Process.GetCurrentProcess.Handle, CType(searchlistbox.SelectedItem, Music).MusicLoc)
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "From - SearchItemProperty_Click", 64, "出错了！")
        End Try
    End Sub



#End Region


#Region "调整音量"


#If CoreAudioApi Then

    Sub 获取系统主音量(data As AudioVolumeNotificationData)
        IsUserChangeSystemVol = False

        Me.Dispatcher.Invoke(Sub()

                                 Dim systemvolslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "systemvolslider")
                                 Dim sysvolchecked As CheckBox = LogicalTreeHelper.FindLogicalNode(mywin, "sysvolchecked")
                                 sysvolchecked.IsChecked = data.Muted
                                 systemvolslider.Value = data.MasterVolume
                             End Sub)
        IsUserChangeSystemVol = True
    End Sub

#End If


    '是否将 系统 静音
    Private Sub sysvol_Click(sender As Object, e As RoutedEventArgs)

#If CoreAudioApi Then

        Dim sysvolchecked As CheckBox = sender
        Dim mmdevice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
        If sysvolchecked.IsChecked = True Then

            mmdevice.AudioEndpointVolume.Mute = True
        Else

            mmdevice.AudioEndpointVolume.Mute = False
        End If

#End If

    End Sub



    'slider 滑动 设置系统音量
    Private Sub systemvolslider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))

#If CoreAudioApi Then

        If IsUserChangeSystemVol = True Then
            Dim mmdevice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
            mmdevice.AudioEndpointVolume.MasterVolumeLevelScalar = e.NewValue
        End If

#End If

    End Sub

    '滑轮调节 音量
    Private Sub systemvol_MosueWheel(sender As Object, e As MouseWheelEventArgs)

#If CoreAudioApi Then

        Dim mmdevice As MMDevice = sysvoldevice.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
        Dim vol As Single = mmdevice.AudioEndpointVolume.MasterVolumeLevelScalar

        vol += e.Delta / 6000
        If vol <= 0 Then vol = 0
        If vol >= 1 Then vol = 1

        mmdevice.AudioEndpointVolume.MasterVolumeLevelScalar = vol

#End If

    End Sub



    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub volbtn_MouseWheel(sender As Object, e As MouseWheelEventArgs)
        MusicPlayer.Volume += e.Delta / 6000
        If MusicPlayer.Volume <= 0 Then MusicPlayer.Volume = 0
        If MusicPlayer.Volume >= 1 Then MusicPlayer.Volume = 1
    End Sub



    '标题栏上 音量图标 的单击  
    Private Sub exevolchecked(sender As Object, e As RoutedEventArgs)

        Dim volbtn As System.Windows.Controls.CheckBox = sender
        If volbtn.Tag Is Nothing Then
            If MusicPlayer.Volume <> 0 Then
                volbtn.Tag = MusicPlayer.Volume
                MusicPlayer.Volume = 0
            End If
        Else
            If MusicPlayer.Volume = 0 Then
                MusicPlayer.Volume = Val(volbtn.Tag)
            Else
                volbtn.Tag = MusicPlayer.Volume
                MusicPlayer.Volume = 0
            End If
        End If

    End Sub

    '设置播放器音量  = 0
    Private Sub voltxt_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        Dim volslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "volslider")
        volslider.Value = 0
    End Sub





#End Region

    '列表中的Item 显示歌词相关信息的工具提示3
    Private Sub Grid_ToolTipOpening(sender As Object, e As ToolTipEventArgs)

        Dim temp_music As Music = CType(sender, Grid).DataContext

        temp_music.MusicImage = GetMusicImage(temp_music.MusicLoc)

    End Sub

    '重置播放速度
    Private Sub Speed_MouseDown(sender As Object, e As MouseButtonEventArgs)
        MusicPlayer.SpeedRatio = 1
        Dim speedslider As Slider = LogicalTreeHelper.FindLogicalNode(mywin, "speed")
        speedslider.Value = 1
    End Sub

    '重置播放平衡
    Private Sub Balance_MouseDown(sender As Object, e As MouseButtonEventArgs)
        MusicPlayer.Balance = 0
    End Sub

    '拖动进度条
    Private Sub sliderProgress_MouseMove(sender As Object, e As System.Windows.Input.MouseEventArgs) Handles sliderProgress.MouseMove
        If e.LeftButton = MouseButtonState.Pressed Then
            IsMouseDownOnSlider = True
        Else
            IsMouseDownOnSlider = False
        End If
        If MusicPlayer.NaturalDuration.HasTimeSpan Then

            textblock_SkipTime.SetValue(Canvas.LeftProperty, e.GetPosition(sliderProgress).X)

            Dim temp_timespan As New TimeSpan(0, 0, Math.Round(MusicPlayer.NaturalDuration.TimeSpan.TotalSeconds * (e.GetPosition(sliderProgress).X / sliderProgress.ActualWidth)))

            textblock_SkipTime.Text = temp_timespan.Minutes.ToString() + ":" + temp_timespan.Seconds.ToString("00.#")

        End If
    End Sub

    '调节播放进度
    Private Sub sliderProgress_PreviewMouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles sliderProgress.PreviewMouseLeftButtonUp
        If MusicPlayer.Source IsNot Nothing And MusicPlayer.NaturalDuration.HasTimeSpan Then
            MusicPlayer.Position = New TimeSpan(0, 0, Mouse.GetPosition(sliderProgress).X / sliderProgress.ActualWidth * sliderProgress.Maximum)
            sliderProgress.Value = MusicPlayer.Position.TotalSeconds
        End If
    End Sub

    '拖动标题栏，拖动窗体
    Private Sub Title_Move(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            Try

                Me.DragMove()

            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub

    '拖动右下角，拖动窗体
    Private Sub resizeGrip_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles resizeGrip.MouseLeftButtonDown
        If e.ChangedButton = MouseButton.Left Then
            Try
                Me.DragMove()
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub

    '重新载入音乐文件
    Private Sub MenuItemReloadList_Click(sender As Object, e As RoutedEventArgs)

        Thread_LoadMusicInfo = New Thread(New ThreadStart(AddressOf LoadMp3Info)) With {.IsBackground = True}
        Thread_LoadMusicInfo.SetApartmentState(ApartmentState.STA)
        Thread_LoadMusicInfo.Start()

    End Sub


    Private Sub sliderProgress_MouseEnter(sender As Object, e As System.Windows.Input.MouseEventArgs)
        If MusicPlayer.NaturalDuration.HasTimeSpan Then
            canvas_SkipTime.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub sliderProgress_MouseLeave(sender As Object, e As System.Windows.Input.MouseEventArgs)
        canvas_SkipTime.Visibility = Visibility.Collapsed
    End Sub


    ''' <summary>
    ''' 更换音乐路径
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    ''' 

    Private Sub MusicPathLabel_MouseDown(sender As Object, e As MouseButtonEventArgs)

        Dim FBDialog As New Forms.FolderBrowserDialog()
        FBDialog.ShowNewFolderButton = False
        If FBDialog.ShowDialog = Forms.DialogResult.OK Then

            BottomPlayBtn.IsChecked = False

            MusicPlayButton_Click(sender, Nothing)

            IsFirstRun = False

            My.Settings.MusicPaths = FBDialog.SelectedPath
            My.Settings.Save()

            LoadForm = New LoadForm()

            LoadForm.Show()

            Hide()

            Task.Factory.StartNew(AddressOf LoadMp3Info)


        End If



    End Sub


    ''' <summary>
    ''' 刷新圆形播放进度条
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub sliderProgress_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        Dim temp = New DoubleCollection()
        temp.Add((circleprogress.ActualHeight - circleprogress.StrokeThickness) * Math.PI * (sliderProgress.Value / sliderProgress.Maximum) / 3)
        temp.Add(1000)
        circleprogress.StrokeDashArray = temp
    End Sub


#If CoreAudioApi = False Then

    Sub VolTimer_Tick(sender As Object, e As EventArgs)
        ''''''''''''''''''''音量相关''''''''''''''''''''

        Dim volProgress As ProgressBar = LogicalTreeHelper.FindLogicalNode(mywin, "volProgress")

        Dim device As New MMDeviceEnumerator
        Dim musicdevice As MMDevice = device.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
        Dim volcount As Integer = musicdevice.AudioSessionManager.Sessions.Count

        For n = 0 To volcount - 1
            If musicdevice.AudioSessionManager.Sessions.Item(n).ProcessID = Process.GetCurrentProcess.Id Then
                If musicdevice.AudioSessionManager.Sessions.Item(n).AudioMeterInformation.MasterPeakValue <> 0 Then
                    volProgress.Value = musicdevice.AudioSessionManager.Sessions.Item(n).AudioMeterInformation.MasterPeakValue

                End If

            End If
        Next
    End Sub

#End If
#End Region


#Region "从网络中获取歌词"


    Sub GetLRCfromNET(ByRef music As Music, Optional IsDeleteSourceFile As Boolean = True)
        Dim mp3_lrc_path As String = Path.GetDirectoryName(music.MusicLoc) + "\" + Path.GetFileNameWithoutExtension(music.MusicLoc) + ".lrc"

        If Not IsDeleteSourceFile AndAlso File.Exists(mp3_lrc_path) Then
            Return
        End If

        File.Delete(mp3_lrc_path)

        Dim list_lrc As New List(Of String)

        Dim http As New WebClient()

        http.Encoding = System.Text.Encoding.GetEncoding("UTF-8")


        Dim songer As String = ""
        If music.MusicAuthors <> "未知歌手" Then
            songer = music.MusicAuthors.Replace(" ", "+")
        End If

        Dim findobject = music.MusicTitle.Trim().Replace(" ", "+")

        If Not HaveChinese(findobject) Then

            Dim lrcpath As String = "http://syair.info/search/?artist=" + songer + "&title=" + findobject + "&format=lrc"

            Dim result As String = http.DownloadStringTaskAsync(lrcpath).Result

            Dim lrcIP = Regex.Matches(result, "[A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][.]html")

            If lrcIP.Count >= 1 Then

                Dim http_temp As New WebClient()

                http_temp.Encoding = System.Text.Encoding.GetEncoding("UTF-8")

                Dim lrchtml = "http://syair.info/lyrics/" + lrcIP(0).ToString()

                Dim result1 As String = http_temp.DownloadStringTaskAsync(New Uri(lrchtml)).Result

                Dim lrcTexts = Regex.Matches(result1, "\[(.+)<br />")

                Dim sw As New StreamWriter(mp3_lrc_path)

                For Each temp In lrcTexts
                    sw.WriteLine(temp.ToString().Replace("<br />", ""))
                Next

                sw.Flush()

                sw.Close()

            End If

        Else

            Dim lrcpath As String = "http://www.cnlyric.com/search.php?k=" + chineseToHex(findobject) + "&t=s"
            Dim http_temp = http.DownloadString(lrcpath)
            Dim lrccount = Regex.Matches(http_temp, ">\d{1,}\.<")
        End If


    End Sub


    Function GetLrcListFormNet(music As Music) As List(Of LrcUrlInfo)

        Dim title = music.MusicTitle
        Dim artist = music.MusicAuthors
        If artist = "未知歌手" Then artist = ""

        '中文歌曲
        If HaveChinese(title) Then

            Dim http As New WebClient()

            Dim lrc_uri As String = "http://www.cnlyric.com/search.php?k=" + chineseToHex(title) + "&t=s"

            Dim http_downstr = http.DownloadString(lrc_uri)

            Dim lrcpart = Regex.Matches(http_downstr, ">\d{1,}\.<")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)


            Dim lrcpath = Regex.Matches(http_downstr, "(LrcDown)/\d{1,}/\d{1,}(\.lrc)")   '下载链接

            For index = 1 To lrcpart.Count

                Dim s1 As String = lrcpath.Item(index - 1).ToString()

                temp_list_lrcurls.Add(New LrcUrlInfo With {.url = "http://www.cnlyric.com/" + s1})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                list_lrcurls.Add(New LrcUrlInfo With {.content = http.DownloadString(item.url), .url = item.url})

            Next

            Return list_lrcurls

            '英文歌曲
        Else
            Dim http As New WebClient()

            title = title.Replace(" ", "+")

            artist = artist.Replace(" ", "+")

            Dim lrcUri As String = "http://syair.info/search/?artist=" + artist + "&title=" + title + "&format=lrc"

            Dim result As String = http.DownloadString(lrcUri)



            Dim lrcIP = Regex.Matches(result, "[A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][.]html")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In lrcIP

                temp_list_lrcurls.Add(New LrcUrlInfo() With {.url = "http://syair.info/lyrics/" + item.ToString()})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                Dim t_content = http.DownloadString(item.url)

                Dim lrcTexts = Regex.Matches(t_content, "\[(.+)<br />")

                Dim content As String = ""

                For Each temp In lrcTexts

                    content += temp.ToString().Replace("<br />", "")

                Next

                list_lrcurls.Add(New LrcUrlInfo() With {.url = item.url, .content = content})

            Next

            Return list_lrcurls
        End If

        Return Nothing

    End Function


    Public Shared Function chineseToHex(ByVal chinese As String) As String
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


#End Region



#Region "歌词处理"

    Function LoadLRC() As Boolean

        '加载歌词
        SyncLock m_Lrc_List

            'LrcList.Clear()
            'LrcTextBlock.Text = ""

            m_Lrc_List.Clear()

            Dim mp3_lrc_path As String = Path.GetDirectoryName(MusicPlayer.Source.OriginalString) + "\" + Path.GetFileNameWithoutExtension(MusicPlayer.Source.OriginalString) + ".lrc"

            Dim temp_lrcline As New List(Of String)

            If File.Exists(mp3_lrc_path) = True Then

                Dim lrcfile As New StreamReader(mp3_lrc_path)

                While Not lrcfile.EndOfStream

                    temp_lrcline.Add(lrcfile.ReadLine())

                End While

                For Each line In temp_lrcline

                    If line.Contains("]") Then

                        Dim split = line.Split("]")

                        For index = 0 To split.Length - 2
                            If split(index).Substring(1) <> "" AndAlso split(split.Length - 1).Trim() <> "" Then
                                m_Lrc_List.Add(New LrcData(split(index).Substring(1), split(split.Length - 1), Me))
                            End If
                        Next

                    End If

                Next

                m_Lrc_List.Sort()

                m_LrcPanel.SetSource(m_Lrc_List, Nothing)

                m_LrcPanel.StartTimer()

                m_LrcPanel.LrcPanelTooltip.Opacity = 0

                Return True
            Else
                m_LrcPanel.LrcPanelTooltip.Opacity = 1

                m_LrcPanel.timer.Stop()

                Return False
            End If
        End SyncLock



    End Function



#End Region



    '改变窗体大小时，刷新歌词滑动位置
    Private Sub m_LrcPanel_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles m_LrcPanel.SizeChanged

        If m_LrcPanel.Visibility = Windows.Visibility.Visible AndAlso MusicPlayer.HasAudio AndAlso m_LrcPanel.m_list_lrc IsNot Nothing Then

            Dim lrcborder As Border = VisualTreeHelper.GetChild(m_LrcPanel.m_ItemControl, 0)

            Dim lrcpre As ItemsPresenter = lrcborder.Child

            Dim lrcstackpanel As StackPanel = VisualTreeHelper.GetChild(lrcpre, 0)
            m_LrcPanel.list_height.Clear()
            For index = 0 To m_LrcPanel.m_list_lrc.Count - 1
                m_LrcPanel.list_height.Add(m_LrcPanel.GetHeight(lrcstackpanel, index))
            Next

        End If
    End Sub


End Class

Public Structure LrcUrlInfo

    Dim content As String

    Dim url As String

End Structure


Public Class LrcData_List
    Inherits ObservableCollection(Of LrcData)

    Dim sort_list As List(Of LrcData)

    Sub Sort()

        sort_list = New List(Of LrcData)

        For index = 0 To Count - 1
            sort_list.Add(Item(index))
        Next

        sort_list.Sort()

        Me.Clear()

        For index = 0 To sort_list.Count - 1
            Add(sort_list.Item(index))
            sort_list.Item(index).index = index
        Next

    End Sub


End Class


Public Class LrcData
    Implements IComparable
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public mainwin As MainWindow



    Public Overloads Function CompareTo(ByVal obj As Object) As Integer Implements IComparable.CompareTo

        If obj Is Nothing Then Return 1

        Dim otherLrc As LrcData = TryCast(obj, LrcData)
        If otherLrc IsNot Nothing Then
            If Me.time < otherLrc.time Then
                Return -1
            End If
            If Me.time = otherLrc.time Then
                Return 0
            End If
            If Me.time > otherLrc.time Then
                Return 1
            End If
        Else
            Throw New ArgumentException("Object is not a Temperature")
        End If
        Return 1
    End Function


    Sub New(_time As String, _text_ As String, _mainwin As MainWindow)
        mainwin = _mainwin
        text = _text_
        s_time = _time
        time = CvtLrcTime(Me)

    End Sub

    Private s_time As String

    Public _text As String

    Public time As Double

    Public index As Integer

    Public translatey As Double

    Public ToTopHeight As Double


    Property text As String
        Set(value As String)
            If value Is Nothing Then
                Throw New Exception("value is nothing")
            End If
            _text = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("text"))
        End Set
        Get
            Return _text
        End Get
    End Property

    Function CvtLrcTime(data As LrcData) As Double
        '01:05.44
        Try

            Dim str As String = data.s_time

            Dim m = str.Split(":")(0)
            Dim s = str.Split(":")(1)

            Dim r As Double = 0

            If m.Last() <> "0" Then
                r += Integer.Parse(m.Last()) * 60
            End If

            If s.First() = "0" Then
                r += Double.Parse(s.Substring(1))
            Else
                r += Double.Parse(s)
            End If

            Return r * 1000

        Catch ex As Exception

            mainwin.m_Lrc_List.Remove(Me)

        End Try
        Return 0
    End Function

End Class
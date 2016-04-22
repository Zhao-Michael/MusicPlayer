Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports System.ComponentModel

Public Class LrcPanel
    Implements INotifyPropertyChanged


    Public LineHeiget As Integer = 20

    Public m_list_lrc As LrcData_List

    Public m_Music As Music

    Public mainwin As MainWindow

    Public timer As New DispatcherTimer(DispatcherPriority.ApplicationIdle)

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public list_height As New List(Of Double)

    Sub StartTimer()

        m_ItemControl.Margin = New Thickness(0, 0, 0, 0)

        timer.Interval = New TimeSpan(0, 0, 1)


        Dim lrcborder As Border = VisualTreeHelper.GetChild(m_ItemControl, 0)

        Dim lrcpre As ItemsPresenter = lrcborder.Child

        Dim lrcstackpanel As StackPanel = VisualTreeHelper.GetChild(lrcpre, 0)


        AddHandler timer.Tick, Sub()

                                   If mainwin IsNot Nothing Then



                                       If mainwin.MusicPlayer IsNot Nothing Then

                                           If mainwin.MusicPlayer.HasAudio Then


                                               If list_height.Count = 0 Then
                                                   '一首歌 只执行一次
                                                   UpdatePosition(-Me.ActualHeight / 2 + 10)


                                                   Dim sb1 = TryCast(Me.FindResource("foregroundani1"), Storyboard)
                                                   Dim content1 As ContentPresenter = lrcstackpanel.Children(0)
                                                   Storyboard.SetTarget(sb1, TryCast(content1.ContentTemplate.FindName("lrctext", content1), TextBlock))
                                                   sb1.Begin()

                                                   Dim sw111 As New Stopwatch


                                                   For index = 0 To m_list_lrc.Count - 1
                                                       list_height.Add(GetHeight(lrcstackpanel, index))
                                                   Next


                                               End If


                                               For Each templrc In m_list_lrc

                                                   If templrc.time >= mainwin.MusicPlayer.Position.TotalMilliseconds Then

                                                       If templrc.index >= 2 Then

                                                           UpdatePosition(list_height(templrc.index - 1))

                                                           UpdateStyle(lrcstackpanel, templrc.index)

                                                       End If

                                                       Return

                                                   End If

                                               Next

                                           End If

                                       End If

                                   End If
                               End Sub

        UpdatePosition(-ActualHeight / 2)

        timer.Start()
    End Sub




    Public Property currentPos As Integer
        Get
            Return GetValue(currentPosProperty)
        End Get

        Set(ByVal value As Integer)
            SetValue(currentPosProperty, value)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("currentPos"))
        End Set
    End Property

    Public Shared ReadOnly currentPosProperty As DependencyProperty = _
                           DependencyProperty.Register("currentPos", _
                           GetType(Integer), GetType(LrcPanel), _
                           New PropertyMetadata(0))




    Function GetHeight(ByVal control As StackPanel, ByVal index As Integer) As Double

        Dim result As Double

        For i = 0 To index
            result += CType(control.Children(i), ContentPresenter).ActualHeight
        Next

        Return result - (Me.ActualHeight - 170) / 2 - 90

    End Function


    Sub SetSource(_list_lrc As LrcData_List, _music As Music)

        list_height.Clear()

        m_list_lrc = _list_lrc

        m_ItemControl.ItemsSource = m_list_lrc

        m_ItemControl.Margin = New Thickness(0, 0, 0, 0)

    End Sub

    '执行颜色更改动画
    Sub UpdateStyle(stack As StackPanel, index As Integer)
        Dim sb1 = TryCast(Me.FindResource("foregroundani1"), Storyboard)
        Dim content1 As ContentPresenter = stack.Children(index - 1)
        Storyboard.SetTarget(sb1, TryCast(content1.ContentTemplate.FindName("lrctext", content1), TextBlock))
        sb1.Begin()


        Dim sb2 = TryCast(Me.FindResource("foregroundani2"), Storyboard)
        Dim content2 As ContentPresenter = stack.Children(index - 2)
        Storyboard.SetTarget(sb2, TryCast(content2.ContentTemplate.FindName("lrctext", content2), TextBlock))
        sb2.Begin()

    End Sub

    '执行 位置更改 动画
    Sub UpdatePosition(currentLenToTop As Double)

        Dim sb_margin As New System.Windows.Media.Animation.Storyboard

        Dim m_thickness As New ThicknessAnimation(New Thickness(0, -currentLenToTop, 0, 0), New Duration(New TimeSpan(0, 0, 1)))

        Storyboard.SetTarget(m_thickness, m_ItemControl)

        Storyboard.SetTargetProperty(m_thickness, New PropertyPath(MarginProperty))

        sb_margin.Children.Add(m_thickness)

        sb_margin.Begin()

    End Sub


 
End Class

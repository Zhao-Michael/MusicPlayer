Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO

Class MyComparer
    Implements IComparer


    Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare

        Return ((New CaseInsensitiveComparer()).Compare(y, x))

    End Function


End Class


Class MusicList
    Inherits ObservableCollection(Of Music)

    Private Mainwin As Window = Application.Current.MainWindow

    Dim Player As MediaElement = Mainwin.FindName("MusicPlayer")

    Public Sub Play()
        Try
            Player.Play()
        Catch ex As Exception
            MsgBox(ex.Message, 64, "From Class MusicList.Play")
        End Try
    End Sub

    Dim sort_list As List(Of Music)

    Sub Sort(str As String, arg As Integer)

        sort_list = New List(Of Music)

        For index = 0 To Count - 1
            sort_list.Add(Item(index))
        Next

        If str = "MusicTitle" Then
            If arg > 0 Then
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicTitle_Ascending", CallType.Method, m1, m2))
            Else
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicTitle_Deascending", CallType.Method, m1, m2))
            End If
        End If

        If str = "MusicAuthors" Then
            If arg > 0 Then
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicAuthors_Ascending", CallType.Method, m1, m2))
            Else
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicAuthors_Deascending", CallType.Method, m1, m2))
            End If
        End If

        If str = "MusicLength" Then
            If arg > 0 Then
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicLength_Ascending", CallType.Method, m1, m2))
            Else
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicLength_Deascending", CallType.Method, m1, m2))
            End If
        End If

        If str = "MusicSize" Then
            If arg > 0 Then
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicSize_Ascending", CallType.Method, m1, m2))
            Else
                sort_list.Sort(Function(m1 As Music, m2 As Music) CallByName(Me, "compare_MusicSize_Deascending", CallType.Method, m1, m2))
            End If
        End If

        Me.Clear()

        For index = 0 To sort_list.Count - 1
            Add(sort_list.Item(index))
        Next

    End Sub


#Region "排序Lambda表达式用到的函数"
    Shared i As Integer = 0
    Public Function compare_MusicTitle_Ascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        'Return ((New CaseInsensitiveComparer()).Compare(m1.MusicTitle, m2.MusicTitle))
        Return m1.MusicTitle.ToLower() < m2.MusicTitle.ToLower()
    End Function

    Public Function compare_MusicTitle_Deascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicTitle.ToLower() > m2.MusicTitle.ToLower()
    End Function

    Public Function compare_MusicAuthors_Ascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicAuthors.ToLower() < m2.MusicAuthors.ToLower()
    End Function

    Public Function compare_MusicAuthors_Deascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicAuthors.ToLower() > m2.MusicAuthors.ToLower()
    End Function

    Public Function compare_MusicLength_Ascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicLength.ToLower() < m2.MusicLength.ToLower()
    End Function

    Public Function compare_MusicLength_Deascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicLength.ToLower() > m2.MusicLength.ToLower()
    End Function

    Public Function compare_MusicSize_Ascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicSize.ToLower() < m2.MusicSize.ToLower()
    End Function

    Public Function compare_MusicSize_Deascending(m1 As Music, m2 As Music)
        If m1 Is Nothing OrElse m2 Is Nothing Then
            Return 0
        End If
        If m1.Equals(m2) Then
            Return 0
        End If
        Return m1.MusicSize.ToLower() > m2.MusicSize.ToLower()
    End Function

#End Region


End Class


Public Class Music

    Implements INotifyPropertyChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged


    Private _MusicImage As BitmapImage    '音乐专辑图片
    Property MusicImage As BitmapImage
        Set(value As BitmapImage)
            _MusicImage = value
            If _MusicImage Is Nothing Then
                _MusicImage = New BitmapImage(New Uri("Default_MusciImage.png", UriKind.RelativeOrAbsolute))
            End If
        End Set
        Get
            Return _MusicImage
        End Get
    End Property


    Private _MusicTitle As String   '音乐名称
    Property MusicTitle As String
        Set(value As String)
            If value Is Nothing Then
                Throw New Exception("value is nothing")
            End If
            _MusicTitle = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicTitle"))
        End Set
        Get
            Return _MusicTitle
        End Get
    End Property


    Private _MusicLoc As String    '音乐位置
    Property MusicLoc As String
        Set(value As String)
            _MusicLoc = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicLoc"))
        End Set
        Get
            Return _MusicLoc
        End Get
    End Property


    Private _MusicAuthors As String '音乐歌手
    Property MusicAuthors As String
        Set(value As String)
            _MusicAuthors = value
            If _MusicAuthors = "" Then _MusicAuthors = "未知歌手"
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicAuthors"))
        End Set
        Get
            Return _MusicAuthors
        End Get
    End Property


    Private _MusicSize As String     '音乐文件大小
    Property MusicSize As String
        Set(value As String)
            _MusicSize = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicSize"))
        End Set
        Get
            Return _MusicSize
        End Get
    End Property


    Private _MusicYear As String    '音乐年代
    Property MusicYear
        Set(value)
            If value = "" Then value = "未知年代"
            _MusicYear = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicYear"))
        End Set
        Get
            Return _MusicYear
        End Get
    End Property


    Private _MusicGenre As String    '音乐风格
    Property MusicGenre
        Set(value)
            If value = "" Then value = "未知风格"
            _MusicGenre = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicGenre"))
        End Set
        Get
            Return _MusicGenre
        End Get
    End Property


    Private _MusicLength As String     '音乐时长
    Property MusicLength As String
        Set(value As String)
            _MusicLength = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicLength"))
        End Set
        Get
            Return _MusicLength
        End Get
    End Property


    Private _MusicBitrate As String     '音乐比特率
    Property MusicBitrate
        Set(value)
            _MusicBitrate = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicBitrate"))
        End Set
        Get
            Return _MusicBitrate
        End Get
    End Property


    Private Property _MusicAlbum As String
    Public Property MusicAlbum As String
        Set(value As String)
            If value = "" Then value = "未知专辑"
            _MusicAlbum = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicAlbum"))
        End Set
        Get
            Return _MusicAlbum
        End Get
    End Property

    Private Property _MusicNameAndAuthors As String
    Public Property MusicNameAndAuthors As String
        Get
            Return _MusicNameAndAuthors
        End Get
        Set(value As String)
            _MusicNameAndAuthors = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MusicNameAndAuthors"))
        End Set
    End Property



End Class


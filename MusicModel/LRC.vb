Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Text.RegularExpressions

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



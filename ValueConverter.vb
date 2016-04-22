
#Region "绑定转换器"

Imports System.Globalization

Class valueplusoneConverter
    Implements IValueConverter
    'items index默认从0开始，该转化器将其+1

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return (Val(value) + 1).ToString()
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

 
Class volto100Converter
    Implements IValueConverter
    '将mediaplay音量从0.1转化为10
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return Math.Round(value * 100)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New System.Exception("绑定发生错误!")
    End Function
End Class

Class volbtntooltipConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return "当前音量：" + value.ToString + vbCrLf + "滑动滑轮调节音量"
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New System.Exception("绑定发生错误!")
    End Function
End Class

Class volpathVisibility0Converter
    Implements IValueConverter
    '将mediaplay音量转化为音量图标的显示(当音量为0时，显示x)
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If Val(value) = 0 Then
            Return Visibility.Visible
        Else
            Return Visibility.Hidden
        End If

    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New System.Exception("绑定发生错误!")
    End Function
End Class

Class volpathopactiy33Converter
    Implements IValueConverter
    '将mediaplay音量转化为音量图标的显示(|)
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If Val(value) > 0 Then
            Return Visibility.Visible
        Else
            Return Visibility.Hidden
        End If

    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New System.Exception("绑定发生错误!")
    End Function
End Class

Class listboxitemcontextmenu
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing Then Return Nothing
        Dim tempmusic As Music = value
        Dim musicname As String = tempmusic.MusicTitle
        If musicname.Length >= 20 Then
            musicname = Left(musicname, 17) + "..."
        End If
        Return "播放  " + musicname
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New Exception("listboxitemcontextmenu ConvertBack 粗错了！")
    End Function
End Class


#End Region

Imports System.Text.RegularExpressions


Public Structure NetMusic

    Dim lrcstring As String

    Dim mp3url As String

End Structure

Public Class Music_itwusun

    'http://ws.itwusun.com/search/song/taylor%20swift


    Shared Function Search(ParamArray content() As String) As String

        Dim urlend As String = Nothing

        For Each temp In content
            urlend += temp.Replace(" ", "%20") + "%20"
        Next

        Dim url As String = "http://ws.itwusun.com/search/song/" + urlend.Substring(0, urlend.Length - 3)

        Return url

    End Function




End Class


Public Class Music_cnlyric

    Function GetLrcListForm_cnlyric(music As Music) As List(Of LrcUrlInfo)

        Dim title = music.MusicTitle
        Dim artist = music.MusicAuthors
        If artist = "未知歌手" Then artist = ""

        '中文歌曲
        If HaveChinese(title) Then

            Dim lrc_uri As String = "http://www.cnlyric.com/search.php?k=" + chineseToHex(title) + "&t=s"

            Dim http_downstr = DownStringFromNetAsync(lrc_uri)

            Dim lrcpart = Regex.Matches(http_downstr, ">\d{1,}\.<")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)


            Dim lrcpath = Regex.Matches(http_downstr, "(LrcDown)/\d{1,}/\d{1,}(\.lrc)")   '下载链接

            For index = 1 To lrcpart.Count

                Dim s1 As String = lrcpath.Item(index - 1).ToString()

                temp_list_lrcurls.Add(New LrcUrlInfo With {.url = "http://www.cnlyric.com/" + s1})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                list_lrcurls.Add(New LrcUrlInfo With {.content = DownStringFromNetAsync(item.url), .url = item.url})

            Next

            Return list_lrcurls

            '英文歌曲
        Else


            title = title.Replace(" ", "+")

            artist = artist.Replace(" ", "+")

            Dim lrcUri As String = "http://syair.info/search/?artist=" + artist + "&title=" + title + "&format=lrc"

            Dim result As String = DownStringFromNetAsync(lrcUri)



            Dim lrcIP = Regex.Matches(result, "[A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][.]html")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In lrcIP

                temp_list_lrcurls.Add(New LrcUrlInfo() With {.url = "http://syair.info/lyrics/" + item.ToString()})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                Dim lrcTexts = Regex.Matches(DownStringFromNetAsync(item.url), "\[(.+)<br />")

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

End Class


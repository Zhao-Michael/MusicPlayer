Imports System.Text.RegularExpressions
Imports MyMusicWPF.Net

Public Structure NetMusic

    Dim lrcstring As String

    Dim mp3_url As String

    Dim title As String

    Dim author As String

    Dim mp3url As Uri

    Sub GetActualUri()



    End Sub

End Structure

Public Class Music_itwusun

    'http://ws.itwusun.com/search/song/taylor%20swift



    Shared Function Search(ParamArray content() As String) As List(Of NetMusic)

        Dim list As New List(Of NetMusic)

        Dim urlend As String = Nothing

        For Each temp In content
            urlend += temp.Replace(" ", "%20") + "%20"
        Next

        Dim url As String = "http://ws.itwusun.com/search/song/" + urlend.Substring(0, urlend.Length - 3)

        Dim html = DownStringFromNet(url).Split(New String() {"<div id=""song-list"" class=""table-responsive"">"}, StringSplitOptions.RemoveEmptyEntries)(1) _
        .Split(New String() {"<div class=""nextpage"">"}, StringSplitOptions.RemoveEmptyEntries)(0)


        Dim result = Regex.Matches(html, "type=""\w{1,10}""\s+id=""\w{3,}""\s{1}name="".*""\s{1}")

        For index = 0 To result.Count - 1

            Dim temp = result.Item(index).ToString()

            Dim arr = temp.Split("""")

            Dim t_NetMusic As New NetMusic

            t_NetMusic.mp3_url = "http://ws.itwusun.com/fsong/" + arr(1) + "/id_" + arr(3) + ".html"

            t_NetMusic.title = arr(5)

            list.Add(t_NetMusic)

        Next

        Return list

    End Function


End Class


Public Class Music_cnlyric

    Function GetLrcListForm_cnlyric(music As Music) As List(Of LrcUrlInfo)

        Dim title = music.MusicTitle
        Dim artist = music.MusicAuthors
        If artist = "未知歌手" Then artist = ""

        '中文歌曲
        If HaveChinese(title) Then

            Dim lrc_uri As String = "http://www.cnlyric.com/search.php?k=" + ChineseToHex(title) + "&t=s"

            Dim http_downstr = DownStringFromNet(lrc_uri)

            Dim lrcpart = Regex.Matches(http_downstr, ">\d{1,}\.<")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)


            Dim lrcpath = Regex.Matches(http_downstr, "(LrcDown)/\d{1,}/\d{1,}(\.lrc)")   '下载链接

            For index = 1 To lrcpart.Count

                Dim s1 As String = lrcpath.Item(index - 1).ToString()

                temp_list_lrcurls.Add(New LrcUrlInfo With {.url = "http://www.cnlyric.com/" + s1})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                list_lrcurls.Add(New LrcUrlInfo With {.content = DownStringFromNet(item.url), .url = item.url})

            Next

            Return list_lrcurls

            '英文歌曲
        Else


            title = title.Replace(" ", "+")

            artist = artist.Replace(" ", "+")

            Dim lrcUri As String = "http://syair.info/search/?artist=" + artist + "&title=" + title + "&format=lrc"

            Dim result As String = DownStringFromNet(lrcUri)



            Dim lrcIP = Regex.Matches(result, "[A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][A-Za-z0-9][.]html")

            Dim temp_list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In lrcIP

                temp_list_lrcurls.Add(New LrcUrlInfo() With {.url = "http://syair.info/lyrics/" + item.ToString()})

            Next

            Dim list_lrcurls As New List(Of LrcUrlInfo)

            For Each item In temp_list_lrcurls

                Dim lrcTexts = Regex.Matches(DownStringFromNet(item.url), "\[(.+)<br />")

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


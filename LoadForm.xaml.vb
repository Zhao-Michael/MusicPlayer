Imports System.Windows.Threading

Public Class LoadForm
    Private Sub Grid_MouseDown(sender As Object, e As MouseButtonEventArgs)

        Environment.Exit(0)
    End Sub

    Dim timer As New DispatcherTimer()


    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim mywin = VisualTreeHelper.GetChild(LoadForm, 0)
        Dim MainBorder As Border = LogicalTreeHelper.FindLogicalNode(mywin, "MainBorder")

        Dim textblock As TextBlock = LogicalTreeHelper.FindLogicalNode(mywin, "textblock")
        timer.Interval = New TimeSpan(2000000)
        Dim i As Int16 = 0
        AddHandler timer.Tick, New EventHandler(Sub()
                                                    Select Case i
                                                        Case 0
                                                            textblock.Text = "Loading ."
                                                            i += 1
                                                        Case 1
                                                            textblock.Text = "Loading .."
                                                            i += 1
                                                        Case 2
                                                            textblock.Text = "Loading ..."
                                                            i += 1
                                                    End Select
                                                    If i = 3 Then
                                                        i = 0
                                                    End If

                                                End Sub)
        timer.Start()


    End Sub


    Private Sub MainGrid_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.LeftButton = MouseButtonState.Pressed Then
            DragMove()
        End If
    End Sub

End Class

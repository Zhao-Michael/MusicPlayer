Imports System.Runtime.InteropServices


Module SHFileCtrl


#Region "查看文件的属性框"


    '------------------------------

    '查看文件属性API

    Public Structure SHELLEXECUTEINFO

        Dim cbSize As Integer

        Dim fMask As Integer

        Dim hwnd As IntPtr

        Dim lpVerb As String

        Dim lpFile As String

        Dim lpParameters As String

        Dim lpDirectory As String

        Dim nShow As Integer

        Dim hInstApp As IntPtr

        Dim lpIDList As IntPtr

        Dim lpClass As String

        Dim hkeyClass As IntPtr

        Dim dwHotKey As Integer

        Dim hIcon As IntPtr

        Dim hProcess As IntPtr

    End Structure




    ' 显示方式

    'Private Const SW_HIDE = 0

    'Private Const SW_SHOWNORMAL = 1

    'Private Const SW_SHOWMINIMIZED = 2

    'Private Const SW_SHOWMAXIMIZED = 3

    'Private Const SW_MAXIMIZE = 3

    'Private Const SW_SHOWNOACTIVATE = 4

    'Private Const SW_SHOW = 5

    'Private Const SW_MINIMIZE = 6

    Private Const SW_SHOWMINNOACTIVE = 7

    'Private Const SW_SHOWNA = 8

    'Private Const SW_RESTORE = 9


    'Private Const SEE_MASK_CLASSKEY = &H3

    'Private Const SEE_MASK_CLASSNAME = &H1

    'Private Const SEE_MASK_CONNECTNETDRV = &H80

    'Private Const SEE_MASK_DOENVSUBST = &H200

    'Private Const SEE_MASK_FLAG_DDEWAIT = &H100

    Private Const SEE_MASK_FLAG_NO_UI = &H400

    'Private Const SEE_MASK_HOTKEY = &H20

    'Private Const SEE_MASK_ICON = &H10

    'Private Const SEE_MASK_IDLIST = &H4

    Private Const SEE_MASK_INVOKEIDLIST = &HC

    Private Const SEE_MASK_NOCLOSEPROCESS = &H40


    Private Declare Function ShellExecuteEx Lib "shell32.dll" (ByRef lpShellInfo As SHELLEXECUTEINFO) As Integer


#End Region


    '显示属性对话框

    '参数

    'hWnd IntPtr 父窗体句柄

    'sFile String 要查看属性的文件

    '调用：ShowPropertie(Me.Handle, sFile)

    Public Sub ShowPropertie(ByVal hWnd As IntPtr, ByVal sFile As String)


        Dim sInfo As New SHELLEXECUTEINFO

        Dim iRet As Integer


        Try

            With sInfo

                .cbSize = Marshal.SizeOf(sInfo)

                .fMask = SEE_MASK_NOCLOSEPROCESS Or SEE_MASK_INVOKEIDLIST Or SEE_MASK_FLAG_NO_UI

                .hwnd = hWnd

                .lpVerb = "properties"

                .lpFile = sFile

                .lpParameters = ""

                .lpDirectory = vbNullString

                .nShow = SW_SHOWMINNOACTIVE

                .hInstApp = New IntPtr(0)

                .lpIDList = New IntPtr(0)

                .lpClass = vbNullString

                .hkeyClass = New IntPtr(0)

                .dwHotKey = 0

                .hIcon = New IntPtr(0)

                .hProcess = New IntPtr(0)

            End With


            iRet = ShellExecuteEx(sInfo)


        Catch ex As Exception

            MsgBox(iRet & vbCrLf & Err.ToString)

        End Try

    End Sub


End Module
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevExpress.Sample

    Public Partial Class Form1
        Inherits Form

        Public Sub New()
            InitializeComponent()
            Dim list As List(Of Record) = New List(Of Record)()
            Dim rnd As Random = New Random()
            For i As Integer = 0 To 100 - 1
                'Record r = new Record(i,
                '    "abc".Substring(i % 3, 1),
                '    "vwxyz".Substring((i * i) % 5, 1),
                '    "vwxyz".Substring((i * i) % 3 + 2, 1),
                '    "vwxyz".Substring((i * i * i) % 5, 1)
                '    );
                Dim r As Record = New Record(i, "abc".Substring(rnd.Next(3), 1), "vwxyz".Substring(rnd.Next(5), 1), "vwxyz".Substring(rnd.Next(3) + 2, 1), "vwxyz".Substring(rnd.Next(2) + 2, 1))
                list.Add(r)
            Next

            Dim ds As SimpleServerModeDataSource = New SimpleServerModeDataSource(GetType(Record), "ID", list)
            gridView1.OptionsView.ShowGroupedColumns = True
            gridView1.OptionsView.GroupFooterShowMode = XtraGrid.Views.Grid.GroupFooterShowMode.VisibleAlways
            gridControl1.DataSource = ds
        End Sub
    End Class

    Public Class Record

        Private idField As Integer

        Private gField As String

        Private uField As String

        Private vField As String

        Private wField As String

        Public Property ID As Integer
            Get
                Return idField
            End Get

            Set(ByVal value As Integer)
                idField = value
            End Set
        End Property

        Public Property G As String
            Get
                Return gField
            End Get

            Set(ByVal value As String)
                gField = value
            End Set
        End Property

        Public Property U As String
            Get
                Return uField
            End Get

            Set(ByVal value As String)
                uField = value
            End Set
        End Property

        Public Property V As String
            Get
                Return vField
            End Get

            Set(ByVal value As String)
                vField = value
            End Set
        End Property

        Public Property W As String
            Get
                Return wField
            End Get

            Set(ByVal value As String)
                wField = value
            End Set
        End Property

        Public Sub New(ByVal id As Integer, ByVal g As String, ByVal u As String, ByVal v As String, ByVal w As String)
            idField = id
            gField = g
            uField = u
            vField = v
            wField = w
        End Sub
    End Class
End Namespace

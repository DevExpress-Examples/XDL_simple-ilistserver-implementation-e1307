Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports DevExpress.Sample

Namespace DevExpress.Sample
	Partial Public Class Form1
		Inherits Form
		Public Sub New()
			InitializeComponent()

			Dim list As New List(Of Record)()
			Dim rnd As New Random()
			For i As Integer = 0 To 99
				'Record r = new Record(i,
				'    "abc".Substring(i % 3, 1),
				'    "vwxyz".Substring((i * i) % 5, 1),
				'    "vwxyz".Substring((i * i) % 3 + 2, 1),
				'    "vwxyz".Substring((i * i * i) % 5, 1)
				'    );
				Dim r As New Record(i, "abc".Substring(rnd.Next(3), 1), "vwxyz".Substring(rnd.Next(5), 1), "vwxyz".Substring(rnd.Next(3) + 2, 1), "vwxyz".Substring(rnd.Next(2) + 2, 1))

				list.Add(r)
			Next i

			Dim ds As New SimpleServerModeDataSource(GetType(Record), "ID", list)

			gridView1.OptionsView.ShowGroupedColumns = True
			gridView1.OptionsView.GroupFooterShowMode = DevExpress.XtraGrid.Views.Grid.GroupFooterShowMode.VisibleAlways

			gridControl1.DataSource = ds
		End Sub
	End Class

	Public Class Record
		Private id_Renamed As Integer
		Private g_Renamed As String
		Private u_Renamed As String
		Private v_Renamed As String
		Private w_Renamed As String

		Public Property ID() As Integer
			Get
				Return id_Renamed
			End Get
			Set(ByVal value As Integer)
				id_Renamed = value
			End Set
		End Property
		Public Property G() As String
			Get
				Return g_Renamed
			End Get
			Set(ByVal value As String)
				g_Renamed = value
			End Set
		End Property
		Public Property U() As String
			Get
				Return u_Renamed
			End Get
			Set(ByVal value As String)
				u_Renamed = value
			End Set
		End Property
		Public Property V() As String
			Get
				Return v_Renamed
			End Get
			Set(ByVal value As String)
				v_Renamed = value
			End Set
		End Property
		Public Property W() As String
			Get
				Return w_Renamed
			End Get
			Set(ByVal value As String)
				w_Renamed = value
			End Set
		End Property

		Public Sub New(ByVal id As Integer, ByVal g As String, ByVal u As String, ByVal v As String, ByVal w As String)
			Me.id_Renamed = id
			Me.g_Renamed = g
			Me.u_Renamed = u
			Me.v_Renamed = v
			Me.w_Renamed = w
		End Sub
	End Class
End Namespace
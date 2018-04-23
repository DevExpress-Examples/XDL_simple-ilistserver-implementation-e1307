Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.ComponentModel
Imports System.Collections
Imports DevExpress.Data
Imports DevExpress.Data.Filtering
Imports DevExpress.Data.Filtering.Helpers

Namespace DevExpress.Sample
	Public Class SimpleServerModeDataSource
		Implements IListServer
		Private objectType As Type
		Private pdKey As PropertyDescriptor

		Private storage As ArrayList 'hoax
		Private storageProxy As ArrayList
		Private groups As Hashtable
		Private totals As Dictionary(Of Object, Object)

		Private filter As CriteriaOperator
		Private sortInfo As IList(Of ServerModeOrderDescriptor)
		Private groupCount As Integer
		Private groupSummaryInfo As ICollection(Of ServerModeSummaryDescriptor)
		Private totalSummaryInfo As ICollection(Of ServerModeSummaryDescriptor)


		Public Sub New(ByVal objectType As Type, ByVal keyProperty As String, ByVal data As ICollection)
			Me.New(objectType, keyProperty)
			storage.AddRange(data)
			ProcessCollection()
		End Sub

		Public Sub New(ByVal objectType As Type, ByVal keyProperty As String)
			Me.objectType = objectType
			Dim pdc As PropertyDescriptorCollection = TypeDescriptor.GetProperties(objectType)
			pdKey = pdc(keyProperty)

			storage = New ArrayList()
			storageProxy = New ArrayList()
			groups = New Hashtable()
			totals = New Dictionary(Of Object, Object)()
		End Sub

		Private Sub SummaryCollect(ByVal dict As Dictionary(Of Object, Object), ByVal info As ICollection(Of ServerModeSummaryDescriptor), ByVal obj As Object)
			If info Is Nothing OrElse dict Is Nothing Then
				Return
			End If
			For Each item As ServerModeSummaryDescriptor In info
				Dim acc As Decimal = 0
				Try
					acc = Convert.ToDecimal(dict(item.SummaryKey))
				Catch
				End Try
				If item.SummaryType = Aggregate.Count Then
					acc += 1
				Else
					Dim v As Decimal = 0
					Try
						Dim evaluator As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), item.SummaryExpression)
						v = Convert.ToDecimal(evaluator.Evaluate(obj))
					Catch
					End Try
					Select Case item.SummaryType
						Case Aggregate.Sum
							acc += v
					End Select
				End If
				dict(item.SummaryKey) = acc
			Next item
		End Sub

		Private Sub SummarySetUp(ByVal dict As Dictionary(Of Object, Object), ByVal info As ICollection(Of ServerModeSummaryDescriptor))
			If info Is Nothing OrElse dict Is Nothing Then
				Return
			End If
			dict.Clear()
			For Each item As ServerModeSummaryDescriptor In info
				dict.Add(item.SummaryKey, 0)
			Next item
		End Sub

		Private Sub ProcessCollection()
			Dim evaluator As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), filter)
			storageProxy.Clear()
			SummarySetUp(totals, totalSummaryInfo)
			For Each o As Object In storage
				If evaluator.Fit(o) Then
					storageProxy.Add(o)
					SummaryCollect(totals, totalSummaryInfo, o)
				End If
			Next o
			If sortInfo IsNot Nothing Then
				storageProxy.Sort(New SimpleComparer(sortInfo, objectType))
			End If
			groups.Clear()
		End Sub

		#Region "IListServer Members"

		Public Event ExceptionThrown As EventHandler(Of ServerModeExceptionThrownEventArgs) Implements IListServer.ExceptionThrown
		Public Event InconsistencyDetected As EventHandler(Of ServerModeInconsistencyDetectedEventArgs) Implements IListServer.InconsistencyDetected

		Private Sub Apply(ByVal filterCriteria As CriteriaOperator, ByVal sortInfo As ICollection(Of ServerModeOrderDescriptor), ByVal groupCount As Integer, ByVal groupSummaryInfo As ICollection(Of ServerModeSummaryDescriptor), ByVal totalSummaryInfo As ICollection(Of ServerModeSummaryDescriptor)) Implements IListServer.Apply
			Me.filter = filterCriteria
			Dim sorts As New List(Of ServerModeOrderDescriptor)()
			If sortInfo IsNot Nothing Then
				sorts.AddRange(sortInfo)
			End If
			Me.sortInfo = sorts
			Me.groupCount = groupCount
			Me.groupSummaryInfo = groupSummaryInfo
			Me.totalSummaryInfo = totalSummaryInfo
			ProcessCollection()
		End Sub

		Private Sub Refresh() Implements IListServer.Refresh
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Private Function FindIncremental(ByVal expression As CriteriaOperator, ByVal value As String, ByVal startIndex As Integer, ByVal searchUp As Boolean, ByVal ignoreStartRow As Boolean, ByVal allowLoop As Boolean) As Integer Implements IListServer.FindIncremental
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Private Function LocateByValue(ByVal expression As CriteriaOperator, ByVal value As Object, ByVal startIndex As Integer, ByVal searchUp As Boolean) As Integer Implements IListServer.LocateByValue
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Private Function GetAllFilteredAndSortedRows() As IList Implements IListServer.GetAllFilteredAndSortedRows
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Private Function GetGroupInfo(ByVal parentGroup As ListSourceGroupInfo) As List(Of ListSourceGroupInfo) Implements IListServer.GetGroupInfo
			Dim rows As New List(Of ListSourceGroupInfo)()
			Dim uniqueValues As New ArrayList()
			Dim level As Integer
			If (parentGroup Is Nothing) Then
				level = 0
			Else
				level = (parentGroup.Level + 1)
			End If
			Dim evaluator As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo(level).SortExpression)
			For Each o As Object In storageProxy
				Dim group As ListSourceGroupInfo = parentGroup
				Do While group IsNot Nothing
					System.Diagnostics.Debug.Assert((group.Level = 0) OrElse groups(group) IsNot Nothing)
					Dim evaluator2 As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo(group.Level).SortExpression)
					Dim fv As Object = evaluator2.Evaluate(o)
					If (Not Equals(fv, group.GroupValue)) Then
						GoTo Skip
					End If
					group = CType(groups(group), ListSourceGroupInfo)
				Loop
				Dim v As Object = evaluator.Evaluate(o)
				Dim info As ListSourceGroupInfo
				Dim index As Integer = uniqueValues.IndexOf(v)
				If index < 0 Then
					uniqueValues.Add(v)
					info = New SimpleListSourceGroupInfo()
					info.GroupValue = v
					info.Level = level
					SummarySetUp(info.Summary, groupSummaryInfo)
					rows.Add(info)
					groups.Add(info, parentGroup)
				Else
					info = rows(index)
				End If
				info.ChildDataRowCount += 1
				SummaryCollect(info.Summary, groupSummaryInfo, o)
			Skip:

			Next o
			Return rows
		End Function

		Private Function GetRowIndexByKey(ByVal key As Object) As Integer Implements IListServer.GetRowIndexByKey
			For i As Integer = 0 To storageProxy.Count - 1
				If Equals(pdKey.GetValue(storageProxy(i)), key) Then
					Return i
				End If
			Next i
			Return -1
		End Function

		Private Function GetRowKey(ByVal index As Integer) As Object Implements IListServer.GetRowKey
			Return pdKey.GetValue(Me(index))
		End Function

		Private Function GetTotalSummary() As Dictionary(Of Object, Object) Implements IListServer.GetTotalSummary
			Return totals
		End Function

		Private Function GetUniqueColumnValues(ByVal expression As CriteriaOperator, ByVal maxCount As Integer, ByVal includeFilteredOut As Boolean) As Object() Implements IListServer.GetUniqueColumnValues
			Dim uniqueValues As New ArrayList()
			Dim list As IList
			If includeFilteredOut Then
				list = storage
			Else
				list = storageProxy
			End If
			Dim evaluator As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), expression)
			For Each o As Object In list
				Dim v As Object = evaluator.Evaluate(o)
				Dim index As Integer = uniqueValues.IndexOf(v)
				If index < 0 Then
					uniqueValues.Add(v)
					If maxCount > 0 AndAlso uniqueValues.Count >= maxCount Then
						Exit For
					End If
				End If
			Next o
			Return uniqueValues.ToArray()
		End Function

		#End Region

		#Region "IList Members"

		Public Function Add(ByVal value As Object) As Integer Implements System.Collections.IList.Add
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Public Sub Clear() Implements System.Collections.IList.Clear
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Public Function Contains(ByVal value As Object) As Boolean Implements System.Collections.IList.Contains
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Public Function IndexOf(ByVal value As Object) As Integer Implements System.Collections.IList.IndexOf
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Public Sub Insert(ByVal index As Integer, ByVal value As Object) Implements System.Collections.IList.Insert
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Public ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IList.IsFixedSize
			Get
				Throw New Exception("The method or operation is not implemented.")
			End Get
		End Property

		Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.IList.IsReadOnly
			Get
				Throw New Exception("The method or operation is not implemented.")
			End Get
		End Property

		Public Sub Remove(ByVal value As Object) Implements System.Collections.IList.Remove
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Public Sub RemoveAt(ByVal index As Integer) Implements System.Collections.IList.RemoveAt
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Default Public Property Item(ByVal index As Integer) As Object Implements System.Collections.IList.Item
			Get
				Return storageProxy(index)
			End Get
			Set(ByVal value As Object)
				Throw New Exception("The method or operation is not implemented.")
			End Set
		End Property

		#End Region

		#Region "ICollection Members"

		Public Sub CopyTo(ByVal array As Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
			Throw New Exception("The method or operation is not implemented.")
		End Sub

		Public ReadOnly Property Count() As Integer Implements System.Collections.ICollection.Count
			Get
				Return storageProxy.Count
			End Get
		End Property

		Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
			Get
				Throw New Exception("The method or operation is not implemented.")
			End Get
		End Property

		Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
			Get
				Throw New Exception("The method or operation is not implemented.")
			End Get
		End Property

		#End Region

		#Region "IEnumerable Members"

		Public Function GetEnumerator() As IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
			Return storageProxy.GetEnumerator()
		End Function

		#End Region

	End Class

	Friend Class SimpleListSourceGroupInfo
		Inherits ListSourceGroupInfo
		Private summary_Renamed As Dictionary(Of Object, Object)
		Public Overrides ReadOnly Property Summary() As Dictionary(Of Object, Object)
			Get
				Return summary_Renamed
			End Get
		End Property
		Public Sub New()
			summary_Renamed = New Dictionary(Of Object, Object)()
		End Sub
	End Class

	Friend Class SimpleComparer
		Implements IComparer
		Private sortInfo As ICollection(Of ServerModeOrderDescriptor)
		Private objectType As Type

		Public Sub New(ByVal sortInfo As ICollection(Of ServerModeOrderDescriptor), ByVal objectType As Type)
			Me.sortInfo = sortInfo
			Me.objectType = objectType
		End Sub

		#Region "IComparer Members"

		Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
			For Each info As ServerModeOrderDescriptor In sortInfo
				Dim evaluator As New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), info.SortExpression)
				Dim xx As Object = evaluator.Evaluate(x)
				Dim yy As Object = evaluator.Evaluate(y)
				Dim sign As Integer = Comparer.Default.Compare(xx, yy)
				If sign <> 0 Then
					If (info.IsDesc) Then
						Return -sign
					Else
						Return sign
					End If
				End If
			Next info
			Return 0
		End Function

		#End Region
	End Class
End Namespace

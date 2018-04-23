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
		Private sortInfo As ListSortDescriptionCollection
		Private groupCount As Integer
		Private summaryInfo As List(Of ListSourceSummaryItem)
		Private totalSummaryInfo As List(Of ListSourceSummaryItem)


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

		Private Sub SummaryCollect(ByVal dict As Dictionary(Of Object, Object), ByVal info As List(Of ListSourceSummaryItem), ByVal obj As Object)
			If info Is Nothing OrElse dict Is Nothing Then
				Return
			End If
			For Each item As ListSourceSummaryItem In info
				Dim acc As Decimal = 0
				Try
					acc = Convert.ToDecimal(dict(item.Key))
				Catch
				End Try
				If item.SummaryType = SummaryItemType.Count Then
					acc += 1
				Else
					Dim v As Decimal = 0
					Try
						v = Convert.ToDecimal(item.Descriptor.GetValue(obj))
					Catch
					End Try
					Select Case item.SummaryType
						Case SummaryItemType.Sum
							acc += v
					End Select
				End If
				dict(item.Key) = acc
			Next item
		End Sub

		Private Sub SummarySetUp(ByVal dict As Dictionary(Of Object, Object), ByVal info As List(Of ListSourceSummaryItem))
			If info Is Nothing OrElse dict Is Nothing Then
				Return
			End If
			dict.Clear()
			For Each item As ListSourceSummaryItem In info
				dict.Add(item.Key, 0)
			Next item
		End Sub

		Private Sub ProcessCollection()
			Dim pdc As PropertyDescriptorCollection = TypeDescriptor.GetProperties(objectType)
			Dim evaluator As New ExpressionEvaluator(pdc, filter)
			storageProxy.Clear()
			SummarySetUp(totals, totalSummaryInfo)
			For Each o As Object In storage
				If evaluator.Fit(o) Then
					storageProxy.Add(o)
					SummaryCollect(totals, totalSummaryInfo, o)
				End If
			Next o
			If sortInfo IsNot Nothing Then
				storageProxy.Sort(New SimpleComparer(sortInfo))
			End If
			groups.Clear()
		End Sub

		#Region "IListServer Members"

		Public Sub ApplySort(ByVal sortInfo As ListSortDescriptionCollection, ByVal groupCount As Integer, ByVal summaryInfo As List(Of ListSourceSummaryItem), ByVal totalSummaryInfo As List(Of ListSourceSummaryItem)) Implements IListServer.ApplySort
			Me.sortInfo = sortInfo
			Me.groupCount = groupCount
			Me.summaryInfo = summaryInfo
			Me.totalSummaryInfo = totalSummaryInfo
			ProcessCollection()
		End Sub

		Public Property FilterCriteria() As CriteriaOperator Implements IListServer.FilterCriteria
			Get
				Return filter
			End Get
			Set(ByVal value As CriteriaOperator)
				If Equals(filter, value) Then
					Return
				End If
				filter = value
				ProcessCollection()
			End Set
		End Property

		Public Function FindIncremental(ByVal column As PropertyDescriptor, ByVal value As String, ByVal startIndex As Integer, ByVal searchUp As Boolean) As Integer Implements IListServer.FindIncremental
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Public Function FindKeyByBeginWith(ByVal column As PropertyDescriptor, ByVal value As String) As Object Implements IListServer.FindKeyByBeginWith
			Throw New Exception("The method or operation is not implemented.")
		End Function

		Public Function FindKeyByValue(ByVal column As PropertyDescriptor, ByVal value As Object) As Object Implements IListServer.FindKeyByValue
			For Each o As Object In storageProxy
				If Equals(column.GetValue(o), value) Then
					Return pdKey.GetValue(o)
				End If
			Next o
			Return Nothing
		End Function

		Public Function GetGroupInfo(ByVal parentGroup As ListSourceGroupInfo) As List(Of ListSourceGroupInfo) Implements IListServer.GetGroupInfo
			Dim rows As List(Of ListSourceGroupInfo) = New List(Of ListSourceGroupInfo)()
			Dim uniqueValues As New ArrayList()
			Dim level As Integer
			If (parentGroup Is Nothing) Then
				level = 0
			Else
				level = (parentGroup.Level + 1)
			End If
			Dim pd As PropertyDescriptor = sortInfo(level).PropertyDescriptor
			For Each o As Object In storageProxy
				Dim group As ListSourceGroupInfo = parentGroup
				Do While group IsNot Nothing
					System.Diagnostics.Debug.Assert((group.Level = 0) OrElse groups(group) IsNot Nothing)
					Dim fv As Object = sortInfo(group.Level).PropertyDescriptor.GetValue(o)
					If (Not Equals(fv, group.GroupValue)) Then
						GoTo Skip
					End If
					group = CType(groups(group), ListSourceGroupInfo)
				Loop
				Dim v As Object = pd.GetValue(o)
				Dim info As ListSourceGroupInfo
				Dim index As Integer = uniqueValues.IndexOf(v)
				If index < 0 Then
					uniqueValues.Add(v)
					info = New SimpleListSourceGroupInfo()
					info.GroupValue = v
					info.Level = level
					SummarySetUp(info.Summary, summaryInfo)
					rows.Add(info)
					groups.Add(info, parentGroup)
				Else
					info = rows(index)
				End If
				info.ChildDataRowCount += 1
				SummaryCollect(info.Summary, summaryInfo, o)
				Skip:

			Next o
			Return rows
		End Function

		Public Function GetRowIndexByKey(ByVal key As Object) As Integer Implements IListServer.GetRowIndexByKey
			For i As Integer = 0 To storageProxy.Count - 1
				If Equals(pdKey.GetValue(storageProxy(i)), key) Then
					Return i
				End If
			Next i
			Return -1
		End Function

		Public Function GetRowKey(ByVal index As Integer) As Object Implements IListServer.GetRowKey
			Return pdKey.GetValue(Me(index))
		End Function

		Public Function GetTotalSummary() As Dictionary(Of Object, Object) Implements IListServer.GetTotalSummary
			Return totals
		End Function

		Public Function GetUniqueColumnValues(ByVal descriptor As PropertyDescriptor, ByVal maxCount As Integer, ByVal includeFilteredOut As Boolean, ByVal roundDataTime As Boolean) As Object() Implements IListServer.GetUniqueColumnValues
			Dim uniqueValues As New ArrayList()
			Dim list As IList
			If includeFilteredOut Then
				list = storage
			Else
				list = storageProxy
			End If
			For Each o As Object In list
				Dim v As Object = descriptor.GetValue(o)
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
		Private sortInfo As ListSortDescriptionCollection

		Public Sub New(ByVal sortInfo As ListSortDescriptionCollection)
			Me.sortInfo = sortInfo
		End Sub

		#Region "IComparer Members"

		Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
			For Each info As ListSortDescription In sortInfo
				Dim xx As Object = info.PropertyDescriptor.GetValue(x)
				Dim yy As Object = info.PropertyDescriptor.GetValue(y)
				Dim sign As Integer = Comparer.Default.Compare(xx, yy)
				If sign <> 0 Then
					If (info.SortDirection = ListSortDirection.Ascending) Then
						Return sign
					Else
						Return -sign
					End If
				End If
			Next info
			Return 0
		End Function

		#End Region
	End Class
End Namespace

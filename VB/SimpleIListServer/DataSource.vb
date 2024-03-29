Imports System
Imports System.Collections.Generic
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

        Private totals As List(Of Object)

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
            totals = New List(Of Object)()
        End Sub

        Private Sub SummaryCollect(ByVal dict As List(Of Object), ByVal info As ICollection(Of ServerModeSummaryDescriptor), ByVal obj As Object)
            If info Is Nothing OrElse dict Is Nothing Then Return
            Dim index As Integer = 0
            For Each item As ServerModeSummaryDescriptor In info
                Dim acc As Decimal = 0
                Try
                    acc = Convert.ToDecimal(dict(index))
                Catch
                End Try

                If item.SummaryType = Aggregate.Count Then
                    acc += 1
                Else
                    Dim v As Decimal = 0
                    Try
                        Dim evaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), item.SummaryExpression)
                        v = Convert.ToDecimal(evaluator.Evaluate(obj))
                    Catch
                    End Try

                    Select Case item.SummaryType
                        Case Aggregate.Sum
                            acc += v
                    End Select
                End If

                dict(index) = acc
                index += 1
            Next
        End Sub

        Private Sub SummarySetUp(ByVal dict As List(Of Object), ByVal info As ICollection(Of ServerModeSummaryDescriptor))
            If info Is Nothing OrElse dict Is Nothing Then Return
            dict.Clear()
            For Each item As ServerModeSummaryDescriptor In info
                dict.Add(0)
            Next
        End Sub

        Private Sub ProcessCollection()
            Dim evaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), filter)
            storageProxy.Clear()
            SummarySetUp(totals, totalSummaryInfo)
            For Each o As Object In storage
                If evaluator.Fit(o) Then
                    storageProxy.Add(o)
                    SummaryCollect(totals, totalSummaryInfo, o)
                End If
            Next

            If sortInfo IsNot Nothing Then
                storageProxy.Sort(New SimpleComparer(sortInfo, objectType))
            End If

            groups.Clear()
        End Sub

'#Region "IListServer Members"
        Private objectLock As Object = New Object()

        Private Event _ExceptionThrown As EventHandler(Of ServerModeExceptionThrownEventArgs)

        Private Custom Event ExceptionThrown As EventHandler(Of ServerModeExceptionThrownEventArgs) Implements IListServer.ExceptionThrown
            AddHandler(ByVal value As EventHandler(Of ServerModeExceptionThrownEventArgs))
                SyncLock objectLock
                    AddHandler _ExceptionThrown, value
                End SyncLock
            End AddHandler

            RemoveHandler(ByVal value As EventHandler(Of ServerModeExceptionThrownEventArgs))
                SyncLock objectLock
                    RemoveHandler _ExceptionThrown, value
                End SyncLock
            End RemoveHandler

            RaiseEvent(ByVal sender As Object, ByVal e As ServerModeExceptionThrownEventArgs)
            End RaiseEvent
        End Event

        Private Event _InconsistencyDetected As EventHandler(Of ServerModeInconsistencyDetectedEventArgs)

        Private Custom Event InconsistencyDetected As EventHandler(Of ServerModeInconsistencyDetectedEventArgs) Implements IListServer.InconsistencyDetected
            AddHandler(ByVal value As EventHandler(Of ServerModeInconsistencyDetectedEventArgs))
                SyncLock objectLock
                    AddHandler _InconsistencyDetected, value
                End SyncLock
            End AddHandler

            RemoveHandler(ByVal value As EventHandler(Of ServerModeInconsistencyDetectedEventArgs))
                SyncLock objectLock
                    RemoveHandler _InconsistencyDetected, value
                End SyncLock
            End RemoveHandler

            RaiseEvent(ByVal sender As Object, ByVal e As ServerModeInconsistencyDetectedEventArgs)
            End RaiseEvent
        End Event

        Private Sub Apply(ByVal filterCriteria As CriteriaOperator, ByVal sortInfo As ICollection(Of ServerModeOrderDescriptor()), ByVal groupCount As Integer, ByVal groupSummaryInfo As ICollection(Of ServerModeSummaryDescriptor), ByVal totalSummaryInfo As ICollection(Of ServerModeSummaryDescriptor)) Implements IListServer.Apply
            filter = filterCriteria
            Dim sorts As List(Of ServerModeOrderDescriptor) = New List(Of ServerModeOrderDescriptor)()
            If sortInfo IsNot Nothing Then
                For Each orderDescriptors As ServerModeOrderDescriptor() In sortInfo
                    If orderDescriptors.Length > 1 Then Throw New NotSupportedException("Multi-grouping is not supported by this collection")
                    sorts.Add(orderDescriptors(0))
                Next
            End If

            Me.sortInfo = sorts
            Me.groupCount = groupCount
            Me.groupSummaryInfo = groupSummaryInfo
            Me.totalSummaryInfo = totalSummaryInfo
            ProcessCollection()
        End Sub

        Private Sub Refresh() Implements IListServer.Refresh
            Throw New NotImplementedException()
        End Sub

        Private Function FindIncremental(ByVal expression As CriteriaOperator, ByVal value As String, ByVal startIndex As Integer, ByVal searchUp As Boolean, ByVal ignoreStartRow As Boolean, ByVal allowLoop As Boolean) As Integer Implements IListServer.FindIncremental
            Throw New NotImplementedException()
        End Function

        Private Function LocateByValue(ByVal expression As CriteriaOperator, ByVal value As Object, ByVal startIndex As Integer, ByVal searchUp As Boolean) As Integer Implements IListServer.LocateByValue
            Throw New NotImplementedException()
        End Function

        Private Function GetAllFilteredAndSortedRows() As IList Implements IListServer.GetAllFilteredAndSortedRows
            Throw New NotImplementedException()
        End Function

        Private Function GetGroupInfo(ByVal parentGroup As ListSourceGroupInfo) As List(Of ListSourceGroupInfo) Implements IListServer.GetGroupInfo
            Dim rows As List(Of ListSourceGroupInfo) = New List(Of ListSourceGroupInfo)()
            Dim uniqueValues As ArrayList = New ArrayList()
            Dim level As Integer = If(parentGroup Is Nothing, 0, parentGroup.Level + 1)
            Dim evaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo(level).SortExpression)
            For Each o As Object In storageProxy
                Dim group As ListSourceGroupInfo = parentGroup
                While group IsNot Nothing
                    System.Diagnostics.Debug.Assert(group.Level = 0 OrElse groups(group) IsNot Nothing)
                    Dim evaluator2 As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo(group.Level).SortExpression)
                    Dim fv As Object = evaluator2.Evaluate(o)
                    If Not Equals(fv, group.GroupValue) Then
                        GoTo Skip
                    End If

                    group = CType(groups(group), ListSourceGroupInfo)
                End While

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
            Next

            Return rows
        End Function

        Private Function GetRowIndexByKey(ByVal key As Object) As Integer Implements IListServer.GetRowIndexByKey
            For i As Integer = 0 To storageProxy.Count - 1
                If Equals(pdKey.GetValue(storageProxy(i)), key) Then Return i
            Next

            Return -1
        End Function

        Private Function GetRowKey(ByVal index As Integer) As Object Implements IListServer.GetRowKey
            Return pdKey.GetValue(CType(Me, IList)(index))
        End Function

        Private Function GetTotalSummary() As List(Of Object) Implements IListServer.GetTotalSummary
            Return totals
        End Function

        Private Function GetUniqueColumnValues(ByVal valuesExpression As CriteriaOperator, ByVal maxCount As Integer, ByVal filterExpression As CriteriaOperator, ByVal ignoreAppliedFilter As Boolean) As Object() Implements IListServer.GetUniqueColumnValues
            Dim uniqueValues As ArrayList = New ArrayList()
            Dim filterCriteria As CriteriaOperator = If(ignoreAppliedFilter, filterExpression, CriteriaOperator.And(filterExpression, filter))
            Dim fitEvaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), filterCriteria)
            Dim evaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), valuesExpression)
            For Each o As Object In storage
                If Not fitEvaluator.Fit(o) Then Continue For
                Dim v As Object = evaluator.Evaluate(o)
                Dim index As Integer = uniqueValues.IndexOf(v)
                If index < 0 Then
                    uniqueValues.Add(v)
                    If maxCount > 0 AndAlso uniqueValues.Count >= maxCount Then Exit For
                End If
            Next

            Return uniqueValues.ToArray()
        End Function

        Private Function PrefetchRows(ByVal groupsToPrefetch As ListSourceGroupInfo(), ByVal cancellationToken As Threading.CancellationToken) As Boolean Implements IListServer.PrefetchRows
            Throw New NotImplementedException()
        End Function

        Private Function LocateByExpression(ByVal expression As CriteriaOperator, ByVal startIndex As Integer, ByVal searchUp As Boolean) As Integer Implements IListServer.LocateByExpression
            Throw New NotImplementedException()
        End Function

'#End Region
'#Region "IList Members"
        Private Function Add(ByVal value As Object) As Integer Implements IList.Add
            Throw New NotImplementedException()
        End Function

        Private Sub Clear() Implements IList.Clear
            Throw New NotImplementedException()
        End Sub

        Private Function Contains(ByVal value As Object) As Boolean Implements IList.Contains
            Throw New NotImplementedException()
        End Function

        Private Function IndexOf(ByVal value As Object) As Integer Implements IList.IndexOf
            Throw New NotImplementedException()
        End Function

        Private Sub Insert(ByVal index As Integer, ByVal value As Object) Implements IList.Insert
            Throw New NotImplementedException()
        End Sub

        Private ReadOnly Property IsFixedSize As Boolean Implements IList.IsFixedSize
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Private ReadOnly Property IsReadOnly As Boolean Implements IList.IsReadOnly
            Get
                Return True
            End Get
        End Property

        Private Sub Remove(ByVal value As Object) Implements IList.Remove
            Throw New NotImplementedException()
        End Sub

        Private Sub RemoveAt(ByVal index As Integer) Implements IList.RemoveAt
            Throw New NotImplementedException()
        End Sub

        Private Property Item(ByVal index As Integer) As Object Implements IList.Item
            Get
                Return storageProxy(index)
            End Get

            Set(ByVal value As Object)
                Throw New NotImplementedException()
            End Set
        End Property

'#End Region
'#Region "ICollection Members"
        Private Sub CopyTo(ByVal array As Array, ByVal index As Integer) Implements ICollection.CopyTo
            Throw New NotImplementedException()
        End Sub

        Private ReadOnly Property Count As Integer Implements ICollection.Count
            Get
                Return storageProxy.Count
            End Get
        End Property

        Private ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Private ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
            Get
                Throw New NotImplementedException()
            End Get
        End Property

'#End Region
'#Region "IEnumerable Members"
        Private Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return storageProxy.GetEnumerator()
        End Function
'#End Region
    End Class

    Friend Class SimpleListSourceGroupInfo
        Inherits ListSourceGroupInfo

        Private summaryField As List(Of Object)

        Public Overrides ReadOnly Property Summary As List(Of Object)
            Get
                Return summaryField
            End Get
        End Property

        Public Sub New()
            summaryField = New List(Of Object)()
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

'#Region "IComparer Members"
        Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
            For Each info As ServerModeOrderDescriptor In sortInfo
                Dim evaluator As ExpressionEvaluator = New ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), info.SortExpression)
                Dim xx As Object = evaluator.Evaluate(x)
                Dim yy As Object = evaluator.Evaluate(y)
                Dim sign As Integer = Comparer.Default.Compare(xx, yy)
                If sign <> 0 Then Return If(info.IsDesc, -sign, sign)
            Next

            Return 0
        End Function
'#End Region
    End Class
End Namespace

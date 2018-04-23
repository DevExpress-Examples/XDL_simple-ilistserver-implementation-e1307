using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;

namespace DevExpress.Sample
{
    public class SimpleServerModeDataSource : IListServer {
        Type objectType;
        PropertyDescriptor pdKey;

        ArrayList storage; //hoax
        ArrayList storageProxy;
        Hashtable groups;
        List<object> totals;

        CriteriaOperator filter;
        IList<ServerModeOrderDescriptor> sortInfo;
        int groupCount;
        ICollection<ServerModeSummaryDescriptor> groupSummaryInfo;
        ICollection<ServerModeSummaryDescriptor> totalSummaryInfo;


        public SimpleServerModeDataSource(Type objectType, string keyProperty, ICollection data)
            : this(objectType, keyProperty) {
            storage.AddRange(data);
            ProcessCollection();
        }

        public SimpleServerModeDataSource(Type objectType, string keyProperty) {
            this.objectType = objectType;
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(objectType);
            pdKey = pdc[keyProperty];

            storage = new ArrayList();
            storageProxy = new ArrayList();
            groups = new Hashtable();
            totals = new List<object>();
        }

        private void SummaryCollect(List<object> dict, ICollection<ServerModeSummaryDescriptor> info, object obj) {
            if (info == null || dict == null) return;
            int index = 0;
            foreach (ServerModeSummaryDescriptor item in info) {
                decimal acc = 0;
                try {
                    acc = Convert.ToDecimal(dict[index]);
                } catch { }
                if (item.SummaryType == Aggregate.Count) {
                    acc++;
                } else {
                    decimal v = 0;
                    try {
                        ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), item.SummaryExpression);
                        v = Convert.ToDecimal(evaluator.Evaluate(obj));
                    } catch { }
                    switch (item.SummaryType) {
                        case Aggregate.Sum:
                            acc += v;
                            break;
                    }
                }
                dict[index] = acc;
                index++;
            }
        }

        private void SummarySetUp(List<object> dict, ICollection<ServerModeSummaryDescriptor> info) {
            if (info == null || dict == null) return;
            dict.Clear();
            foreach (ServerModeSummaryDescriptor item in info) {
                dict.Add(0);
            }
        }

        private void ProcessCollection() {
            ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), filter);
            storageProxy.Clear();
            SummarySetUp(totals, totalSummaryInfo);
            foreach (object o in storage) {
                if (evaluator.Fit(o)) {
                    storageProxy.Add(o);
                    SummaryCollect(totals, totalSummaryInfo, o);
                }
            }
            if (sortInfo != null) {
                storageProxy.Sort(new SimpleComparer(sortInfo, objectType));
            }
            groups.Clear();
        }

        #region IListServer Members

        object objectLock = new object();

        event EventHandler<ServerModeExceptionThrownEventArgs> _ExceptionThrown;
        event EventHandler<ServerModeExceptionThrownEventArgs> IListServer.ExceptionThrown {
            add { lock (objectLock) { _ExceptionThrown += value; } }
            remove { lock (objectLock) { _ExceptionThrown -= value; } }
        }

        event EventHandler<ServerModeInconsistencyDetectedEventArgs> _InconsistencyDetected;
        event EventHandler<ServerModeInconsistencyDetectedEventArgs> IListServer.InconsistencyDetected {
            add { lock (objectLock) { _InconsistencyDetected += value; } }
            remove { lock (objectLock) { _InconsistencyDetected -= value; } }
        }

        void IListServer.Apply(CriteriaOperator filterCriteria, ICollection<ServerModeOrderDescriptor> sortInfo, int groupCount, ICollection<ServerModeSummaryDescriptor> groupSummaryInfo, ICollection<ServerModeSummaryDescriptor> totalSummaryInfo) {
            this.filter = filterCriteria;
            List<ServerModeOrderDescriptor> sorts= new List<ServerModeOrderDescriptor>();
            if (sortInfo != null) sorts.AddRange(sortInfo);
            this.sortInfo = sorts;
            this.groupCount = groupCount;
            this.groupSummaryInfo = groupSummaryInfo;
            this.totalSummaryInfo = totalSummaryInfo;
            ProcessCollection();
        }

        void IListServer.Refresh() {
            throw new NotImplementedException();
        }

        int IListServer.FindIncremental(CriteriaOperator expression, string value, int startIndex, bool searchUp, bool ignoreStartRow, bool allowLoop){
            throw new NotImplementedException();
        }

        int IListServer.LocateByValue(CriteriaOperator expression, object value, int startIndex, bool searchUp) {
            throw new NotImplementedException();
        }

        IList IListServer.GetAllFilteredAndSortedRows() {
            throw new NotImplementedException();
        }

        List<ListSourceGroupInfo> IListServer.GetGroupInfo(ListSourceGroupInfo parentGroup) {
            List<ListSourceGroupInfo> rows = new List<ListSourceGroupInfo>();
            ArrayList uniqueValues = new ArrayList();
            int level = (parentGroup == null) ? 0 : (parentGroup.Level + 1);
            ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo[level].SortExpression);
            foreach (object o in storageProxy) {
                ListSourceGroupInfo group = parentGroup;
                while (group != null) {
                    System.Diagnostics.Debug.Assert((group.Level == 0) || groups[group] != null);
                    ExpressionEvaluator evaluator2 = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), sortInfo[group.Level].SortExpression);
                    object fv = evaluator2.Evaluate(o);
                    if (!Equals(fv, group.GroupValue)) goto Skip;
                    group = (ListSourceGroupInfo)groups[group];
                }
                object v = evaluator.Evaluate(o);
                ListSourceGroupInfo info;
                int index = uniqueValues.IndexOf(v);
                if (index < 0) {
                    uniqueValues.Add(v);
                    info = new SimpleListSourceGroupInfo();
                    info.GroupValue = v;
                    info.Level = level;
                    SummarySetUp(info.Summary, groupSummaryInfo);
                    rows.Add(info);
                    groups.Add(info, parentGroup);
                } else {
                    info = rows[index];
                }
                info.ChildDataRowCount++;
                SummaryCollect(info.Summary, groupSummaryInfo, o);
            Skip: ;
            }
            return rows;
        }

        int IListServer.GetRowIndexByKey(object key) {
            for (int i = 0; i < storageProxy.Count; i++) {
                if (Equals(pdKey.GetValue(storageProxy[i]), key))
                    return i;
            }
            return -1;
        }

        object IListServer.GetRowKey(int index) {
            return pdKey.GetValue(((IList)this)[index]);
        }

        List<object> IListServer.GetTotalSummary() {
            return totals;
        }

        object[] IListServer.GetUniqueColumnValues(CriteriaOperator valuesExpression, int maxCount, CriteriaOperator filterExpression, bool ignoreAppliedFilter) {
            ArrayList uniqueValues = new ArrayList();
            CriteriaOperator filterCriteria = ignoreAppliedFilter ? filterExpression : CriteriaOperator.And(filterExpression, this.filter);
            ExpressionEvaluator fitEvaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), filterCriteria);
            ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), valuesExpression);
            foreach (object o in storage) {
                if (!fitEvaluator.Fit(o)) continue;
                object v = evaluator.Evaluate(o);
                int index = uniqueValues.IndexOf(v);
                if (index < 0) {
                    uniqueValues.Add(v);
                    if (maxCount > 0 && uniqueValues.Count >= maxCount)
                        break;
                }
            }
            return uniqueValues.ToArray();
        }

        bool IListServer.PrefetchRows(ListSourceGroupInfo[] groupsToPrefetch, System.Threading.CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        int IListServer.LocateByExpression(CriteriaOperator expression, int startIndex, bool searchUp) {
            throw new NotImplementedException();
        }

        #endregion

        #region IList Members

        int IList.Add(object value) {
            throw new NotImplementedException();
        }

        void IList.Clear() {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value) {
            throw new NotImplementedException();
        }

        int IList.IndexOf(object value) {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value) {
            throw new NotImplementedException();
        }

        bool IList.IsFixedSize {
            get { throw new NotImplementedException(); }
        }

        bool IList.IsReadOnly {
            get { return true; }
        }

        void IList.Remove(object value) {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index) {
            throw new NotImplementedException();
        }

        object IList.this[int index] {
            get {
                return storageProxy[index];
            }
            set {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }

        int ICollection.Count {
            get { return storageProxy.Count; }
        }

        bool ICollection.IsSynchronized {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return storageProxy.GetEnumerator();
        }

        #endregion

    }

    class SimpleListSourceGroupInfo : ListSourceGroupInfo
    {
        private List<object> summary;
        public override List<object> Summary
        {
            get { return summary; }
        }
        public SimpleListSourceGroupInfo()
        {
            summary = new List<object>();
        }
    }

    class SimpleComparer : IComparer
    {
        ICollection<ServerModeOrderDescriptor> sortInfo;
        Type objectType;

        public SimpleComparer(ICollection<ServerModeOrderDescriptor> sortInfo, Type objectType)
        {
            this.sortInfo = sortInfo;
            this.objectType = objectType;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            foreach (ServerModeOrderDescriptor info in sortInfo)
            {
                ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), info.SortExpression);
                object xx = evaluator.Evaluate(x);
                object yy = evaluator.Evaluate(y);
                int sign = Comparer.Default.Compare(xx, yy);
                if (sign != 0)
                    return (info.IsDesc) ? -sign : sign;
            }
            return 0;
        }

        #endregion
    }
}

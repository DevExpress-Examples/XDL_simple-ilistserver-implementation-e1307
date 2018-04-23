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
        Dictionary<object, object> totals;

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
            totals = new Dictionary<object, object>();
        }

        private void SummaryCollect(Dictionary<object, object> dict, ICollection<ServerModeSummaryDescriptor> info, object obj) {
            if (info == null || dict == null) return;
            foreach (ServerModeSummaryDescriptor item in info) {
                decimal acc = 0;
                try {
                    acc = Convert.ToDecimal(dict[item.SummaryKey]);
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
                dict[item.SummaryKey] = acc;
            }
        }

        private void SummarySetUp(Dictionary<object, object> dict, ICollection<ServerModeSummaryDescriptor> info) {
            if (info == null || dict == null) return;
            dict.Clear();
            foreach (ServerModeSummaryDescriptor item in info) {
                dict.Add(item.SummaryKey, 0);
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

        public event EventHandler<ServerModeExceptionThrownEventArgs> ExceptionThrown;
        public event EventHandler<ServerModeInconsistencyDetectedEventArgs> InconsistencyDetected;

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
            throw new Exception("The method or operation is not implemented.");
        }

        int IListServer.FindIncremental(CriteriaOperator expression, string value, int startIndex, bool searchUp, bool ignoreStartRow, bool allowLoop){
            throw new Exception("The method or operation is not implemented.");
        }

        int IListServer.LocateByValue(CriteriaOperator expression, object value, int startIndex, bool searchUp) {
            throw new Exception("The method or operation is not implemented.");
        }

        IList IListServer.GetAllFilteredAndSortedRows() {
            throw new Exception("The method or operation is not implemented.");
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
            return pdKey.GetValue(this[index]);
        }

        Dictionary<object, object> IListServer.GetTotalSummary() {
            return totals;
        }

        object[] IListServer.GetUniqueColumnValues(CriteriaOperator expression, int maxCount, bool includeFilteredOut){
            ArrayList uniqueValues = new ArrayList();
            IList list = includeFilteredOut ? storage : storageProxy;
            ExpressionEvaluator evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(objectType), expression);
            foreach (object o in list) {
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

        #endregion

        #region IList Members

        public int Add(object value) {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Clear() {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(object value) {
            throw new Exception("The method or operation is not implemented.");
        }

        public int IndexOf(object value) {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Insert(int index, object value) {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsFixedSize {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsReadOnly {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void Remove(object value) {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveAt(int index) {
            throw new Exception("The method or operation is not implemented.");
        }

        public object this[int index] {
            get {
                return storageProxy[index];
            }
            set {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index) {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count {
            get { return storageProxy.Count; }
        }

        public bool IsSynchronized {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public object SyncRoot {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return storageProxy.GetEnumerator();
        }

        #endregion

    }

    class SimpleListSourceGroupInfo : ListSourceGroupInfo
    {
        private Dictionary<object, object> summary;
        public override Dictionary<object, object> Summary
        {
            get { return summary; }
        }
        public SimpleListSourceGroupInfo()
        {
            summary = new Dictionary<object, object>();
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

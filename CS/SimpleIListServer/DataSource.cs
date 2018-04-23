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
    public class SimpleServerModeDataSource: IListServer
    {
        Type objectType;
        PropertyDescriptor pdKey;

        ArrayList storage; //hoax
        ArrayList storageProxy;
        Hashtable groups;
        Dictionary<object, object> totals;

        CriteriaOperator filter;
        ListSortDescriptionCollection sortInfo;
        int groupCount;
        List<ListSourceSummaryItem> summaryInfo;
        List<ListSourceSummaryItem> totalSummaryInfo;


        public SimpleServerModeDataSource(Type objectType, string keyProperty, ICollection data)
            :this(objectType, keyProperty)
        {
            storage.AddRange(data);
            ProcessCollection();
        }

        public SimpleServerModeDataSource(Type objectType, string keyProperty)
        {
            this.objectType = objectType;
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(objectType);
            pdKey = pdc[keyProperty];

            storage = new ArrayList();
            storageProxy = new ArrayList();
            groups = new Hashtable();
            totals = new Dictionary<object, object>();
        }

        private void SummaryCollect(Dictionary<object, object> dict, List<ListSourceSummaryItem> info, object obj)
        {
            if (info == null || dict == null) return;
            foreach (ListSourceSummaryItem item in info)
            {
                decimal acc = 0;
                try
                {
                    acc = Convert.ToDecimal(dict[item.Key]);
                }
                catch { }
                if (item.SummaryType == SummaryItemType.Count)
                {
                    acc++;
                }
                else
                {
                    decimal v = 0;
                    try
                    {
                        v = Convert.ToDecimal(item.Descriptor.GetValue(obj));
                    }
                    catch { }
                    switch (item.SummaryType)
                    {
                        case SummaryItemType.Sum:
                            acc += v;
                            break;
                    }
                }
                dict[item.Key] = acc;
            }
        }

        private void SummarySetUp(Dictionary<object, object> dict, List<ListSourceSummaryItem> info)
        {
            if (info == null || dict == null) return;
            dict.Clear();
            foreach (ListSourceSummaryItem item in info)
            {
                dict.Add(item.Key, 0);
            }
        }

        private void ProcessCollection()
        {
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(objectType);
            ExpressionEvaluator evaluator = new ExpressionEvaluator(pdc, filter);
            storageProxy.Clear();
            SummarySetUp(totals, totalSummaryInfo);
            foreach (object o in storage)
            {
                if (evaluator.Fit(o))
                {
                    storageProxy.Add(o);
                    SummaryCollect(totals, totalSummaryInfo, o);
                }
            }
            if (sortInfo != null)
            {
                storageProxy.Sort(new SimpleComparer(sortInfo));
            }
            groups.Clear();
        }

        #region IListServer Members

        public void ApplySort(ListSortDescriptionCollection sortInfo, int groupCount, List<ListSourceSummaryItem> summaryInfo, List<ListSourceSummaryItem> totalSummaryInfo)
        {
            this.sortInfo = sortInfo;
            this.groupCount = groupCount;
            this.summaryInfo = summaryInfo;
            this.totalSummaryInfo = totalSummaryInfo;
            ProcessCollection();
        }

        public CriteriaOperator FilterCriteria
        {
            get
            {
                return filter;
            }
            set
            {
                if (Equals(filter, value)) return;
                filter = value;
                ProcessCollection();
            }
        }

        public int FindIncremental(PropertyDescriptor column, string value, int startIndex, bool searchUp)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object FindKeyByBeginWith(PropertyDescriptor column, string value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object FindKeyByValue(PropertyDescriptor column, object value)
        {
            foreach (object o in storageProxy)
            {
                if (Equals(column.GetValue(o), value))
                    return pdKey.GetValue(o);
            }
            return null;
        }

        public List<ListSourceGroupInfo> GetGroupInfo(ListSourceGroupInfo parentGroup)
        {
            List<ListSourceGroupInfo> rows = new List<ListSourceGroupInfo>();
            ArrayList uniqueValues = new ArrayList();
            int level = (parentGroup == null) ? 0 : (parentGroup.Level + 1);
            PropertyDescriptor pd = sortInfo[level].PropertyDescriptor;
            foreach (object o in storageProxy)
            {
                ListSourceGroupInfo group = parentGroup;
                while (group != null)
                {
                    System.Diagnostics.Debug.Assert((group.Level == 0) || groups[group] != null);
                    object fv = sortInfo[group.Level].PropertyDescriptor.GetValue(o);
                    if (!Equals(fv, group.GroupValue)) goto Skip;
                    group = (ListSourceGroupInfo)groups[group];
                }
                object v = pd.GetValue(o);
                ListSourceGroupInfo info;
                int index = uniqueValues.IndexOf(v);
                if (index < 0)
                {
                    uniqueValues.Add(v);
                    info = new SimpleListSourceGroupInfo();
                    info.GroupValue = v;
                    info.Level = level;
                    SummarySetUp(info.Summary, summaryInfo);
                    rows.Add(info);
                    groups.Add(info, parentGroup);
                }
                else
                {
                    info = rows[index];
                }
                info.ChildDataRowCount++;
                SummaryCollect(info.Summary, summaryInfo, o);
                Skip:;
            }
            return rows;
        }

        public int GetRowIndexByKey(object key)
        {
            for (int i = 0; i < storageProxy.Count; i++)
            {
                if (Equals(pdKey.GetValue(storageProxy[i]), key))
                    return i;
            }
            return -1;
        }

        public object GetRowKey(int index)
        {
            return pdKey.GetValue(this[index]);
        }

        public Dictionary<object, object> GetTotalSummary()
        {
            return totals;
        }

        public object[] GetUniqueColumnValues(PropertyDescriptor descriptor, int maxCount, bool includeFilteredOut, bool roundDataTime)
        {
            ArrayList uniqueValues = new ArrayList();
            IList list = includeFilteredOut ? storage : storageProxy;
            foreach (object o in list)
            {
                object v = descriptor.GetValue(o);
                int index = uniqueValues.IndexOf(v);
                if (index < 0)
                {
                    uniqueValues.Add(v);
                    if (maxCount > 0 && uniqueValues.Count >= maxCount)
                        break;
                }
            }
            return uniqueValues.ToArray();
        }

        #endregion

        #region IList Members

        public int Add(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Clear()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int IndexOf(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Insert(int index, object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsFixedSize
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsReadOnly
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void Remove(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveAt(int index)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object this[int index]
        {
            get
            {
                return storageProxy[index];
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count
        {
            get { return storageProxy.Count; }
        }

        public bool IsSynchronized
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
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
        ListSortDescriptionCollection sortInfo;

        public SimpleComparer(ListSortDescriptionCollection sortInfo)
        {
            this.sortInfo = sortInfo;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            foreach (ListSortDescription info in sortInfo)
            {
                object xx = info.PropertyDescriptor.GetValue(x);
                object yy = info.PropertyDescriptor.GetValue(y);
                int sign = Comparer.Default.Compare(xx, yy);
                if (sign != 0)
                    return (info.SortDirection == ListSortDirection.Ascending) ? sign : -sign;
            }
            return 0;
        }

        #endregion
    }
}

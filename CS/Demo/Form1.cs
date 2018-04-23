using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.Sample;

namespace DevExpress.Sample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            List<Record> list = new List<Record>();
            Random rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                //Record r = new Record(i,
                //    "abc".Substring(i % 3, 1),
                //    "vwxyz".Substring((i * i) % 5, 1),
                //    "vwxyz".Substring((i * i) % 3 + 2, 1),
                //    "vwxyz".Substring((i * i * i) % 5, 1)
                //    );
                Record r = new Record(i,
                    "abc".Substring(rnd.Next(3), 1),
                    "vwxyz".Substring(rnd.Next(5), 1),
                    "vwxyz".Substring(rnd.Next(3) + 2, 1),
                    "vwxyz".Substring(rnd.Next(2) + 2, 1)
                    );

                list.Add(r);
            }

            SimpleServerModeDataSource ds = new SimpleServerModeDataSource(typeof(Record), "ID", list);

            gridView1.OptionsView.ShowGroupedColumns = true;
            gridView1.GroupFooterShowMode = DevExpress.XtraGrid.Views.Grid.GroupFooterShowMode.VisibleAlways;

            gridControl1.ServerMode = true;
            gridControl1.DataSource = ds;
        }
    }

    public class Record
    {
        private int id;
        private string g;
        private string u;
        private string v;
        private string w;

        public int ID { get { return id; } set { id = value; } }
        public string G { get { return g; } set { g = value; } }
        public string U { get { return u; } set { u = value; } }
        public string V { get { return v; } set { v = value; } }
        public string W { get { return w; } set { w = value; } }

        public Record(int id, string g, string u, string v, string w)
        {
            this.id = id;
            this.g = g;
            this.u = u;
            this.v = v;
            this.w = w;
        }
    }
}
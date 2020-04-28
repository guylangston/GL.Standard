using System;
using System.Collections.Generic;

namespace TextRenderZ.Reporting.Adapters
{
    public class TableReportModel<TCol, TRow>
    {
        public List<Col> Columns { get;  } = new List<Col>();
        public List<Row> Rows { get;  } = new List<Row>();
        
        public class Col
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public TCol Value { get; set; }
        }
        
        public class Row
        {
            public string Name  { get; set; }
            public int    Index { get; set; }
            public TRow Value { get; set; }
        }

        public void AddCols(IEnumerable<TCol> cols, Func<TCol, string> getName)
        {
            foreach (var col in cols)
            {
                var cc = new Col()
                {
                    Index = Columns.Count,
                    Name  = getName(col),
                    Value = col
                };
                Columns.Add(cc);
            }
            
        }
        
        
        public void AddRows(IEnumerable<TRow> rows, Func<TRow, string> getName)
        {
            foreach (var row in rows)
            {
                var cc = new Row()
                {
                    Index = Rows.Count,
                    Name  = getName(row),
                    Value = row
                };
                Rows.Add(cc);
            }
        }
        
    }
}
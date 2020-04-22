using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TextRenderZ.Reporting
{
    public static class MapToReporting
    {
        public static MapToReporting<T> Create<T>(IEnumerable<T> _) => new MapToReporting<T>();
        public static MapToReporting<T>  Create<T>() => new MapToReporting<T>();
        
        public static MapToReporting<T> Create<T>(IMapToReportingCellAdapter adapter) => new MapToReporting<T>(adapter);
        public static MapToReporting<T> Create<T>(IMapToReportingCellAdapter adapter, IEnumerable<T> _) => new MapToReporting<T>(adapter);
    }

    public interface IMapToReportingCellAdapter
    {
        void Enrich(ColumnInfo col);
        void Enrich(Cell cell);
        Cell ConvertToCell(ColumnInfo col, object value, object container);
        Cell ConvertToCell(ColumnInfo col, Exception error, object container);
    }
    
    
    public class ColumnInfoFunc : ColumnInfo
    {
        private Func<object?, object?> func;

        public ColumnInfoFunc(Type targetType, Type containerType, string title, Func<object?, object?> getValue) 
            : base(targetType, containerType, title)
        {
                
            this.func = getValue;
        }

        public override object GetCellValue(object container) => func(container);
    }
    
    
    public class ColumnInfoPropertyInfo : ColumnInfo
    {
        public ColumnInfoPropertyInfo(PropertyInfo info, Type containerType, string title) 
            : base(info.PropertyType, containerType, title)
        {
            PropertyInfo = info; 
        }
            
            
        public PropertyInfo PropertyInfo { get; }
            
        public override object GetCellValue(object container) => PropertyInfo.GetValue(container);
    }
    

    public class MapToReporting<T> : IMapToReporting<T>
    {
        private readonly List<ColumnInfo> columns = new List<ColumnInfo>();
        private PropertyInfo[] props;
        
        public MapToReporting(IMapToReportingCellAdapter cellAdapter)
        {
            this.props  = typeof(T).GetProperties();
            CellAdapter = cellAdapter;
        }

        public MapToReporting() : this(new MapToReportingCellAdapter()) { }

        

        public IMapToReportingCellAdapter CellAdapter { get; set; }

        public MapToReporting<T> AddColumn<TP>(Expression<Func<T, TP>> exp)
        {
            throw new NotImplementedException();
        }

        public MapToReporting<T> AddColumn<TP>(string title, Func<T, TP> getVal, Action<ColumnInfo>? setupCol = null)
        {
#pragma warning disable 8605
            var columnInfoFunc = new ColumnInfoFunc(typeof(TP), typeof(T), title, o => (object?)getVal((T) o));
#pragma warning restore 8605
            if (setupCol != null) setupCol(columnInfoFunc);
            columns.Add(columnInfoFunc);

            
            return this;
        }
        
        
        public MapToReporting<T> AddColumn(string? title, PropertyInfo info, Action<ColumnInfo> setupCol = null)
        {
            var columnInfoPropertyInfo = new ColumnInfoPropertyInfo(info, typeof(T), title ?? info.Name);
            columns.Add(columnInfoPropertyInfo);
            if (setupCol != null) setupCol(columnInfoPropertyInfo);
            return this;
        }
        public MapToReporting<T> AddColumn(string propName) => AddColumn(null, props.First(x => x.Name == propName), null);
        
       
        


        class MapToRow : IMapToRow<T>
        {
            private readonly MapToReporting<T> owner;
            private readonly T item;

            public MapToRow(MapToReporting<T> owner, T item)
            {
                this.owner = owner;
                this.item = item;
            }

            public IEnumerator<Cell> GetEnumerator()
            {
                foreach (var col in owner.columns)
                {
                    var c = GetCell(col, item);
                    yield return c;
                }
            }

            private Cell GetCell(ColumnInfo col, T data)
            {
                try
                {
                    var obj = col.GetCellValue(data);
                    var c =  owner.CellAdapter.ConvertToCell(col, obj, data);
                    if (col.Adapters != null) 
                    {
                        foreach (var adapter in col.Adapters)
                        {
                            adapter.Adapt(c);
                        }    
                    }

                    return c;
                }
                catch (Exception e)
                {
                    return owner.CellAdapter.ConvertToCell(col, e, data);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }


        public IReadOnlyList<ColumnInfo> Columns => columns;
        public IEnumerable<IMapToRow<T>> GetRows(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return new MapToRow(this, item);
            }
        }

        public IMapToReporting<T> RenderTo(IEnumerable<T> items, IMapToReportingRenderer renderer, TextWriter outp )
        {
            renderer.Render(this, items, outp);

            return this;
        }

       
        public IMapToReporting<T> RenderTo(IEnumerable<T> items, IMapToReportingRenderer renderer, StringBuilder sb)
        {
            using var sw = new StringWriter(sb);
            RenderTo(items, renderer, sw);
            return this;
        }

        public void CodeGen(TextWriter output)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                output.WriteLine($"\t.AddColumn(\"{prop.Name}\", x=>x.{prop.Name})");
            }
        }
    }
}
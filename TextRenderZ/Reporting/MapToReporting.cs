using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TextRenderZ.Reporting.Adapters;

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
        
        public static ColumnInfoFunc Create<T, TP>(string title, Func<T, TP> getValue) 
            => new ColumnInfoFunc(typeof(T), typeof(TP), title, x => (object)getValue((T)x));

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
    
    public class FluentColumn<T>
    {
        public FluentColumn(ColumnInfo columnInfo)
        {
            ColumnInfo = columnInfo;
        }

        public ColumnInfo ColumnInfo { get; set; }

        public FluentColumn<T> Add(ICellAdapter adapter)
        {
            ColumnInfo.Add(adapter);
            return this;
        }
        
        public FluentColumn<T> Link(CellLink<T> link)
        {
            ColumnInfo.Add(link);
            return this;
        }
        
        public FluentColumn<T> Link(Func<Cell, T, string> getUrl) => Link(new CellLink<T>(getUrl));
    }
    

    public class MapToReporting<T> : IMapToReporting<T>
    {
        private readonly List<ColumnInfo> columns = new List<ColumnInfo>();
        private PropertyInfo[]? _props;
        
        private PropertyInfo[] props => _props ??= typeof(T).GetProperties();
        
        
        public MapToReporting(IMapToReportingCellAdapter cellAdapter)
        {
            CellAdapter = cellAdapter;
        }

        public MapToReporting() : this(new MapToReportingCellAdapter()) { }

        

        public IMapToReportingCellAdapter CellAdapter { get; set; }
        
        public MapToReporting<T> AddColumn(ColumnInfo manual)
        {
            columns.Add(manual);
            return this;
        }
        public MapToReporting<T> AddColumns(IEnumerable<ColumnInfo> cols)
        {
            columns.AddRange(cols);
            return this;
        }
        
        public MapToReporting<T> AddColumns()
        {
            foreach (var propertyInfo in props)
            {
                AddColumn(StringUtil.UnCamel(propertyInfo.Name), propertyInfo);
            }
            return this;
        }

        public MapToReporting<T> AddColumn<TP>(Expression<Func<T, TP>> exp)
        {
            throw new NotImplementedException();
        }

        public MapToReporting<T> AddColumn<TP>(string title, Func<T, TP> getVal, Action<FluentColumn<T>>? setupCol = null)
        {
#pragma warning disable 8605
            var columnInfoFunc = new ColumnInfoFunc(typeof(TP), typeof(T), title, o => (object?)getVal((T) o));
#pragma warning restore 8605
            if (setupCol != null) setupCol(new FluentColumn<T>(columnInfoFunc));
            columns.Add(columnInfoFunc);

            
            return this;
        }
        
        
        public MapToReporting<T> AddColumn(string? title, PropertyInfo info, Action<FluentColumn<T>>? setupCol = null)
        {
            var columnInfoPropertyInfo = new ColumnInfoPropertyInfo(info, typeof(T), title ?? info.Name);
            columns.Add(columnInfoPropertyInfo);
            if (setupCol != null) setupCol(new FluentColumn<T>(columnInfoPropertyInfo));
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
        
        public IMapToReporting<T> RenderTo(T item, IMapToReportingRendererSingle renderer, TextWriter outp )
        {
            renderer.Render(this, item, outp);
            return this;
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

        public void CodeGen(TextWriter output, bool wrapHtml = true)
        {
            if (wrapHtml) output.WriteLine("<pre class='code-cs'><code>");
            foreach (var prop in typeof(T).GetProperties())
            {
                output.WriteLine($"\t.AddColumn(\"{prop.Name}\", x=>x.{prop.Name})");
            }
            if (wrapHtml) output.WriteLine("</code></pre>");
        }
    }
}
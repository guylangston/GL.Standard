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
        public static MapToReporting<T>  Create<T>() => new MapToReporting<T>();
    }

    public interface IMapToReportingCellAdapter
    {
        Cell ConvertToCell(ColumnInfo col, object value);
        Cell ConvertToCell(ColumnInfo col, Exception error);
    }

    public class MapToReporting<T> : IMapToReporting<T>
    {
        private readonly List<ColumnInfo> columns = new List<ColumnInfo>();
        private PropertyInfo[] props;

        public MapToReporting()
        {
            this.props = typeof(T).GetProperties();
        }

        public IMapToReportingCellAdapter CellAdapter { get; set; }

        public MapToReporting<T> AddColumn<TP>(Expression<Func<T, TP>> exp)
        {
            throw new NotImplementedException();
        }

        public MapToReporting<T> AddColumn<TP>(string title, Func<T, TP> getVal)
        {
#pragma warning disable 8605
            columns.Add(new ColumnInfoFunc(typeof(TP), typeof(T), title, o => (object?)getVal((T) o)));
#pragma warning restore 8605
            return this;
        }
        
        public MapToReporting<T> AddColumn(string propName) => AddColumn(props.First(x => x.Name == propName));
        public MapToReporting<T> AddColumn(PropertyInfo info)
        {
            columns.Add(new ColumnInfoPropertyInfo(info, typeof(T), info.Name));
            return this;
        }
        
        class ColumnInfoFunc : ColumnInfo
        {
            private Func<object?, object?> func;

            public ColumnInfoFunc(Type targetType, Type containerType, string title, Func<object?, object?> getValue) 
                : base(targetType, containerType, title)
            {
                
                this.func = getValue;
            }

            public override object GetCellValue(object container)
            {
                return func(container);
            }

            
        }
        

        class ColumnInfoPropertyInfo : ColumnInfo
        {
            public ColumnInfoPropertyInfo(PropertyInfo info, Type containerType, string title) 
                : base(info.PropertyType, containerType, title)
            {
                PropertyInfo = info; 
            }
            
            
            public PropertyInfo PropertyInfo { get; }
            
            public override object GetCellValue(object container)
            {
                return PropertyInfo.GetValue(container);
            }
            
        }

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
                    return owner.CellAdapter.ConvertToCell(col, obj);
                }
                catch (Exception e)
                {
                    return owner.CellAdapter.ConvertToCell(col, e);
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TextRenderZ.Reporting
{
    public interface ICellAdapter
    {
        void Adapt(Cell cell);
    }

    public class CellAdapter : ICellAdapter
    {
        private readonly Action<Cell> func;

        public CellAdapter(Action<Cell> func)
        {
            this.func = func;
        }

        public void Adapt(Cell cell) => func(cell);
    }
    
    public enum TextAlign { Left, Center, Right }
    public enum NumberStyle { None, Number, Percentage, PercentageMul100, Currency }
    
    public abstract class ColumnInfo
    {
        public ColumnInfo(Type targetType, Type containerType, string title)
        {
            TargetType = targetType;
            ContainerType = containerType;
            Title = title;
        }

        public Type TargetType    { get;  }
        public Type ContainerType { get; }
        
        public string Title { get; }
        public string? Description { get; set; }
        public TextAlign TextAlign { get; set; }
        public NumberStyle IsNumber { get; set; }
        public string Prefix { get; set; } // May be overridden per cell
        public string Suffix { get; set; } // May be overridden per cell
        
        public IReadOnlyDictionary<string, string> Attributes { get; set; }
        
        public List<ICellAdapter> Adapters { get; set; }

        public ColumnInfo Add(ICellAdapter adapter)
        {
            Adapters ??= new List<ICellAdapter>();
            Adapters.Add(adapter);
            return this;
        }

        public abstract object GetCellValue(object? container);

        public ColumnInfo AsPercentage()
        {
            Suffix = " %";
            IsNumber = NumberStyle.Percentage;
            return this;
        }
    }

    public class CellInfo
    {
        public string Id    { get; set; }
        public string Class { get; set; }
        
        public NumberStyle IsNumber { get; set; }
        public bool IsErr { get; set; }    // Number NaN, etc (not exception info)
        public bool IsNeg { get; set; } // Number NaN, etc (not exception info)
        public string Prefix { get; set; } // May be overridden per cell
        public string Suffix { get; set; } // May be overridden per cell
        public string ToolTip { get; set; }
        
        public IReadOnlyDictionary<string, string> Attributes { get; set; }
    }
    
    public sealed class Cell
    {
        public Cell(ColumnInfo colInfo,  CellInfo? cellInfo, object? valueInput)
        {
            Column = colInfo;
            CellInfo = cellInfo;
            ValueInput = valueInput;
        }

        public ColumnInfo Column { get;  }
        public CellInfo? CellInfo { get; set; }

        public object? ValueInput { get; set; }
        public object? ValueDisplay { get; set; }
        
        public Exception? Error { get; set; }
        public bool IsNull { get; set; }

        public object? GetValue() => ValueDisplay ?? ValueInput;  
        public string? GetValueString() => GetValue()?.ToString();
        public override string ToString() => GetValueString();
    }
    
    /// <summary>
    /// For ease the nomenclature of Column/Row/Cell; think of a single Item as always wrapped into Cell[1] array
    /// </summary>
    public interface IMapToReporting<T> 
    {
        public IReadOnlyList<ColumnInfo> Columns { get;  }
        public IEnumerable<IMapToRow<T>> GetRows(IEnumerable<T> items);
        public IMapToReporting<T> RenderTo(IEnumerable<T> items, IMapToReportingRenderer renderer, TextWriter outp);
        public IMapToReporting<T> RenderTo(IEnumerable<T> items, IMapToReportingRenderer renderer, StringBuilder sb);
    }

    public interface IMapToReportingRenderer
    {
        void Render<T>(IMapToReporting<T> mapping, IEnumerable<T> items, TextWriter outp);
    }
    
    public interface IMapToRow<T> : IEnumerable<Cell>
    {
        
    }
    
    
}

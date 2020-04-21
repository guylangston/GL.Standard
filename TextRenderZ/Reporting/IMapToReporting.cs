using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TextRenderZ.Reporting
{
    public enum TextAlign { Left, Center, Right }
    
    public class CellStyle
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TextAlign TextAlign { get; set; }
        
    }
    
    public class Cell
    {
        public object? Value { get; set; }
        public object? ValueDisplay { get; set; }        // Check for ICellValue for more complex rules
        public CellStyle? Style { get; set; }
        public Exception? Error { get; set; }

        public object? GetValue() => ValueDisplay ?? Value;  
        public string? GetValueString() => GetValue()?.ToString();
    }

    public interface ICellValue
    {
        public Cell Cell { get;  }
    }

    public class SuffixPrefixCell : ICellValue
    {
        public SuffixPrefixCell(string prefix, string suffix, Cell cell)
        {
            Prefix = prefix;
            Suffix = suffix;
            Cell = cell;
        }

        public string? Prefix { get;  }
        public string? Suffix { get;  }
        public Cell Cell { get;  }
    }
    
    /// <summary>
    /// For ease the nomenclature of Column/Row/Cell; think of a single Item as always wrapped into Cell[1] array
    /// </summary>
    public interface IMapToReporting<T> 
    {
        public IReadOnlyList<CellStyle> Columns { get;  }
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

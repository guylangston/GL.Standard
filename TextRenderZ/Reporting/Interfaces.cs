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
    
    public enum TextAlign { None, Left, Center, Right }
    public enum NumberStyle { None, Number, Percentage, PercentageMul100, Currency }

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

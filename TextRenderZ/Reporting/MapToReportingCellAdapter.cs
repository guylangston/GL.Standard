using System;

namespace TextRenderZ.Reporting
{
    public class MapToReportingCellAdapter : IMapToReportingCellAdapter
    {

        public virtual void Enrich(ColumnInfo col)
        {
            if (col.TargetType == typeof(decimal))
            {
                col.TextAlign = TextAlign.Right;
                col.IsNumber = NumberStyle.Number;
                return;
            }
            if (col.TargetType == typeof(double))
            {
                col.TextAlign = TextAlign.Right;
                col.IsNumber  = NumberStyle.Number;
                return;
            }
        }
        
        public virtual Cell ConvertToCell(ColumnInfo col, object value, object container)
        {
            if (value is Cell c) return c; // Passthrough 
            var cell = new Cell(col, null, value, container);
            cell.IsNull = value == null;
            Enrich(cell);

            return cell;
        }

        public virtual void Enrich(Cell cell)
        {
            if (cell.ValueInput is double valueDouble)
            {
                cell.IsNull = double.IsNaN(valueDouble);
                
                cell.CellInfo ??= new CellInfo();
                cell.CellInfo.IsErr       = double.MaxValue.Equals(valueDouble);
                cell.CellInfo.IsNeg = valueDouble < 0;
                
                cell.ValueDisplay = AddPrefixSuffixNull(cell, valueDouble.ToString("#,##0.00"));
                return;
            }
            
            if (cell.ValueInput is int valueInt)
            {
                cell.IsNull = valueInt == Decimal.MinValue;
                
                cell.CellInfo       ??= new CellInfo();
                cell.CellInfo.IsErr = valueInt == Decimal.MaxValue;
                cell.CellInfo.IsNeg = valueInt < 0;
                
                cell.ValueDisplay = AddPrefixSuffixNull(cell, valueInt.ToString("#,##0"));
                return;
            }

            if (cell.ValueInput is DateTime valueDate)
            {
                cell.IsNull = valueDate == DateTime.MinValue;
                
                cell.CellInfo       ??= new CellInfo();
                cell.CellInfo.IsErr = valueDate == DateTime.MaxValue;
                
                
                cell.ValueDisplay = AddPrefixSuffixNull(cell, valueDate.ToString("yyyy-MM-dd"));
                return;
            }

            cell.ValueDisplay = cell.ValueInput?.ToString();
        }

        protected virtual string AddPrefixSuffixNull(Cell cell, string toString)
        {
            return (cell.CellInfo?.Prefix ?? cell.Column.Prefix ?? "")
                   + (toString ?? "")
                   + (cell.CellInfo?.Suffix ?? cell.Column.Suffix ?? "");
        }

        public Cell ConvertToCell(ColumnInfo col, Exception error, object container)
        {
            var cell = new Cell(col, null, null, container);
            cell.Error = error;
            cell.IsNull = true;
            return cell;
        }
    }
}
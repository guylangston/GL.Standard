using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace TextRenderZ.Reporting
{
    public class CellContainerTag
    {
        public CellContainerTag()
        {
        }

        public CellContainerTag(string tagName, string id, string @class)
        {
            TagName = tagName;
            Id = id;
            Class = @class;
        }

        public string TagName { get; set; }
        public string Id    { get; set; }
        public string Class { get; set; }
    }
    
    public interface ICellFormatter
    {
        void WriteCell<T>(TextWriter tw, T input, CellContainerTag cell);
    }

    public class CellFormatter : ICellFormatter
    {
        
        public string NullToken { get; set; } = "~";
        public string StringFormatNumber = "{0:#,##0.0000}";

        public virtual string GetTitle<T>(T item, CellContainerTag cell)
        {
            return item.ToString();
        }
        
        public void WriteCell<T>(TextWriter tw, T input, CellContainerTag cell)
        {
            var itemType = typeof(T) == typeof(object) ? input?.GetType() : typeof(T);
            
            object data = input;
            if (input is Cell itemCell)
            {
                itemType = itemCell.ValueInput?.GetType() ?? itemType;
                data = itemCell.ValueDisplay;
            }

            bool isNum = IsNumberType(itemType);
            bool isNull = IsNull(data);
            
            tw.Write($"<{cell.TagName}");
            if (cell.Id != null)
            {
                tw.Write($" id='{cell.Id}'");
            }
            
            tw.Write($" class='{cell.Class}");
            if (isNull) tw.Write(" null");
            if (isNum) tw.Write(" num");
            if (isNum && IsNumberNegative(data)) tw.Write(" num-neg");
            tw.Write("'");

            var title = GetTitle(input, cell);
            if (title != null) tw.Write($" title='{title}'");
            
            tw.Write(">");

            
            if (isNull)
            {
                tw.Write(NullToken);
            }
            else if (isNum)
            {
                tw.Write(string.Format(StringFormatNumber, data));
            }
            else
            {
                tw.Write(data?.ToString());
            }
            tw.WriteLine($"</{cell.TagName}>");
        }
        
        private bool IsNull(object item)
        {
            if (item == null) return true;
            if (item is double dd && double.IsNaN(dd)) return true;
            if (item is decimal dc && decimal.MinValue == dc) return true;
            return false;
        }

        private bool IsNumberNegative(object item)
        {
            if (item is double dd && dd < 0) return true;  
            if (item is int ii && ii < 0) return true;
            if (item is decimal dc && dc < 0) return true;
            return false;
        }

        private bool IsNumberType(Type type) => 
            type == typeof(double) 
            || type == typeof(int) 
            || type == typeof(decimal);
    }
}
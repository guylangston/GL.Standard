using System;
using System.IO;

namespace TextRenderZ.Reporting
{
      public class CellContainer
    {
        public CellContainer()
        {
        }

        public CellContainer(string tagName, string id, string @class)
        {
            TagName = tagName;
            Id = id;
            Class = @class;
        }

        public string TagName { get; set; }
        public string Id { get; set; }
        public string Class { get; set; }
    }
    
    public interface ICellFormatter
    {
        void WriteCell<T>(TextWriter tw, T item, CellContainer cell);
    }

    public class CellFormatter : ICellFormatter
    {
        
        public string NullToken { get; set; } = "~";
        public string StringFormatNumber = "{0:#,##0.0000}";

        public virtual string GetTitle<T>(T item, CellContainer cell)
        {
            return item.ToString();
        }
        
        public void WriteCell<T>(TextWriter tw, T item, CellContainer cell)
        {
            var itemType = typeof(T) == typeof(object) ? item?.GetType() : typeof(T);
            bool isNum = IsNumberType(itemType);
            bool isNull = IsNull(item);
            
            tw.Write($"<{cell.TagName}");
            if (cell.Id != null)
            {
                tw.Write($" id='{cell.Id}'");
            }
            
            tw.Write($" class='{cell.Class}");
            if (isNull) tw.Write(" null");
            if (isNum) tw.Write(" num");
            if (isNum && IsNumberNegative(item)) tw.Write(" num-neg");
            tw.Write("'");

            var title = GetTitle(item, cell);
            if (title != null) tw.Write($" title='{title}'");
            
            tw.Write(">");

            if (isNull)
            {
                tw.Write(NullToken);
            }
            else if (isNum)
            {
                tw.Write(string.Format(StringFormatNumber, item));
            }
            else
            {
                tw.Write(item?.ToString());
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
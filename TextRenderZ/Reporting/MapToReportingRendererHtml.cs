using System.Collections.Generic;
using System.IO;

namespace TextRenderZ.Reporting
{
    public class MapToReportingRendererHtml : IMapToReportingRenderer
    {
        public string TableClass { get; set; } = "table table-sm";
        public ICellFormatter CellFormatter { get; set; }
        
        public void Render<T>(IMapToReporting<T> mapping, IEnumerable<T> items, TextWriter outp)
        {
            if (CellFormatter == null)
            {
                outp.WriteLine($"<table class='{TableClass}'>");
                outp.WriteLine("<thead><tr>");
            
                foreach (var col in mapping.Columns)
                {
                    outp.WriteLine($"<th title='{col.Description}'>{col.Title}</th>");    
                }
                outp.WriteLine("</tr></thead>");

                foreach (var item in mapping.GetRows(items))
                {
                    outp.WriteLine("<tr>");
            
                    foreach (var cell in item)
                    {
                        outp.WriteLine($"<td>{cell.ValueDisplay}</th>");    
                    }
                    outp.WriteLine("</tr>");
                }
                outp.WriteLine("</table>");
            }
            else
            {
                outp.WriteLine($"<table class='{TableClass}'>");
                outp.WriteLine("<thead><tr>");
            
                foreach (var col in mapping.Columns)
                {
                    
                    outp.WriteLine($"<th title='{col.Description}'>{col.Title}</th>");    
                }
                outp.WriteLine("</tr></thead>");

                foreach (var item in mapping.GetRows(items))
                {
                    outp.WriteLine("<tr>");
            
                    foreach (var cell in item)
                    {
                        CellFormatter.WriteCell(outp, cell, new CellContainerTag("td", null, null));
                    }
                    outp.WriteLine("</tr>");
                }
                outp.WriteLine("</table>");
            }
        }
    }
}
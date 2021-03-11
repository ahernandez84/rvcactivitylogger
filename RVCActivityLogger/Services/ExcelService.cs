using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Xml;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

using NLog;

namespace RVCActivityLogger.Services
{
    class ExcelService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static bool GenerateReport(string fileNamePath, string reportName, DataTable dt, Color? reportColor = null)
        {
            try
            {
                using (ExcelPackage p = new ExcelPackage())
                {
                    p.Workbook.Properties.Author = "2021 Schneider Electric Homewood";
                    p.Workbook.Properties.Title = reportName;

                    ExcelWorksheet ws = CreateSheet(p, reportName);

                    //Merging cells and create a center heading for out table
                    ws.Cells[1, 1].Value = $"{reportName} Generated On {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt")}";
                    ws.Cells[1, 1, 1, 12].Merge = true;
                    ws.Cells[1, 1, 1, 12].Style.Font.Bold = true;
                    ws.Cells[1, 1, 1, 12].Style.Font.Size = 14;
                    ws.Cells[1, 1, 1, 12].Style.Font.Name = "Calibri";
                    ws.Cells[1, 1, 1, 12].Style.Font.Color.SetColor(Color.FromArgb(36, 77, 129));
                    ws.Cells[1, 1, 1, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[1, 1, 1, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ws.Cells[1, 1, 1, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[1, 1, 1, 12].Style.Fill.BackgroundColor.SetColor(reportColor ?? Color.FromArgb(232, 184, 14));
                   
                    ws.Row(1).Height = 60;
                    ws.Row(5).Height = 20;

                    int rowIndex = 5;

                    CreateHeader(ws, ref rowIndex, dt, reportColor);
                    CreateData(ws, ref rowIndex, dt);

                    for (int i = 1; i <= dt.Columns.Count; i++)
                    {
                        if (i == 6)
                            ws.Column(i).Width = 12;
                        else if (i == 7)
                            ws.Column(i).Width = 12;
                        else
                            ws.Column(i).AutoFit();
                    }

                    string file = fileNamePath + $"_{DateTime.Now.ToString("yyyyMMdd")}.xlsx";

                    Byte[] bin = p.GetAsByteArray();
                    File.WriteAllBytes(file, bin);

                    return true;
                }
            }
            catch (Exception ex) { logger.Error(ex, "ExcelService <GenerateReport> method."); return false; }
        }

        #region local methods
        private static ExcelWorksheet CreateSheet(ExcelPackage p, string sheetName)
        {
            p.Workbook.Worksheets.Add(sheetName);
            ExcelWorksheet ws = p.Workbook.Worksheets[1];
            ws.Name = sheetName; 
            ws.Cells.Style.Font.Size = 11; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            return ws;
        }

        private static void CreateHeader(ExcelWorksheet ws, ref int rowIndex, DataTable dt, Color? reportColor = null)
        {
            int colIndex = 1;
            foreach (DataColumn dc in dt.Columns) //Creating Headings
            {
                var cell = ws.Cells[rowIndex, colIndex];

                //Setting the background color of header cells
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(reportColor ?? Color.FromArgb(36, 77, 129));

                if (colIndex == 1)
                    cell.Style.Border.Bottom.Style = cell.Style.Border.Top.Style = cell.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                else if (colIndex == dt.Columns.Count)
                    cell.Style.Border.Bottom.Style = cell.Style.Border.Top.Style = cell.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                else
                    cell.Style.Border.Bottom.Style = cell.Style.Border.Top.Style = ExcelBorderStyle.Medium;

                //Setting Value in cell
                cell.Value = dc.ColumnName.Contains("Column") ? "" : dc.ColumnName;

                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                colIndex++;
            }
        }

        private static void CreateData(ExcelWorksheet ws, ref int rowIndex, DataTable dt)
        {
            foreach (DataRow dr in dt.Rows) // Adding Data into rows
            {
                int colIndex = 1;
                rowIndex++;

                foreach (DataColumn dc in dt.Columns)
                {
                    var cell = ws.Cells[rowIndex, colIndex];

                    if (colIndex == 1)
                        cell.Style.Numberformat.Format = "MM/dd/yyyy";
                    else if (colIndex == 6)
                        cell.Style.Numberformat.Format = "HH:mm:ss AM/PM";
                    else if (colIndex == 7)
                        cell.Style.Numberformat.Format = "HH:mm:ss AM/PM";

                    cell.Value = dr[dc.ColumnName];

                    //Setting borders of cell
                    var border = cell.Style.Border;
                    border.Left.Style = border.Right.Style = border.Bottom.Style = ExcelBorderStyle.Thin;
                    colIndex++;
                }
            }
        }
        #endregion


    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelicalGearInspection.Classes
{
    public static class CsvExporter
    {
        public static void ExportToCsv(DataGridView dataGridView)
        {
            // Show SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            saveFileDialog.Title = "Save CSV File";
            saveFileDialog.DefaultExt = "csv";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Get the data from the DataGridView
                    DataTable dataTable = GetDataTableFromDataGridView(dataGridView);

                    // Write the data to the CSV file
                    WriteDataTableToCsv(dataTable, saveFileDialog.FileName);

                    MessageBox.Show("Data exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static DataTable GetDataTableFromDataGridView(DataGridView dataGridView)
        {
            DataTable dataTable = new DataTable();

            // Add columns to DataTable
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                dataTable.Columns.Add(column.HeaderText, column.ValueType);
            }

            // Add rows to DataTable
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                List<object> rowData = new List<object>();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    rowData.Add(cell.Value);
                }
                dataTable.Rows.Add(rowData.ToArray());
            }

            return dataTable;
        }

        private static void WriteDataTableToCsv(DataTable dataTable, string filePath)
        {
            using (StreamWriter streamWriter = new StreamWriter(filePath))
            {
                // Write header
                string header = string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(column => QuoteIfNeeded(column.ColumnName)));
                streamWriter.WriteLine(header);

                // Write data rows
                foreach (DataRow row in dataTable.Rows)
                {
                    string rowData = string.Join(",", row.ItemArray.Select(value => QuoteIfNeeded(value.ToString())));
                    streamWriter.WriteLine(rowData);
                }
            }
        }

        private static string QuoteIfNeeded(string value)
        {
            // Quote the value if it contains a comma, double quote, or newline
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}

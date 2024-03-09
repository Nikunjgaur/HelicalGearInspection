using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.Xml;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml.Style;
using DocumentFormat.OpenXml.Office2013.Excel;
using System.Diagnostics;
using static ClosedXML.Excel.XLPredefinedFormat;
using DateTime = System.DateTime;

namespace HelicalGearInspection
{

    public static class Database
    {
        // Process process = new Process();

        static Database()
        {
            CreateDashboardTable();
            CreateGearReportTable();
        }

        public static void InsertDataIntoGearReport(string modelName, DateTime date, DateTime time, string result, string defectDetailsCam1, string defectDetailsCam2)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                using (NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = "INSERT INTO gearreport (ModelName, _date, _time, result, defectDetailsCam1, defectdetailscam2) VALUES (@ModelName, @Date, @Time, @Result, @DefectDetailsCam1, @DefectDetailsCam2)";

                    cmd.Parameters.AddWithValue("@ModelName", modelName);
                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@Time", time);
                    cmd.Parameters.AddWithValue("@Result", result);
                    cmd.Parameters.AddWithValue("@DefectDetailsCam1", defectDetailsCam1);
                    cmd.Parameters.AddWithValue("@DefectDetailsCam2", defectDetailsCam2);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static void CreateDashboardTable()
        {
            using (NpgsqlConnection connection = GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = connection;

                    // Define the SQL query to create the dashboard table
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS public.dashboard
                    (
                        _date date,
                        _time time without time zone,
                        part_number character varying COLLATE pg_catalog.""default"",
                        model character varying COLLATE pg_catalog.""default"",
                        status character varying COLLATE pg_catalog.""default"",
                        defect_type character varying COLLATE pg_catalog.""default"",
                        machine_name character varying COLLATE pg_catalog.""default""
                    )";

                    // Execute the SQL command
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void CreateGearReportTable()
        {
            using (NpgsqlConnection connection = GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = connection;

                    // Define the SQL query to create the gearreport table
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS public.gearreport
                    (
                        modelname character varying COLLATE pg_catalog.""default"",
                        _date date,
                        _time time without time zone,
                        result character varying COLLATE pg_catalog.""default"",
                        defectdetailscam1 character varying COLLATE pg_catalog.""default"",
                        defectdetailscam2 character varying COLLATE pg_catalog.""default""
                    )";

                    // Execute the SQL command
                    command.ExecuteNonQuery();
                }
            }
        }


        private static void TestConnection()
        {
            using (NpgsqlConnection con = GetConnection())
            {
                con.Open();
                if (con.State != ConnectionState.Open)
                {
                    MessageBox.Show("Can not connect to Database");
                }
                else
                {
                    Console.WriteLine("Connected to database.");
                }
            }
        }

        public static NpgsqlConnection GetConnection()
        {

            return new NpgsqlConnection(@"Server=localhost; Port = 5432; user Id = postgres; password = 1234; Database = postgres;");
        }

        static string machineNo = "ROLLING04";
        private static DateTime currentDate = DateTime.Now;

        // Replace with your SQL Server connection details
        static string connectionString = "Server=172.16.32.42;Database=TS02SonaScadaLine5;User Id=sa;Password=office@123;";
        static string RemoveHyphensAndColons(string input)
        {
            // Replace hyphens and colons with an empty string
            string cleanedString = input.Replace("-", "").Replace(":", "");

            return cleanedString;
        }
        //12:57:24
        public static string GetPartNoData(DateTime currentDate)
        {
            DataTable dataTable = new DataTable();
            string PartName = "NoNameFound";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string sqlQuery = $"SELECT top 1 PartNumber FROM multigaue_vision_data order by Date_Time desc;";
                Console.WriteLine(sqlQuery);
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@MachineNo", machineNo);
                    //command.Parameters.AddWithValue("@CurrentDate", currentDate);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PartName = reader[0].ToString();
                        }
                    }
                }
            }
            Console.WriteLine(PartName);
            Console.WriteLine(RemoveHyphensAndColons(PartName));
            return RemoveHyphensAndColons(PartName);
        }

        static (int okCount, int ngCount) GetFinalCount()
        {

            DateTime date = DateTime.Now.AddDays(-1);


            string query = $@"SELECT status, COUNT(status) AS Occurrences
                          FROM dashboard 
                          WHERE _date + _time > timestamp '{date:yyyy-MM-dd} {"07:00:00"}' and _date + _time < timestamp '{date.AddDays(1):yyyy-MM-dd} {"06:59:00"}'
                          GROUP BY status
                          ORDER BY status DESC;";

            int okCount = 0;
            int ngCount = 0;

            using (NpgsqlConnection connection = GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string status = reader["status"].ToString();
                            int occurrences = Convert.ToInt32(reader["occurrences"]);

                            if (status == "OK")
                                okCount = occurrences;
                            else if (status == "NG")
                                ngCount = occurrences;
                        }
                    }
                }
            }

            return (okCount, ngCount);
        }



        //public static void InsertData(string jsonData)
        //{
        //    //File.WriteAllText("JsonDefect.json", jsonData);
        //    using (NpgsqlConnection connection = GetConnection())
        //    {
        //        connection.Open();

        //        using (NpgsqlCommand command = new NpgsqlCommand())
        //        {
        //            command.Connection = connection;
        //            command.CommandType = CommandType.Text;

        //            // Parse JSON data
        //            List<List<Defect>> defectGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<List<List<Defect>>>(jsonData);

        //            foreach (List<Defect> defectGroup in defectGroups)
        //            {
        //                foreach (Defect defect in defectGroup)
        //                {
        //                    // Combine defect types separated by comma
        //                    string combinedDefectTypes = string.Join(", ", defectGroup.Select(d => d.defType).Distinct());

        //                    // Insert data query
        //                    string insertDataQuery = $@"
        //                    INSERT INTO dashboard (part_number, model, status, defect_type, machine_name)
        //                    VALUES ('{defect.CameraNum}', '{defect.Degree}', '{defect.Type}', '{combinedDefectTypes}', 'MachineNameValue')";

        //                    command.CommandText = insertDataQuery;
        //                    command.ExecuteNonQuery();
        //                }
        //            }
        //        }
        //    }
        //}

        static string RemoveDuplicateWords(string input)
        {
            // Split the input string into words
            string[] words = input.Split(',');

            // Use LINQ to filter out duplicate words
            string[] distinctWords = words.Distinct().ToArray();



            // Join the distinct words back into a string
            string result = string.Join(", ", distinctWords);
            result = result.Replace("Ng,", "");
            result = result.Replace("Ng", "");
            result = string.Join(", ", result);

            return result;
        }
        //    _date date,
        //    _time time without time zone,
        //part_number character varying COLLATE pg_catalog."default",
        //model character varying COLLATE pg_catalog."default",
        //status character varying COLLATE pg_catalog."default",
        //defect_type character varying COLLATE pg_catalog."default",
        //machine_name character varying COLLATE pg_catalog."default"

        public static void CreateDailyExcel()
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();


                //string query = $@"SELECT defectdetailscam1, defectdetailscam2 FROM public.gearreport WHERE _date + _time > timestamp '{date:yyyy-MM-dd} {"07:00:00"}' and _date + _time < timestamp '{date.AddDays(1):yyyy-MM-dd} {"06:59:00"}'";


                DateTime date = DateTime.Now.AddDays(-1);

                string sql = $@"SELECT _date as ""Date"", _time as ""Time"", model as ""Model Name"",part_number as ""Part Number"",
                              status as ""Status"", defect_type as ""Defect Type"", machine_name as ""Machine Name""
                              FROM dashboard WHERE _date + _time > timestamp '{date:yyyy-MM-dd} {"07:00:00"}' and _date + _time < timestamp '{date.AddDays(1):yyyy-MM-dd} {"06:59:00"}'
                               order by _date asc, _time asc";


                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {

                    using (NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        WriteDataTableToExcel(dt, $"{AppData.ProjectDirectory}/Report/Dailyreport.xlsx");
                    }
                }
            }
        }

        private static void WriteDataTableToExcel(DataTable dataTable, string excelFilePath)
        {
            System.DateTime date = Convert.ToDateTime(System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


            using (ExcelPackage package = new ExcelPackage())
            {

                // Add a worksheet to the workbook
                package.Workbook.Worksheets.Add("SummaryReport");
                var worksheet = package.Workbook.Worksheets.Add("DetailReport");
                StyleCells(package, "SummaryReport");
                // Write the DataTable to the worksheet starting from cell A1
                worksheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Column(2).Style.Numberformat.Format = "hh:mm:ss";
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);
                worksheet.Columns.BestFit = true;
                worksheet.Cells.AutoFitColumns();
                worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                string modelRange = "A1:G" + dataTable.Rows.Count.ToString();
                var modelTable = worksheet.Cells[modelRange];
                // Assign borders
                modelTable.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                modelTable.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                modelTable.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                modelTable.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Call function to display chart

                var count = GetFinalCount();



                // Query to get defectdetailscam1 and defectdetailscam2 for the current day
                string query = $@"SELECT defectdetailscam1, defectdetailscam2 FROM public.gearreport WHERE _date + _time > timestamp '{date:yyyy-MM-dd} {"07:00:00"}' and _date + _time < timestamp '{date.AddDays(1):yyyy-MM-dd} {"06:59:00"}'";
                //Console.WriteLine(query);
                DisplayPieChart(package, count.okCount, count.ngCount);
                DisplayBarChart(package, GetDefectCountsForCurrentDate(date));
                // Save the workbook to the specified Excel file
                worksheet.Cells.AutoFitColumns();

                try
                {
                    package.SaveAs(new System.IO.FileInfo(excelFilePath));

                }
                catch (InvalidOperationException ex)
                {
                    CloseWindowByName("Dailyreport - Excel");
                    package.SaveAs(new System.IO.FileInfo(excelFilePath));

                }
                catch (Exception ex)
                {
                    ConsoleExtension.WriteWithColor("Unable to save Daily Report. " + ex.Message, ConsoleColor.Red);
                }
                worksheet.Dispose();
                string server = "172.16.12.168";
                int port = 25;
                string username = "notifications.msr@sonacomstar.com";
                string password = "@#Alerts*5373";
                string[] recipientEmails = { "assembly.msr@sonacomstar.com", "pravi.mishra@sonacomstar.com" };
                string[] ccEmails = { "pradeep.gupta@sonacomstar.com", "parakh.manocha@sonacomstar.com", "avi.lakhani@sonacomstar.com", "anmol.singh@sonacomstar.com", "umesh.miglani@sonacomstar.com" };

                EmailSender.SendEmail(server, port, username, password, recipientEmails, ccEmails, excelFilePath);
            }
        }

        private static void CloseWindowByName(string windowName)
        {
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                IntPtr mainWindowHandle = process.MainWindowHandle;
                string currentWindowTitle = process.MainWindowTitle;

                if (!string.IsNullOrEmpty(currentWindowTitle) && currentWindowTitle.Contains(windowName))
                {
                    process.CloseMainWindow();
                    process.WaitForExit();

                    Console.WriteLine($"Window '{windowName}' closed successfully.");
                    return;
                }
            }

            Console.WriteLine($"Window '{windowName}' not found.");
        }

        private static void StyleCells(ExcelPackage package, string worksheetName)
        {
            // Add a chart to the worksheet
            var worksheet = package.Workbook.Worksheets[worksheetName];
            int fontSize = 22;
            worksheet.Cells["A1"].Value = "SONA COMSTAR";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1:F1"].Style.Font.Bold = true;
            worksheet.Cells["A1:F1"].Style.Font.Size = fontSize;
            //worksheet.Cells["A1:F1"].Style.Font.Color.SetColor(0, 59, 106, 200);
            worksheet.Cells["A1:F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A1:F1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells["K1"].Value = $"SUMMARY REPORT - {DateTime.Now.ToString("dd/MM/yyyy")}";
            worksheet.Cells["K1:Q1"].Merge = true;
            worksheet.Cells["K1:Q1"].Style.Font.Size = fontSize;
            worksheet.Cells["K1:Q1"].Style.Font.Bold = true;
            //worksheet.Cells["K1:Q1"].Style.Font.Color.SetColor();

            worksheet.Cells["K1:Q1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["K1:Q1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }


        private static void DisplayBarChart(ExcelPackage package, Dictionary<string, int> defectCounts)
        {
            // Add a chart to the worksheet
            var worksheet = package.Workbook.Worksheets["SummaryReport"];
            int rowIndex = 26;
            worksheet.Cells["L25"].Value = "Defect Name";
            worksheet.Cells["M25"].Value = "Defect Value";

            foreach (var defect in defectCounts)
            {
                worksheet.Cells[$"L{rowIndex}"].Value = defect.Key;
                worksheet.Cells[$"M{rowIndex}"].Value = defect.Value;
                rowIndex++;
            }
            var chart = worksheet.Drawings.AddChart("DefectCountsChart", eChartType.ColumnClustered);
            chart.SetPosition(2, 0, 8, 0);
            chart.SetSize(550, 400);
            chart.Title.Text = "Defect Counts Chart";
            chart.Legend.Position = eLegendPosition.Right;
            for (int i = 0; i < defectCounts.Count; i++)
            {
                // Add series to the chart
                var series = chart.Series.Add(worksheet.Cells[$"M{i + 26}"]);
                var barSeries = (ExcelBarChartSerie)series;
                barSeries.DataLabel.Font.Bold = true;
                barSeries.DataLabel.ShowValue = true;
                barSeries.DataLabel.ShowPercent = true;
                barSeries.DataLabel.ShowLeaderLines = true;
                barSeries.DataLabel.Separator = ";";
                barSeries.DataLabel.Position = eLabelPosition.OutEnd;
                series.Header = worksheet.Cells[$"L{i + 26}"].Value.ToString();
            }
            worksheet.Cells.AutoFitColumns();


            // You can further customize the chart as needed
        }

        private static void DisplayPieChart(ExcelPackage package, int okCount, int ngCount)
        {
            // Add a chart to the worksheet
            var worksheet = package.Workbook.Worksheets["SummaryReport"];
            worksheet.Cells["D25"].Value = "Total OK";
            worksheet.Cells["E25"].Value = okCount;
            worksheet.Cells["D26"].Value = "Total NG";
            worksheet.Cells["E26"].Value = ngCount;



            var chart = worksheet.Drawings.AddChart("FinalReportChart", eChartType.Pie);
            chart.SetPosition(2, 0, 0, 0);
            chart.SetSize(500, 400);
            chart.Title.Text = "Final Result Chart";
            chart.Legend.Position = eLegendPosition.Right;
            var series = chart.Series.Add(worksheet.Cells[$"E25:E26"], worksheet.Cells[$"D25:D26"]);

            var pieSeries = (ExcelPieChartSerie)series;
            pieSeries.DataLabel.Font.Bold = true;
            pieSeries.DataLabel.ShowValue = true;
            pieSeries.DataLabel.ShowPercent = true;
            pieSeries.DataLabel.Separator = ";";
            pieSeries.DataLabel.Position = eLabelPosition.OutEnd;
            worksheet.Cells.AutoFitColumns();

            //for (int i = 0; i < defectCounts.Count; i++)
            //{
            //    // Add series to the chart
            //    var barSeries = (ExcelBarChartSerie)series;
            //    barSeries.DataLabel.Font.Bold = true;
            //    barSeries.DataLabel.ShowValue = true;
            //    barSeries.DataLabel.ShowPercent = true;
            //    barSeries.DataLabel.ShowLeaderLines = true;
            //    barSeries.DataLabel.Separator = ";";
            //    barSeries.DataLabel.Position = eLabelPosition.OutEnd;
            //    series.Header = worksheet.Cells[$"I{i + 1}"].Value.ToString();
            //}


            // You can further customize the chart as needed
        }


        public static void CreateCsvlFile()
        {
            try
            {
                using (NpgsqlConnection connection = GetConnection())
                {
                    connection.Open();

                    string query = $@"COPY (SELECT * FROM dashboard where _date = '{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")}') TO '{AppData.ProjectDirectory}/Report/Dailyreport.csv' WITH CSV HEADER;";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                string server = "172.16.12.168";
                int port = 25;
                string username = "notifications.msr@sonacomstar.com";
                string password = "@#Alerts*5373";
                string[] recipientEmails = { "gaurnikunj116@gmail.com", "assembly.msr@sonacomstar.com", "pravi.mishra@sonacomstar.com" };
                string[] ccEmails = { "pradeep.gupta@sonacomstar.com", "parakh.manocha@sonacomstar.com", "avi.lakhani@sonacomstar.com", "anmol.singh@sonacomstar.com", "umesh.miglani@sonacomstar.com" };

                //EmailSender.SendEmail(server, port, username, password, recipientEmails, ccEmails, );

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }



        public static void InsertData(string partNumber, string modelName, string result, params string[] jsonData)
        {
            using (NpgsqlConnection connection = GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    string combinedDefectTypes = "";
                    foreach (string json in jsonData)
                    {
                        // Parse JSON data
                        List<List<Defect>> defectGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<List<List<Defect>>>(json);
                        if (!string.IsNullOrEmpty(combinedDefectTypes))
                        {
                            combinedDefectTypes += ", ";
                        }
                        foreach (List<Defect> defectGroup in defectGroups)
                        {
                            if (!string.IsNullOrEmpty(combinedDefectTypes) && combinedDefectTypes.Substring(combinedDefectTypes.Length - 2) != ", ")
                            {
                                combinedDefectTypes += ", ";
                            }
                            // Combine defect types separated by comma
                            combinedDefectTypes += string.Join(", ", defectGroup.Select(d => d.defType).Distinct());
                        }
                    }


                    //                _date date,
                    //                _time time without time zone,
                    //part_number character varying COLLATE pg_catalog."default",
                    //model character varying COLLATE pg_catalog."default",
                    //status character varying COLLATE pg_catalog."default",
                    //defect_type character varying COLLATE pg_catalog."default",
                    //machine_name character varying COLLATE pg_catalog."default"
                    //ConsoleExtension.WriteWithColor("");
                    //File.WriteAllText("combinedDefectTypes.txt", combinedDefectTypes);

                    string defects = RemoveDuplicateWords(combinedDefectTypes);

                    string insertDataQuery = $@"
                            INSERT INTO dashboard (_date, _time, part_number, model, status, defect_type, machine_name)
                            VALUES (@Date, @Time,  '{partNumber}', '{modelName}', '{result}', '{defects}', 'ROLLING04')";
                    command.CommandText = insertDataQuery;
                    command.Parameters.AddWithValue("@Date", Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")));
                    command.Parameters.AddWithValue("@Time", Convert.ToDateTime(DateTime.Now.ToString("HH:mm:ss")));
                    command.ExecuteNonQuery();
                }
            }
        }

        class Defect
        {
            public int CameraNum { get; set; }
            public int Degree { get; set; }
            public string defType { get; set; }
            public int Type { get; set; }
        }




        public static Dictionary<string, int> GetDataForDate(DateTime dateTime)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // Get the current date
                DateTime currentDate = dateTime;

                // Query to get defectdetailscam1 and defectdetailscam2 for the current day
                string query = $"SELECT defectdetailscam1, defectdetailscam2 FROM public.gearreport WHERE _date = '{currentDate.ToString("yyyy-MM-dd")}'";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        Dictionary<string, int> totalDefectCounts = new Dictionary<string, int>(); // Combined counts

                        while (reader.Read())
                        {
                            // Retrieve defectdetailscam1 data
                            string defectDetailsCam1Json = reader["defectdetailscam1"].ToString();
                            List<List<DefectInfo>> defectDetailsCam1 = JsonConvert.DeserializeObject<List<List<DefectInfo>>>(defectDetailsCam1Json);

                            // Retrieve defectdetailscam2 data
                            string defectDetailsCam2Json = reader["defectdetailscam2"].ToString();
                            List<List<DefectInfo>> defectDetailsCam2 = JsonConvert.DeserializeObject<List<List<DefectInfo>>>(defectDetailsCam2Json);

                            // Count occurrences of each defect class and accumulate into totalDefectCounts
                            AccumulateDefectCounts(defectDetailsCam1, totalDefectCounts);
                            AccumulateDefectCounts(defectDetailsCam2, totalDefectCounts);
                        }

                        // Display combined defect counts
                        Console.WriteLine("Defect Counts:");
                        foreach (var defectCount in totalDefectCounts)
                        {
                            Console.WriteLine($"{defectCount.Key}: {defectCount.Value}");
                        }
                        return totalDefectCounts;
                    }
                }
            }
        }


        static Dictionary<string, int> GetDefectCountsForCurrentDate(DateTime dateTime)
        {

            Dictionary<string, int> defectCounts = new Dictionary<string, int>();

            using (NpgsqlConnection connection = GetConnection())
            {
                connection.Open();

                // Write your SQL query
                string sqlQuery = $@"
                SELECT
                    trim(unnested_defect) AS defect_type,
                    COUNT(*) AS defect_count
                FROM
                    public.dashboard,
                    unnest(string_to_array(defect_type, ',')) AS unnested_defect
                WHERE
                    _date + _time > timestamp '{dateTime:yyyy-MM-dd} {"07:00:00"}' 
                    and _date + _time < timestamp '{dateTime.AddDays(1):yyyy-MM-dd} {"06:59:00"}' and status = 'NG'
                    AND trim(unnested_defect) <> 'Ng' AND trim(unnested_defect) <> ''
                GROUP BY
                    unnested_defect;
            ";

                ConsoleExtension.WriteWithColor(sqlQuery, ConsoleColor.Yellow);

                using (NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string defectType = reader["defect_type"].ToString();
                        int defectCount = Convert.ToInt32(reader["defect_count"]);

                        // Check if the defect type already exists in the dictionary
                        if (defectCounts.ContainsKey(defectType))
                        {
                            // Update the existing defect count
                            defectCounts[defectType] += defectCount;
                        }
                        else
                        {
                            // Add a new entry to the dictionary
                            defectCounts.Add(defectType, defectCount);
                        }
                    }
                }
            }

            return defectCounts;
        }

        public static Dictionary<string, int> GetChartDataForDate(DateTime dateTime, string query = "")
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                if (string.IsNullOrEmpty(query))
                {
                    // Query to get defectdetailscam1 and defectdetailscam2 for the current day
                    query = $@"SELECT defectdetailscam1, defectdetailscam2 FROM public.gearreport WHERE _date + _time >= timestamp '{dateTime:yyyy-MM-dd} {dateTime:HH:mm:ss}'";

                }

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        Dictionary<string, int> totalDefectCounts = new Dictionary<string, int>(); // Combined counts

                        while (reader.Read())
                        {
                            // Retrieve defectdetailscam1 data
                            string defectDetailsCam1Json = reader["defectdetailscam1"].ToString();
                            List<List<DefectInfo>> defectDetailsCam1 = JsonConvert.DeserializeObject<List<List<DefectInfo>>>(defectDetailsCam1Json);

                            // Retrieve defectdetailscam2 data
                            string defectDetailsCam2Json = reader["defectdetailscam2"].ToString();
                            List<List<DefectInfo>> defectDetailsCam2 = JsonConvert.DeserializeObject<List<List<DefectInfo>>>(defectDetailsCam2Json);

                            // Count occurrences of each defect class and accumulate into totalDefectCounts
                            AccumulateDefectCounts(defectDetailsCam1, totalDefectCounts);
                            AccumulateDefectCounts(defectDetailsCam2, totalDefectCounts);
                        }

                        // Display combined defect counts
                        Console.WriteLine("Defect Counts:");
                        foreach (var defectCount in totalDefectCounts)
                        {
                            Console.WriteLine($"{defectCount.Key}: {defectCount.Value}");
                        }
                        return totalDefectCounts;
                    }
                }
            }
        }

        static void AccumulateDefectCounts(List<List<DefectInfo>> defectDetails, Dictionary<string, int> totalDefectCounts)
        {
            // Defect classes
            List<string> defectClasses = new List<string>
            {
                "Handling dent",
                "Root grinding",
                "ChamferMiss",
                "step grinding",
                "Flank_unclean",
                "Rust",
                "Without grinding",
                "Heat treatment dent"
            };

            // Flatten the list of defect details
            List<DefectInfo> allDefects = defectDetails.SelectMany(x => x).ToList();

            // Count occurrences of each defect class and accumulate into totalDefectCounts
            foreach (string defectClass in defectClasses)
            {
                int count = allDefects.Count(defect => defect.defType == defectClass);

                if (totalDefectCounts.ContainsKey(defectClass))
                {
                    totalDefectCounts[defectClass] += count;
                }
                else
                {
                    totalDefectCounts.Add(defectClass, count);
                }
            }
        }

        class DefectInfo
        {
            public int CameraNum { get; set; }
            public int Degree { get; set; }
            public string defType { get; set; }
            public int Type { get; set; }
        }



    }


}
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;

namespace Logging
{
    /// <summary>
    /// This class is used to perform IO operations on *csv files. It can be used by various Proschlaf applications for the purposes of storing and retrieving data based on *csv files.
    /// </summary>
    public class CsvHandler
    {
        private CsvHandler()
        { }


        /// <summary>
        /// Reads a *csv file (separated by semicolons) and returns a DataTable which contains all values from the file.
        /// Note that the first line is expected to represent the column names. If those names are enclosed in apostrophes, the apostrophes are removed for easier access to the column names in the DataTable.
        /// If an exception occurs, the table is empty and the exception details are contained in the out-parameter.
        /// </summary>
        /// <param name="path">the path to the file, must include the " .csv" at the end</param>
        /// <param name="includesHeaderLine">If set to true, this method reads the first line of the file as header line.</param>
        /// <returns>a DataTable filled with the values from the file.</returns>
        public static DataTable ReadCSVFile(string path, out Exception exception, bool includesHeaderLine = true)
        {
            DataTable table = null;

            if (File.Exists(path))
                exception = ReadCsvFile(path, includesHeaderLine, out table);
            else
                exception = new FileNotFoundException("Could not locate the file to be read.", path);

            return table;
        }

        /// <summary>
        /// Reads a *csv file (separated by semicolons) and returns a DataTable which contains all values from the file.
        /// Note that the first line is expected to represent the column names. If those names are enclosed in apostrophes, the apostrophes are removed for easier access to the column names in the DataTable.
        /// If an exception occurs, NULL is returned.
        /// </summary>
        /// <param name="path">the path to the file, must include the " .csv" at the end</param>
        /// <returns>a DataTable filled with the values from the file or NULL.</returns>
        public static DataTable ReadCSVFile(string path, bool includesHeaderLine = true)
        {
            DataTable table = null;
            Exception exception;

            if (File.Exists(path))
                exception = ReadCsvFile(path, includesHeaderLine, out table);
            else
                exception = new FileNotFoundException("Could not locate the file to be read.", path);

            return exception == null ? table : null;
        }

        private static Exception ReadCsvFile(string path, bool includesHeaderLine, out DataTable table)
        {
            StreamReader reader = null;
            table = null;

            if (string.IsNullOrEmpty(path))
                return new ArgumentNullException("path");

            if (!File.Exists(path))
                return new FileNotFoundException("Could not find *csv file: " + path);

            try
            {
                try
                {
                    reader = new StreamReader(path, System.Text.Encoding.Default); //do not change this to UTF-8 or certain files with umlauts will stop working
                }
                catch(Exception)
                {
                    reader = new StreamReader(path, System.Text.Encoding.UTF8);
                }

                table = new DataTable();
                string temp;
                table.BeginInit();

                if (includesHeaderLine)
                {
                    string[] header = reader.ReadLine().Split(';');

                    int infiniteStopCnt = 0;
                    while (header.Length == 0 && infiniteStopCnt < 500) //find the column headers
                    {
                        header = reader.ReadLine().Split(';');
                        infiniteStopCnt++;
                    }

                    if (header == null || header.Length == 0) //the file is empty
                        return null;


                    for (int i = 0; i < header.Length; i++) //add as many empty columns as there are columns in the csv file and set header names
                    {
                        if (header[i] != null) //some *csv files enclose each column name in apostrophes ('"') --> remove those
                        {
                            if (header[i].StartsWith("\""))
                                header[i] = header[i].TrimStart(new char[] { '"' });

                            if (header[i].EndsWith("\""))
                                header[i] = header[i].TrimEnd(new char[] { '"' });
                        }

                        table.Columns.Add(header[i]);
                    }

                    temp = reader.ReadLine();
                }
                else
                {
                    //initialize the datatable columns
                    string firstLine = reader.ReadLine();

                    if (string.IsNullOrEmpty(firstLine)) //the file is either empty or has a faulty format
                        return null;

                    string[] firstLineSplit = firstLine.Split(';');

                    for (int i = 0; i < firstLineSplit.Length; i++) //add as many empty columns as there are columns in the csv file
                    {
                        table.Columns.Add();
                    }

                    temp = firstLine;
                }

                table.EndInit();

                if (temp == null) //the file only contains one row
                    return null;

                table.BeginLoadData();

                do //read all rows, one per iteration and fill the values into the datatable
                {
                    string[] tempArray = temp.Split(';'); //split the values from the read row

                    if (tempArray.Length < table.Columns.Count && !temp.Contains("\"")) //multiline entries have 1 to N columns
                        continue;

                    DataRow row = table.NewRow(); //new row with the format from the datatable

                    row.BeginEdit();

                    for (int i = 0; i < table.Columns.Count; i++) //set the values in the columns of the row
                    {
                        if (i >= tempArray.Length)
                            continue;

                        if (tempArray[i].StartsWith("\"")) //check if one of the columns contains valid line breaks (starts and ends with '"') and this is a multi-line column entry
                        {
                            string remainingLine;
                            row[i] = ReadMultiLine(tempArray, i, reader, out remainingLine);

                            if (row[i] != null)
                            {
                                tempArray[i] = (string)row[i];

                                if (remainingLine != null)
                                {
                                    string[] remaingLineSplit = remainingLine.Split(';'); //split the remaining columns
                                    string[] newTempArray = new string[tempArray.Length + remaingLineSplit.Length];
                                    tempArray.CopyTo(newTempArray, 0);
                                    remaingLineSplit.CopyTo(newTempArray, tempArray.Length);
                                    tempArray = newTempArray;
                                }
                            }
                            else if (row[i] == null)
                                continue;
                        }
                        else
                            row[i] = tempArray[i];
                    }

                    row.EndEdit();

                    table.Rows.Add(row); //add the row to the table
                }
                while ((temp = reader.ReadLine()) != null);

                table.EndLoadData();

                //return the scanned table
                return null;
            }

            catch (Exception ex)
            {
                table = null;
                return ex;
            }

            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        private static string ReadMultiLine(string[] tempArray, int i, StreamReader reader, out string remainingLine)
        {
            remainingLine = null;
            bool multiLineEnd = false;

            try
            {
                string fullText = tempArray[i];

                do
                {
                    string nextLine = reader.ReadLine();

                    if (nextLine == null)
                        multiLineEnd = true;
                    else
                        multiLineEnd = nextLine.Contains("\"");

                    if (multiLineEnd)
                    {
                        if (nextLine != null)
                        {
                            string replacementString = "\";";
                            int index = nextLine.IndexOf(replacementString);

                            if (index < 0)
                            {
                                replacementString = "\"";
                                index = nextLine.IndexOf(replacementString);
                                remainingLine = nextLine.Substring(index);
                            }
                            else
                                remainingLine = nextLine.Substring(index + 2);

                            nextLine = nextLine.Substring(0, nextLine.IndexOf(replacementString));
                        }
                    }

                    fullText += nextLine;
                }
                while (!multiLineEnd); //read lines until the ending '"' has been reached (there might still be another multiline-entry in the remaining portion of the current line, which has to be handled with another call to this method

                return fullText;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Writes the datatable to a *csv file using the provided separator and Unicode (UTF-16) as default encoding.
        /// </summary>
        /// <param name="datatable">the datatable that ahs to be saved</param>
        /// <param name="seperator">the seperator that delimits every column, in this case its always a ';'</param>
        /// <param name="path">The path to where the DataTable shall be written to.</param>
        public static Exception WriteDataTable(DataTable datatable, char seperator, string path)
        {
            return WriteDataTable(datatable, seperator, path, Encoding.Unicode);
        }

        /// <summary>
        /// Writes the datatable to a *csv file using the provided separator.
        /// </summary>
        /// <param name="datatable">the datatable that ahs to be saved</param>
        /// <param name="seperator">the seperator that delimits every column, in this case its always a ';'</param>
        /// <param name="path">The path to where the DataTable shall be written to.</param>
        public static Exception WriteDataTable(DataTable datatable, char seperator, string path, Encoding encoding)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path, false, encoding)) //overwrite existing files
                {
                    int numberOfColumns = datatable.Columns.Count;

                    for (int i = 0; i < numberOfColumns; i++) //write Header names
                    {
                        sw.Write(datatable.Columns[i]);
                        if (i < numberOfColumns - 1)
                            sw.Write(seperator);
                    }

                    sw.Write(sw.NewLine);

                    foreach (DataRow dr in datatable.Rows) //write data
                    {
                        for (int i = 0; i < numberOfColumns; i++)
                        {
                            sw.Write(dr[i].ToString());

                            if (i < numberOfColumns - 1)
                                sw.Write(seperator);
                        }
                        sw.Write(sw.NewLine);
                    }

                    sw.Close();
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Fatal, "Error in CsvHandler.WriteDataTable() for file: " + path, ex);
                return ex;
            }
        }

        /// <summary>
        /// Reads a *csv file which was stored as string and returns a DataTable which contains all values from the file.
        /// </summary>
        /// <param name="csv">The string that contains the CSV-formatted text (NOT the path to some *csv file).</param>
        /// <param name="table">Will contain a DataTable filled with the values from the file when the string could be parsed successfully.</param>
        /// <returns>Null if successful or an exception.</returns>
        public static Exception ReadStringAsCsv(string csv, out DataTable table)
        {
            StringReader reader = null;
            table = new DataTable();

            try
            {
                reader = new StringReader(csv);

                string[] header = reader.ReadLine().Split(';'); //read the column headers

                if (header == null) //the file is empty
                    return null;

                for (int i = 0; i < header.Length; i++) //add as many empty columns as there are columns in the csv file and set header names
                {
                    table.Columns.Add(header[i]);
                }

                string temp = reader.ReadLine();

                if (temp == null) //the file only contains one row
                    return null;

                do //read all rows, one per iteration and fill the values into the datatable
                {
                    string[] tempArray = temp.Split(';'); //split the values from the read row

                    if (tempArray.Length != table.Columns.Count)
                        continue;

                    DataRow row = table.NewRow(); //new row with the format from the datatable

                    row.BeginEdit();

                    for (int i = 0; i < table.Columns.Count; i++) //set the values in the columns of the row
                    {
                        row[i] = tempArray[i];
                    }

                    row.EndEdit();

                    table.Rows.Add(row); //add the row to the table
                }
                while ((temp = reader.ReadLine()) != null);

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        /// <summary>
        /// Writes the datatable to a *csv formatted String using the provided separator.
        /// </summary>
        /// <param name="datatable">the datatable that ahs to be saved</param>
        /// <param name="seperator">the seperator that delimits every column, in this case its always a ';'</param>
        /// <param name="path">The path to where the DataTable shall be written to.</param>
        public static Exception WriteDataTableToString(DataTable datatable, char seperator, out string csv)
        {
            csv = null;
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    int numberOfColumns = datatable.Columns.Count;

                    for (int i = 0; i < numberOfColumns; i++) //write Header names
                    {
                        sw.Write(datatable.Columns[i]);
                        if (i < numberOfColumns - 1)
                            sw.Write(seperator);
                    }

                    sw.Write(sw.NewLine);

                    foreach (DataRow dr in datatable.Rows) //write data
                    {
                        for (int i = 0; i < numberOfColumns; i++)
                        {
                            sw.Write(dr[i].ToString());

                            if (i < numberOfColumns - 1)
                                sw.Write(seperator);
                        }
                        sw.Write(sw.NewLine);
                    }
                    csv = sw.GetStringBuilder().ToString();
                    sw.Close();
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logging.Logger.LogEntryCategories.Fatal, "Error in CsvHandler.WriteDataTableToString() : " + ex);
                return ex;
            }
        }

        #region Experimental
        /// <summary>
        /// Experimental method that can handle *csv files where each value starts and ends with '"' and possibly even multi-line values.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static DataTable ReadCsvFileWithApostrophes(string path, out Exception exception)
        {
            DataTable table = null;
            exception = ReadCSVFileNew(path, out table);
            return table;
        }

        /// <summary>
        /// Experimental parse method that can handle apostrophes and multiline-values.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private static Exception ReadCSVFileNew(string path, out DataTable table)
        {
            StreamReader reader = null;
            table = null;

            try
            {
                reader = new StreamReader(path, System.Text.Encoding.Default);
                table = new DataTable();

                string[] header = reader.ReadLine().Split(';');

                int infiniteStopCnt = 0;
                while (header.Length == 0 && infiniteStopCnt < 500)  //find the column headers
                {
                    header = reader.ReadLine().Split(';');
                    infiniteStopCnt++;
                }

                if (header == null || header.Length == 0) //the file is empty
                    return null;

                for (int i = 0; i < header.Length; i++) //add as many empty columns as there are columns in the csv file and set header names
                {
                    if (header[i] != null) //some *csv files enclose each column name in apostrophes ('"') --> remove those
                    {
                        if (header[i].StartsWith("\""))
                            header[i] = header[i].TrimStart(new char[] { '"' });

                        if (header[i].EndsWith("\""))
                            header[i] = header[i].TrimEnd(new char[] { '"' });
                    }

                    table.Columns.Add(header[i]);
                }

                string temp = reader.ReadLine();

                if (temp == null) //the file only contains one row
                    return null;

                table.BeginLoadData();

                do //read all rows, one per iteration and fill the values into the datatable
                {
                    DataRow row = table.NewRow(); //new row with the format from the datatable
                    row.BeginEdit();
                    bool finished = false;
                    bool isMuliLine = false;
                    int separatorIndex = -1, columnIndex = 0;
                    do
                    {
                        string columnText;
                        separatorIndex = ReadUntilSeparator(temp, (separatorIndex + 1), ';', out columnText); //this trims start and end of temp if it starts/ends with '"'

                        if (separatorIndex >= 0)
                        {
                            if (!isMuliLine)
                            {
                                row[columnIndex] = columnText;
                                columnIndex++;
                            }
                            else if (isMuliLine || columnText.StartsWith("\"")) //possible multi-line text
                            {
                                isMuliLine = true;
                                row[columnIndex] += columnText;
                            }
                            else if (isMuliLine && temp.EndsWith("\";")) //end of multi-line text
                            {
                                row[columnIndex] += columnText;
                                columnIndex++;
                            }
                        }
                        else if (separatorIndex == -1)
                        {
                            row[columnIndex] = columnText; //usually the end of a line
                            finished = true;
                        }
                        else //error / bad format
                        {
                            row[columnIndex] = "CSV parse error";
                            finished = true;
                            columnIndex++;
                        }

                        if (columnIndex >= table.Columns.Count)
                        {
                            finished = true;
                        }
                    }
                    while (finished == false);

                    row.EndEdit();

                    table.Rows.Add(row); //add the row to the table
                }
                while ((temp = reader.ReadLine()) != null);

                table.EndLoadData();

                return null;   //return the parsed table
            }
            catch (Exception ex)
            {
                table = null;
                return ex;
            }

            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        private static int ReadUntilSeparator(string temp, int startIndex, char separator, out string columnText)
        {
            columnText = "";

            if (temp == null || temp.Length < 1)
                return 0;

            int index = temp.IndexOf(separator, startIndex);

            if (index >= 0)
            {
                int tmpIndex = index - startIndex;
                if (temp[startIndex] == '"' && temp[index - 1] == '"') //column starts and ends with '"' --> trim it
                    columnText = temp.Substring(startIndex + 1, tmpIndex - 2);
                else
                    columnText = temp.Substring(startIndex, tmpIndex);

                return index;
            }
            else
            {
                columnText = temp.Substring(startIndex);

            }

            return -1;
        }
        #endregion
    }
}

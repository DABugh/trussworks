using System;
using System.IO;
using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

//Assumptions:
//	CSV file has a header row; This row will be validated for proper encoding, but will not be
//		parsed to conform with ColumnTypes (each header field is assumed to be a string)
//	Input data will be ignored for any columns of TotalDuration type,
//		and value will be recalculated
//	Any colums of TotalDuration type will be sum of all preceding (but
//		not subsequent) columns of Duration type
//	Total record length will not exceed 2048 bytes (adjustable default of CsvParser)
//	Strings containing delimiter are enclosed in double-quotes {"} in input
//	Time stamps have no time zone information (foundation laid to remove this assumption)


//Purpose: Read in delimited spreadsheet, apply rules based on datatypes,
//  validate character encoding, format data to specified standards, output results
class CsvNormalizer
{
    #region ENUMS

    //Each column has a DataType, which determines how fields are validated, modified, and formatted
    public enum DataType
    {
        Timestamp,
        ZipCode,
        FullName,
        Duration,
        TotalDuration,
        UnmodifiedString
    }

    #endregion ENUMS
    #region PROPERTIES

    //The respective DataType of each column, which determines how a field is validated, modified, and formatted
    //	This is a List as it may change from its initial length (e.g. if the passed value is too short)
    public List<DataType> ColumnTypes { get; set; } = new List<DataType>();

    //The string used to separate fields in the input
    public string Delimiter { get; set; } = ",";

    //The number of hours (positive or negative) by which to offset any fields of DataType 'Timestamp'
    public int TimestampOffset { get; set; } = 0;

    //Format specifier to define text representation of fields of DataType 'Timestamp'; defaults to ISO-8601 compliant
    //See: https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
    public string TimestampOutputFormat { get; set; } = "s";

    #endregion PROPERTIES
    #region PRIVATE MEMBERS

    //Header labels, as read from the first row; this is used to guess columnTypes if not provided
    private string[] headers;

    #endregion PRIVATE MEMBERS
    #region CONSTRUCTORS

    //Default Constructor
    public CsvNormalizer()
    {
        //Nothing else to do; properties already initialized in respective definitions
    }

    //Constructor to set some or all necessary properties
    public CsvNormalizer(List<DataType> colTypes = null, string delim = ",", int dateTimeOffset = 0, string dateTimeOutputFormat = "s")
    {
        ColumnTypes = colTypes != null ? colTypes : new List<DataType>();
        Delimiter = delim;
        TimestampOffset = dateTimeOffset;
        TimestampOutputFormat = dateTimeOutputFormat;
    }

    #endregion CONSTRUCTORS
    #region PUBLIC METHODS

	//Reads in entire stream one record at a time, validates/modifies/formats fields as required, outputs one record at a time
    public void NormalizeCsv(TextReader inStream, TextWriter outStream,
        TextWriter errorStream = null)
    {
        if (errorStream == null)
        {
            errorStream = outStream;
        }

        using (CsvWriter csvWriter = new CsvWriter(outStream))
        using (CsvParser csvParser = new CsvParser(inStream))
        {
            csvParser.Configuration.Delimiter = Delimiter;

            string[] record = csvParser.Read();

            if (record != null)
            {
                headers = record;

                //Send headers back to output
                for (int i = 0; i < headers.Length; i++)
                {
                    csvWriter.WriteField(headers[i]);
                }
                csvWriter.NextRecordAsync();
            }
            else
            {
                errorStream.WriteLine("Error: No input to process.");
                return;
            }

            // Read current line fields, pointer moves to the next line.
            record = csvParser.Read();

            while (record != null)
            {
                string[] parsedRecord = new string[record.Length];

                //Before parsing fields, be sure we know (or have tried to guess) the type of each field
                //This will normally run at most once, on line #2 (first line of data), unless later records have more columns
                if (ColumnTypes.Count < headers.Length)
                {
                    errorStream.WriteLine("Warning: Some or all column data types were not specified");

                    for (int i = ColumnTypes.Count; i < headers.Length; i++)
                    {
                        ColumnTypes.Add(GuessDataType(record[i], headers[i]));
                    }
                }

                try
                {
                    for (int i = 0; i < record.Length; i++)
                    {
                        switch (ColumnTypes[i])
                        {
                            case DataType.Timestamp:
                                parsedRecord[i] = ProcessTimestamp(record[i]);
                                break;
                            case DataType.ZipCode:
                                parsedRecord[i] = ProcessZipCode(record[i]);
                                break;
                            case DataType.FullName:
                                parsedRecord[i] = ProcessFullName(record[i]);
                                break;
                            case DataType.Duration:
                                parsedRecord[i] = ProcessDuration(record[i]);
                                break;
                            case DataType.TotalDuration:
                                //Build a list of all fields of DataType 'Duration' preceding this one
                                List<string> durations = new List<string>();
                                for (int j = 0; j < i; j++)
                                {
                                    if (ColumnTypes[j] == DataType.Duration)
                                    {
                                        durations.Add(parsedRecord[j]);
                                    }
                                }

                                parsedRecord[i] = ProcessTotalDuration(durations);
                                break;
                            case DataType.UnmodifiedString:
                                //Validation already done, no conversion necessary
								parsedRecord[i] = record[i];
                                break;
                            default:
                                //In theory, this line will only be called if a new datatype is added to the enum and
                                //	passed to the function, but not handled in the Switch statement
                                throw (new MissingMethodException("DataType not handled"));
                                //break; //Commenting out to eliminate VSCode warning, leaving in to show it was not forgotten
                        } //switch
                    } //for

                    //Now that all fields are validated, write them all to outStream
                    for (int i = 0; i < record.Length; i++)
                    {
                        //This will add quotes around the field if it contains the delimiter, then
                        //	convert from UTF-16 (.NET string encoding) to encoding specified for outStream, 
                        csvWriter.WriteField(parsedRecord[i]);
                    }

                    csvWriter.NextRecord();

                } //try
                catch (Exception e)
                {
                    //Output error, including original data and Exception message
                    errorStream.WriteLine("Record discarded due to Critical Error: <" +
                        string.Join(",", record) + ">  (Error message: " +
                        e.Message + ")");

                    //If exception is thrown because a DataType is not handled, every line will
                    //	fail. No need to spit out the same error over and over.
                    if (e is MissingMethodException)
                    {
                        throw;
                    }
                } //catch

                //CsvParser converts the input using .toString(), which implements the built-in
                //	type converter based on the specified character encoding of inStream
                //	(This will correct any invalid characters, then load into a UTF-16 string)
                record = csvParser.Read();

            } //while
        } //using
    } //NormalizeCsv()

    #endregion PUBLIC METHODS
    #region PRIVATE METHODS

    //Adjust DateTime with an offset, then format
	//TODO: Account for time zone in input string, set to DefaultTimezone (add this as class public property) if not specified in string;
	//TODO: Instead of AddHours, request DateTime for NewTimezone
    private string ProcessTimestamp(string inString)
    {
        return DateTime.Parse(inString).AddHours(TimestampOffset).ToString(TimestampOutputFormat);
    }

    //Zip code may be at most 5 digits, but will pad with leading zeros if shorter
    //TODO If ZIP+4, concatenate
    private string ProcessZipCode(string inString)
    {
        //Will throw exception if not numeric
        uint zipInt = uint.Parse(inString);

        if (zipInt > 99999)
        {
            throw new ArgumentOutOfRangeException(inString, "Zip Code has too many digits");
        }

        //Return 5-digit zip code, padding with leading zeros if necessary
        return zipInt.ToString("D5");
    }

    //Capitalizes First Letter Of Each Word
    private string ProcessFullName(string inString)
    {
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        return textInfo.ToTitleCase(inString.ToLower());
    }

    //Converts HH:MM:SS.MS to floating-point seconds
	//Note: TimeSpan.Parse throws an exception if hours>23, even if no days are specified; using RegEx instead
	//TODO This function is awful. Fix it.
    private string ProcessDuration(string inString)
    {
		String pattern = @"^(\d*?)([:\.]?)(\d*?)([:\.]?)(\d*?)([:\.]?)(\d*?)([:\.]?)(\d+)$";
		Match match = Regex.Match(inString, pattern);
		if(!match.Success)
		{
			throw(new FormatException("Cannot parse " + inString + " as a Duration"));
		}

		int ms = 0;
		int ss = 0;
		int mm = 0;
		int hh = 0;
		int dd = 0;
		int parseindex = match.Groups.Count-1;

		if(parseindex > 1 && string.Compare(match.Groups[parseindex-1].Value, ".") == 0)
		{
			int.TryParse(match.Groups[parseindex].Value, out ms);
			parseindex -= 2;
		}
		if(parseindex >= 1)
		{
			int.TryParse(match.Groups[parseindex].Value, out ss);
			parseindex -= 2;
		}
		if(parseindex >= 1)
		{
			int.TryParse(match.Groups[parseindex].Value, out mm);
			parseindex -= 2;
		}
		if(parseindex >= 1)
		{
			int.TryParse(match.Groups[parseindex].Value, out hh);
			parseindex -= 2;
		}
		if(parseindex >= 1)
		{
			int.TryParse(match.Groups[parseindex].Value, out dd);
			parseindex -= 2;
		}

		TimeSpan duration = new TimeSpan(dd, hh, mm, ss, ms);
		
		//TimeSpan duration = TimeSpan.Parse(inString);
        double totalSeconds = duration.TotalSeconds;

        return totalSeconds.ToString();
    }

    //Adds all durations passed to it; if any duration cannot be converted to a valid float, an exception is thrown
    private string ProcessTotalDuration(List<string> durations)
    {
        double totalDuration = 0;

        foreach (string duration in durations)
        {
            totalDuration += double.Parse(duration);
        }
        return totalDuration.ToString();
    }


    private DataType GuessDataType(string value, string fieldName)
    {
        //TODO Guess datatype based on criteria:
        /*
			ZipCode: (1-5 digits, OR 5 digits + 1 hyphen + 4 digits) AND fieldname contains "zip"
			Duration: Groups of 1-2 digits separated by colon or decimal, one decimal at most, decimal last
			Timestamp: 3 groups of 1-4 digits separated by slashes (only one group may have 4, others must have 1-2), then whitespace, then 2-3 groups of 1-2 digits separated by colons. May end with 0-2 words up to three characters (e.g. AM/PM, EST/CDT, etc)
			FullName: 1-3 words, fieldname contains "name"
			UnmodifiedString: Default
		*/
        //Note: Cannot guess TotalDuration field without checking input value to be sum of previous durations, and fieldName containing "Total" or "Sum"

		//TODO Following line is a placeholder
        DataType retval = DataType.UnmodifiedString;

        return retval;
    }

    #endregion PRIVATE METHODS
} //CsvNormalizer
﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace TrussWorks_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding inputEncoding = Encoding.UTF8;
            Encoding outputEncoding = Encoding.UTF8;
            Encoding errorEncoding = Encoding.UTF8;

            CsvNormalizer normalizer = new CsvNormalizer();

            Console.OutputEncoding = outputEncoding;


            using (StreamReader inStream = new StreamReader(Console.OpenStandardInput(), inputEncoding))
            //using( StreamReader inStream = new StreamReader("./Instructions/DB-sample-with-broken-utf8.csv", inputEncoding) )
            using (StreamWriter outStream = new StreamWriter(Console.OpenStandardOutput(), outputEncoding))
            //using (StreamWriter outStream = new StreamWriter("./Instructions/DB-sample-with-broken-utf8.out.csv", false, outputEncoding) )
            using (StreamWriter errStream = new StreamWriter(Console.OpenStandardError(), errorEncoding))
            //using (StreamWriter outStream = new StreamWriter("./Instructions/DB-sample-with-broken-utf8.err.csv", false, outputEncoding) )
            {
                //Columns: Timestamp,Address,ZIP,FullName,FooDuration,BarDuration,TotalDuration,Notes
                List<CsvNormalizer.DataType> columnTypes = new List<CsvNormalizer.DataType>()
                    {CsvNormalizer.DataType.Timestamp,
                    CsvNormalizer.DataType.UnmodifiedString,
                    CsvNormalizer.DataType.ZipCode,
                    CsvNormalizer.DataType.FullName,
                    CsvNormalizer.DataType.Duration,
                    CsvNormalizer.DataType.Duration,
                    CsvNormalizer.DataType.TotalDuration,
                    CsvNormalizer.DataType.UnmodifiedString};

                try
                {
                    normalizer.ColumnTypes = columnTypes;
                    normalizer.Delimiter = ",";
                    normalizer.TimestampOffset = 3;
                    normalizer.TimestampOutputFormat = "s";

                    normalizer.NormalizeCsv(inStream, outStream, outStream);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}

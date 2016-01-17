using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HFT.Model;

namespace HFT.FileProcessing
{
    class Parser
    {
        public string Path { get; set; }

        public List<RawDataModel> ReadFile(string path)
        {
            Path = path;

            var logFile = new List<RawDataModel>();

            using (var sr = new StreamReader(Path))
            {
                while (sr.Peek() >= 0 && sr.Peek() != '\0')
                {
                    var c = new char[76];
                    sr.Read(c, 0, c.Length);


                    var symbol = new String(c, 0, 16);
                    var status = new String(c, 16, 1);
                    var date = new String(c, 17, 8);
                    var updateTime = new String(c, 25, 6).Replace(' ', '0');
                    var referencedPrice = new String(c, 31, 15);
                    var orderType = new String(c, 46, 1);
                    var pricePoint = new String(c, 47, 15);
                    var shares = new String(c, 62, 9);
                    var numberOfOrders = new String(c, 71, 5);
       
                    var logEntry = new RawDataModel(
                        symbol, 
                        status, 
                        DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None),
                        DateTime.ParseExact(updateTime, "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None),
                        double.Parse(referencedPrice, CultureInfo.InvariantCulture),
                        int.Parse(orderType),
                        double.Parse(pricePoint, CultureInfo.InvariantCulture),
                        int.Parse(shares),
                        int.Parse(numberOfOrders));

                    logFile.Add(logEntry);
                }
            }

            return logFile;
        }
    }
}

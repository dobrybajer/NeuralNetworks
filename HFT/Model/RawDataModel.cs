using System;

namespace HFT.Model
{
    class RawDataModel
    {
        public string Symbol { get; set; }

        public string Status { get; set; }

        public DateTime Date { get; set; }

        public DateTime UpdateTime { get; set; }

        public double ReferencedPrice { get; set; }

        public int OrderType { get; set; }

        public double PricePoint { get; set; }

        public int Shares { get; set; }

        public int NumberOfOrders { get; set; }

        public int sellClass { get; set; }

        public int buyClass { get; set; }

        public int Group { get; set; }

        public RawDataModel(
            string symbol, 
            string status, 
            DateTime date, 
            DateTime updateTime, 
            double referencedPrice,
            int orderType,
            double pricePoint,
            int shares,
            int numberOfOrders)
        {
            Symbol = symbol;
            Status = status;
            Date = date;
            UpdateTime = updateTime;
            ReferencedPrice = referencedPrice;
            OrderType = orderType;
            PricePoint = pricePoint;
            Shares = shares;
            NumberOfOrders = numberOfOrders;
            sellClass = -1;
            buyClass = -1;
            Group = -1;
        }
    }
}

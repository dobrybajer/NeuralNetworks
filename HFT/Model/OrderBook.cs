using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFT.Model
{
    class OrderBook
    {
        List<RawDataModel> sellOffer { get; set; }
        List<RawDataModel> buyOffer  { get; set; }

        public OrderBook(List<RawDataModel> SellOffer, List<RawDataModel> BuyOffer)
        {
            sellOffer = SellOffer;
            buyOffer = BuyOffer;
        }
    }
}

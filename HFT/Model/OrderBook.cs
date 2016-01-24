using System.Collections.Generic;

namespace HFT.Model
{
    class OrderBook
    {
        List<RawDataModel> SellOffer { get; set; }

        List<RawDataModel> BuyOffer  { get; set; }

        public OrderBook(List<RawDataModel> sellOffer, List<RawDataModel> buyOffer)
        {
            SellOffer = sellOffer;
            BuyOffer = buyOffer;
        }
    }
}

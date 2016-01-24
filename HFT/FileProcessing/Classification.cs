using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HFT.Model;

namespace HFT.FileProcessing
{
    class Classification
    {
        public List<RawDataModel> AddClasses(List<RawDataModel> traiSet)
        {
            int time = 5;
            var indexFirst = traiSet.First(x => x.Status == "T");
            var indexLast = traiSet[traiSet.Count - 1];
            DateTime endTime = indexFirst.UpdateTime;
            endTime = endTime.AddMinutes(5);
            var counterSet = (indexLast.UpdateTime.Hour - indexFirst.UpdateTime.Hour) * 60 + (indexLast.UpdateTime.Minute - indexFirst.UpdateTime.Minute);
            var counter = 1;

            //zmienne dla sell
            Double[] sellMidlePrice = new Double[(int)Math.Ceiling((double)counterSet / time) + 1];
            int[] sellRecordCount = new int[(int)Math.Ceiling((double)counterSet / time) + 1];
            int[] sellClassForTime = new int[(int)Math.Ceiling((double)counterSet / time)];

            //zmienne dla buy
            Double[] buyMidlePrice = new Double[(int)Math.Ceiling((double)counterSet / time) + 1];
            int[] buyRecordCount = new int[(int)Math.Ceiling((double)counterSet / time) + 1];
            int[] buyClassForTime = new int[(int)Math.Ceiling((double)counterSet / time)];

            foreach (var el in traiSet)
            {
                if (el.Status == "1") //dane po nocnej zmianie
                {
                    if (el.OrderType == 1)
                    {  //dla sell
                        sellMidlePrice[0] += el.PricePoint;
                        sellRecordCount[0] += 1;
                        el.Group = 0;
                    }
                    else
                    {  //dla buy
                        buyMidlePrice[0] += el.PricePoint;
                        buyRecordCount[0] += 1;
                        el.Group = 0;
                    }
                }
                else
                {
                    if (DateTime.Compare(el.UpdateTime, endTime) < 0)
                    {
                        if (el.OrderType == 1)
                        {
                            sellMidlePrice[counter] += el.PricePoint;
                            sellRecordCount[counter] += 1;
                            el.Group = counter;
                        }
                        else
                        {
                            buyMidlePrice[counter] += el.PricePoint;
                            buyRecordCount[counter] += 1;
                            el.Group = counter;
                        }
                    }
                    else
                    {
                        if (el.OrderType == 1)
                        {
                            counter++;
                            endTime = endTime.AddMinutes(time);
                            sellMidlePrice[counter] += el.PricePoint;
                            sellRecordCount[counter] += 1;
                            el.Group = counter;
                        }
                        else
                        {
                            counter++;
                            endTime = endTime.AddMinutes(time);
                            buyMidlePrice[counter] += el.PricePoint;
                            buyRecordCount[counter] += 1;
                            el.Group = counter;
                        }

                    }
                }
            }
            //wyliczamy srednią dla każdej grupy
            for (int i = 0; i < sellMidlePrice.Length; i++)
            {
                if (sellRecordCount[i] != 0)
                    sellMidlePrice[i] = sellMidlePrice[i] / sellRecordCount[i];
                else
                    sellMidlePrice[i] = 0;
            }

            for (int i = 0; i < buyMidlePrice.Length; i++)
            {
                if (buyRecordCount[i] != 0)
                    buyMidlePrice[i] = buyMidlePrice[i] / buyRecordCount[i];
                else
                    buyMidlePrice[i] = 0;

            }

            //przypisujemy klase na podstawie średniej
            for (int i = 0; i < sellClassForTime.Length; i++)
            {
                if (sellMidlePrice[i] < sellMidlePrice[i + 1] && sellMidlePrice[i + 1] != 0)
                    sellClassForTime[i] = 2; //wzrost
                else if (sellMidlePrice[i] > sellMidlePrice[i + 1] && sellMidlePrice[i + 1] != 0)
                    sellClassForTime[i] = 0; // spadek
                else
                    sellClassForTime[i] = 1; //stały
            }

            for (int i = 0; i < buyClassForTime.Length; i++)
            {
                if (buyMidlePrice[i] < buyMidlePrice[i + 1] && buyMidlePrice[i + 1] != 0)
                    buyClassForTime[i] = 2; //wzrost
                else if (buyMidlePrice[i] > buyMidlePrice[i + 1] && buyMidlePrice[i + 1] != 0)
                    buyClassForTime[i] = 0; // spadek
                else
                    buyClassForTime[i] = 1; //stały
            }

            //Finalne przypisanie kalsy do rekordu
            foreach (var el in traiSet)
                if (el.Group < sellClassForTime.Length)
                    if (el.OrderType == 1)
                    {
                        el.sellClass = sellClassForTime[el.Group];
                        // sell
                    }
                    else
                        el.buyClass = buyClassForTime[el.Group];

            return traiSet;
        }

    }
}

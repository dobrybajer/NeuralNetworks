using System;
using System.Collections.Generic;
using System.Linq;
using HFT.Model;

namespace HFT.FileProcessing
{
    internal class Classification
    {
        public List<RawDataModel> AddClasses(List<RawDataModel> trainSet)
        {
            const int time = 5; // TODO parametr okno czasowe

            var indexFirst = trainSet.FirstOrDefault(x => x.Status == "T") ?? trainSet.First();
            var indexLast = trainSet[trainSet.Count - 1];

            var endTime = indexFirst.UpdateTime;
            endTime = endTime.AddMinutes(time);

            var counterSet = (indexLast.UpdateTime.Hour - indexFirst.UpdateTime.Hour)*60 +
                             (indexLast.UpdateTime.Minute - indexFirst.UpdateTime.Minute);
            var counter = 1;

            //zmienne dla sell
            var sellMiddlePrice = new Double[(int) Math.Ceiling((double) counterSet/time) + 1];
            var sellRecordCount = new int[(int) Math.Ceiling((double) counterSet/time) + 1];
            var sellClassForTime = new int[(int) Math.Ceiling((double) counterSet/time)];

            //zmienne dla buy
            var buyMidlePrice = new Double[(int) Math.Ceiling((double) counterSet/time) + 1];
            var buyRecordCount = new int[(int) Math.Ceiling((double) counterSet/time) + 1];
            var buyClassForTime = new int[(int) Math.Ceiling((double) counterSet/time)];

            foreach (var el in trainSet)
            {
                if (el.Status == "1") //dane po nocnej zmianie
                {
                    if (el.OrderType == 1)
                    {
                        //dla sell
                        sellMiddlePrice[0] += el.PricePoint;
                        sellRecordCount[0] += 1;
                        el.Group = 0;
                    }
                    else
                    {
                        //dla buy
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
                            sellMiddlePrice[counter] += el.PricePoint;
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
                            sellMiddlePrice[counter] += el.PricePoint;
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
            for (var i = 0; i < sellMiddlePrice.Length; i++)
            {
                if (sellRecordCount[i] != 0)
                    sellMiddlePrice[i] = sellMiddlePrice[i]/sellRecordCount[i];
                else
                    sellMiddlePrice[i] = 0;
            }

            for (var i = 0; i < buyMidlePrice.Length; i++)
            {
                if (buyRecordCount[i] != 0)
                    buyMidlePrice[i] = buyMidlePrice[i]/buyRecordCount[i];
                else
                    buyMidlePrice[i] = 0;

            }

            //przypisujemy klase na podstawie średniej
            for (var i = 0; i < sellClassForTime.Length; i++)
            {
                if (sellMiddlePrice[i] < sellMiddlePrice[i + 1] && sellMiddlePrice[i + 1] != 0)
                    sellClassForTime[i] = 2; //wzrost
                else if (sellMiddlePrice[i] > sellMiddlePrice[i + 1] && sellMiddlePrice[i + 1] != 0)
                    sellClassForTime[i] = 0; // spadek
                else
                    sellClassForTime[i] = 1; //stały
            }

            for (var i = 0; i < buyClassForTime.Length; i++)
            {
                if (buyMidlePrice[i] < buyMidlePrice[i + 1] && buyMidlePrice[i + 1] != 0)
                    buyClassForTime[i] = 2; //wzrost
                else if (buyMidlePrice[i] > buyMidlePrice[i + 1] && buyMidlePrice[i + 1] != 0)
                    buyClassForTime[i] = 0; // spadek
                else
                    buyClassForTime[i] = 1; //stały
            }

            //Finalne przypisanie kalsy do rekordu
            foreach (var el in trainSet.Where(el => el.Group < sellClassForTime.Length))
                if (el.OrderType == 1)          
                    el.SellClass = sellClassForTime[el.Group]; 
                else
                    el.BuyClass = buyClassForTime[el.Group];

            return trainSet;
        }
    }
}

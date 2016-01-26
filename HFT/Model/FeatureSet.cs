using System;
using System.Collections.Generic;
using System.Linq;
using Encog.Neural.Data.Basic;
using HFT.Logic;

namespace HFT.Model
{
    class FeatureSet
    {
        #region Parameters

        private int GroupSize { get; set; }

        private int SlideWindow { get; set; }

        private double ValidationSetSize { get; set; }

        #endregion

        #region Variables

        private int[] VSizes { get; set; }

        public int Size { get; set; }

        private IEnumerable<RawDataModel> TrainingModel { get; set; }

        private IEnumerable<RawDataModel> TestModel { get; set; }

        private List<double[,]> Vector1 { get; set; }

        private List<double[,]> Vector2 { get; set; }

        private List<double[,]> Vector3 { get; set; }

        private List<double[]> Vector4 { get; set; }

        private List<double[]> Vector5 { get; set; }

        private int[] ClassCounter { get; set; }

        private double[][] Input { get; set; }

        private double[][] Output { get; set; }

        #endregion

        #region Constructor

        public FeatureSet(IEnumerable<RawDataModel> trainingModel, IEnumerable<RawDataModel> testModel, Parameters parameters, int[] size = null)
        {
            GroupSize = parameters.GroupSize;
            SlideWindow = parameters.SlideWindow;
            ValidationSetSize = parameters.ValidationSetSize;

            TrainingModel = trainingModel;
            TestModel = testModel;
            VSizes = size ?? new[] { 4, 2, 4, 4, 2 };
            Size = VSizes.Sum() * GroupSize;
        }

        #endregion

        #region Public Properties

        public void LoadTrainingData(ref BasicNeuralDataSet trainingSet, ref BasicNeuralDataSet validationSet)
        {
            ParseTrainingModel(TrainingModel);

            var trainSetCount = (int)(Input.Count() * ((100.0 - ValidationSetSize) / 100));

            Input.Shuffle();
            Output.Shuffle();
            MyExtensions.ResetStableShuffle();

            trainingSet = new BasicNeuralDataSet(Input.Take(trainSetCount).ToArray(), Output.Take(trainSetCount).ToArray());
            validationSet = new BasicNeuralDataSet(Input.Skip(trainSetCount).ToArray(), Output.Skip(trainSetCount).ToArray());
        }

        public void LoadTestData(ref double[][] testSet, ref double[][] idealTestOutput)
        {
            ParseTrainingModel(TestModel);

            testSet = Input;
            idealTestOutput = Output;
        }

        #endregion

        #region Private Properties

        private void ParseTrainingModel(IEnumerable<RawDataModel> model)
        {
            var sellOffer = new List<RawDataModel>();
            var buyOffer = new List<RawDataModel>();

            foreach (var rec in model)
            {
                if (rec.OrderType == 1)
                    sellOffer.Add(rec);
                else
                    buyOffer.Add(rec);
            }

            var counter = sellOffer.Count <= buyOffer.Count ? sellOffer.Count : buyOffer.Count; // wybieramy ktorych jest wiecej

            Vector1 = new List<double[,]>();
            Vector2 = new List<double[,]>();
            Vector3 = new List<double[,]>();
            Vector4 = new List<double[]>();
            Vector5 = new List<double[]>();

            Output = new double[counter - GroupSize][];

            for (var i = GroupSize; i < counter; i += SlideWindow)
            {
                var newRecord1 = new double[GroupSize, VSizes[0]];
                var newRecord2 = new double[GroupSize, VSizes[1]];
                var newRecord3 = new double[GroupSize, VSizes[2]];
                var newRecord4 = new double[VSizes[3]];
                var newRecord5 = new double[VSizes[4]];

                ClassCounter = new[] {0, 0, 0};

                for (var j = 0; j < GroupSize; j++)
                {
                    newRecord1[j, 0] = sellOffer[i - j].PricePoint;
                    newRecord1[j, 1] = sellOffer[i - j].Shares;
                    newRecord1[j, 2] = buyOffer[i - j].PricePoint;
                    newRecord1[j, 3] = buyOffer[i - j].Shares;

                    newRecord2[j, 0] = sellOffer[i - j].PricePoint - buyOffer[i - j].PricePoint;
                    newRecord2[j, 1] = (sellOffer[i - j].PricePoint + buyOffer[i - j].PricePoint) / 2;

                    newRecord3[j, 0] = sellOffer[i - GroupSize].PricePoint - sellOffer[i].PricePoint;
                    newRecord3[j, 1] = buyOffer[i].PricePoint - buyOffer[i - GroupSize].PricePoint;
                    newRecord3[j, 2] = Math.Abs(sellOffer[(i - j - 1) < 0 ? 0 : i - j - 1].PricePoint - sellOffer[i - j].PricePoint);
                    newRecord3[j, 3] = Math.Abs(buyOffer[(i - j - 1) < 0 ? 0 : i - j - 1].PricePoint - buyOffer[i - j].PricePoint);

                    newRecord4[0] += sellOffer[i - j].PricePoint;
                    newRecord4[1] += buyOffer[i - j].PricePoint;
                    newRecord4[2] += sellOffer[i - j].Shares;
                    newRecord4[3] += buyOffer[i - j].Shares;

                    newRecord5[0] += sellOffer[i - j].PricePoint - buyOffer[i - j].PricePoint;
                    newRecord5[1] += sellOffer[i - j].Shares - buyOffer[i - j].Shares;

                    if (sellOffer[i - j].SellClass >= 0) ClassCounter[sellOffer[i - j].SellClass]++;
                    if (sellOffer[i - j].BuyClass >= 0) ClassCounter[sellOffer[i - j].BuyClass]++;
                }

                Vector1.Add(newRecord1);
                Vector2.Add(newRecord2);
                Vector3.Add(newRecord3);

                for (var j = 0; j < VSizes[3]; j++)
                    newRecord4[j] = newRecord4[j] / GroupSize;

                Vector4.Add(newRecord4);
                Vector5.Add(newRecord5);

                Output[i - GroupSize] = SetClass(); //sprawdzić
            }

            Input = ParseVectorsToInputVector(); //składamy wektory wejściowe
        }

        private double[] SetClass()
        {
            var maxValue = ClassCounter.Max();
            var maxIndex = ClassCounter.ToList().IndexOf(maxValue);

            var output = new double[] { 0, 0, 0 };
            output[maxIndex] = 1;

            return output;
        }

        private double[][] ParseVectorsToInputVector()
        {
            var final = new double[Vector1.Count][];

            for (var i = 0; i < Vector1.Count; i++)
            {
                final[i] = new double[Size];

                for (var j = 0; j < GroupSize; j++)
                {
                    final[i][j * 16 + 0] = Vector1[i][j, 0];
                    final[i][j * 16 + 1] = Vector1[i][j, 1];
                    final[i][j * 16 + 2] = Vector1[i][j, 2];
                    final[i][j * 16 + 3] = Vector1[i][j, 3];

                    final[i][j * 16 + 4] = Vector2[i][j, 0];
                    final[i][j * 16 + 5] = Vector2[i][j, 1];

                    final[i][j * 16 + 6] = Vector3[i][j, 0];
                    final[i][j * 16 + 7] = Vector3[i][j, 1];
                    final[i][j * 16 + 8] = Vector3[i][j, 2];
                    final[i][j * 16 + 9] = Vector3[i][j, 3];

                    final[i][j * 16 + 10] = Vector4[i][0];
                    final[i][j * 16 + 11] = Vector4[i][1];
                    final[i][j * 16 + 12] = Vector4[i][2];
                    final[i][j * 16 + 13] = Vector4[i][3];

                    final[i][j * 16 + 14] = Vector5[i][0];
                    final[i][j * 16 + 15] = Vector5[i][1];
                }
            }

            var currentRow = new double[Vector1.Count];
            // TODO mozliwe usprawnienie
            for (var i = 0; i < Size; i++) //liczba kolumn
            {
                for (var j = 0; j < Vector1.Count; j++)
                    currentRow[j] = final[j][i];

                currentRow = Normalization(currentRow);

                for (var j = 0; j < Vector1.Count; j++)
                    final[j][i] = currentRow[j];
            }

            return final;
        }

        private static double[] Normalization(double[] values)
        {
            var max = values.Max();
            var min = values.Min();
            var means = values.Average();

            var factor = Math.Abs(max - means) > Math.Abs(min - means) ? Math.Abs(max - means) : Math.Abs(min - means);

            for (var i = 0; i < values.Length; i++)
                values[i] = (values[i] - means) / factor;

            return values;
        }

        #endregion
    }
}

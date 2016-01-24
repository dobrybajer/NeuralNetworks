using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using Encog;
using Encog.Engine.Network.Activation;
using Encog.ML.Data;
using Encog.Neural.Data.Basic;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training.Propagation.Back;
using HFT.FileProcessing;
using HFT.Model;

namespace HFT.Logic
{
    class ProblemBase
    {
        #region Parameters

        const int GroupSize = 10; // TODO parametr
        const int SlideWindow = 1; // TODO parametr

        public Parameters Parameters { get; set; }

        public Network Network { get; set; }

        public BasicNeuralDataSet TrainingSet; // TODO add real training set

        public BasicNeuralDataSet ValidationSet;  // TODO add real validation set

        public double[][] ErrorSet;

        public double[][] TestSet; // TODO add real test set

        public BasicNeuralDataSet ResultTestSet;

        public OrderBook OrderBook;

        public List<double[,]> Vector1;

        public List<double[,]> Vector2;

        public List<double[,]> Vector3;

        public List<double[]> Vector4;

        public List<double[]> Vector5;

        public  int Class1Counter;

        public int Class2Counter;

        public int Class0Counter;

        //public List<double[,]> vector6;

        //public List<double[,]> vector7;

        //public List<double[,]> vector8;

        //public List<double[,]> vector9;

        #endregion

        #region Constructors

        public ProblemBase()
        {

        }

        public ProblemBase(Parameters parameters)
        {
            Parameters = parameters;

            Initialize();
        }

        private void Initialize()
        {
            Network = new Network();
        }

        #endregion

        #region Network actions

        public virtual void CreateNetwork()
        {
            Network.AddMainLayer(true);

            foreach (var neurons in Parameters.Layers)
            {
                Network.AddHiddenLayer(new BasicLayer(new ActivationSigmoid(), Parameters.HasBias, neurons));
            }

            Network.AddMainLayer(false);

            Network.Model.Structure.FinalizeStructure();
            Network.Model.Reset();
        }

        public virtual void TrainNetwork()
        {
            var train = new Backpropagation(Network.Model, TrainingSet, Parameters.LearingCoefficient, Parameters.InertiaCoefficient)
            {
                BatchSize = 1
            };

            var iteration = 1;

            var errors = new List<double[]>();

            do
            {
                train.Iteration();

                var validationError = Network.Model.CalculateError(ValidationSet);

                errors.Add(new[] { iteration, train.Error, validationError });

                Console.WriteLine(@"Iteration #" + iteration++ + @" Training error:" + train.Error + @", Validation error:" + validationError);

            } while ((iteration < Parameters.IterationsCount) && (train.Error > Parameters.AcceptedError));

            train.FinishTraining();

            ErrorSet = errors.ToArray();
        }

        public void CalculateResult()
        {
            var values = new List<double[]>();
            var result = new List<double[]>();

            foreach (var input in TestSet)
            {
                var output = new double[Network.OutputSize];
                Network.Model.Compute(input, output);

                values.Add(input);
                result.Add(output);
            }

            ResultTestSet = new BasicNeuralDataSet(values.ToArray(), result.ToArray());
        }

        #endregion

        #region Creating feature set

        private void ParseTrainingModel(IEnumerable<RawDataModel> trainingModel)
        {
            var sellOffer = new List<RawDataModel>();
            var buyOffer = new List<RawDataModel>();

            foreach (var rec in trainingModel)
            {
                if (rec.OrderType == 1)
                    sellOffer.Add(rec);
                else
                    buyOffer.Add(rec);
            }

            OrderBook = new OrderBook(sellOffer, buyOffer);

            var counter = sellOffer.Count <= buyOffer.Count ? sellOffer.Count : buyOffer.Count; //wybieramy ktorych jest wiecej

            Vector1 = new List<double[,]>();
            Vector2 = new List<double[,]>();
            Vector3 = new List<double[,]>();
            Vector4 = new List<double[]>();
            Vector5 = new List<double[]>();
            //vector6 = new List<double[,]>();
            //vector7 = new List<double[,]>();
            //vector8 = new List<double[,]>();
            //vector9 = new List<double[,]>();

            var finalClass = new double[counter - GroupSize][];

            for (var i = GroupSize; i < counter; i += SlideWindow)
            {
                var newRecord1 = new double[GroupSize, 4];
                var newRecord2 = new double[GroupSize, 2];
                var newRecord3 = new double[GroupSize, 4];
                var newRecord4 = new double[4];
                var newRecord5 = new double[2];

                Class0Counter = 0;
                Class1Counter = 0;
                Class2Counter = 0;

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

                    UpClassCounter(sellOffer[i - j].SellClass);
                    UpClassCounter(sellOffer[i - j].BuyClass);
                }

                Vector1.Add(newRecord1);
                Vector2.Add(newRecord2);
                Vector3.Add(newRecord3);

                for (var j = 0; j < 4; j++)
                    newRecord4[j] = newRecord4[j] / GroupSize;

                Vector4.Add(newRecord4);
                Vector5.Add(newRecord5);

                finalClass[i - GroupSize] = SetClass(); //sprawdzić
            }

            Network.UpdateSizeInfo(10 * (Vector1.Count() + Vector2.Count() + Vector3.Count() + Vector4.Count() + Vector5.Count()), 3); // TODO check

            var final = ParseVectorsToInputVector(); //składamy wektory wejściowe

            var trainSetCount = (int)(final.Count() * ((100.0 - 15) / 100));

            final.Shuffle();
            finalClass.Shuffle();
            MyExtensions.ResetStableShuffle();

            TrainingSet = new BasicNeuralDataSet(final.Take(trainSetCount).ToArray(), finalClass.Take(trainSetCount).ToArray());
            ValidationSet = new BasicNeuralDataSet(final.Skip(trainSetCount).ToArray(), finalClass.Skip(trainSetCount).ToArray());
        }

        private double[] SetClass()
        {
            var classes = new[] { Class0Counter, Class1Counter, Class2Counter };
            var maxValue = classes.Max();
            var maxIndex = classes.ToList().IndexOf(maxValue);

            var output = new double[] { 0, 0, 0 };
            output[maxIndex] = 1;

            return output;
        }

        void UpClassCounter(int currentClass)
        {
            switch (currentClass)
            {
                case 1:
                    Class1Counter++;
                    break;
                case 2:
                    Class2Counter++;
                    break;
                default:
                    Class0Counter++;
                    break;
            }
        }

        private double[][] ParseVectorsToInputVector()
        {
            var final = new double[Vector1.Count][];

            for (var i = 0; i < Vector1.Count; i++)
            {
                final[i] = new double[Network.InputSize]; // (4 + 2 + 4 + 4 + 2)*10

                for (var j = 0; j < 10; j++)
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

            for (var i = 0; i < Network.InputSize; i++) //liczba kolumn
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
            var s = values.Sum() / values.Length;

            var factor = Math.Abs(max - means) > Math.Abs(min - means) ? Math.Abs(max - means) : Math.Abs(min - means);

            for (var i = 0; i < values.Length; i++)
                values[i] = (values[i] - means) / factor;

            return values;
        }

        #endregion

        #region Run program

        public virtual void Execute(List<RawDataModel> trainingModel, List<RawDataModel> testModel)
        {
            ParseTrainingModel(trainingModel); // TODO calculate feature set (training)

            //LoadTestData(testModel); // TODO calculate feature set (test)

            CreateNetwork();

            TrainNetwork();

            //CalculateResult();

            //Save();

            EncogFramework.Instance.Shutdown();
        }

        #endregion

        // TODO sprawdzic zapisywanie
        #region Write results

        private void Save()
        {
            SaveErrorToFile();
            SaveResultToFile();

            Matlab.GenerateCharts();
        }

        private void SaveErrorToFile() // TODO check if works fine
        {
            var directoryPath = ConfigurationManager.AppSettings["PathToTestFiles"];
            const string errorSetPath = "error.csv";

            Matlab.ErrorPath = Path.Combine(directoryPath, errorSetPath);

            var errors = ErrorSet.Select(e =>
                    e[0].ToString(CultureInfo.InvariantCulture) + "," +
                    e[1].ToString(CultureInfo.InvariantCulture) + "," +
                    e[2].ToString(CultureInfo.InvariantCulture)); // TODO check if output agrees

            File.WriteAllLines(Matlab.ErrorPath, errors);
        }

        private void SaveResultToFile() // TODO check if works fine
        {
            var directoryPath = ConfigurationManager.AppSettings["PathToTestFiles"];
            const string resultSetPath = "result.csv";

            Matlab.ResultPath = Path.Combine(directoryPath, resultSetPath);

            var result = new List<string>();

            result.AddRange
                (
                    ResultTestSet.Select(t =>
                        t.Input[0].ToString(CultureInfo.InvariantCulture) + "," +
                        t.Input[1].ToString(CultureInfo.InvariantCulture) + "," +
                        MaxInArray(t.Ideal).ToString(CultureInfo.InvariantCulture)) // TODO check if output agrees
                );

            File.WriteAllLines(Matlab.ResultPath, result);
        }

        private double MaxInArray(IMLData result)
        {
            var array = new double[Network.OutputSize];
            result.CopyTo(array, 0, Network.OutputSize - 1);

            var maxValue = array.Max();
            var maxIndex = array.ToList().IndexOf(maxValue);

            return maxIndex + 1;
        }

        #endregion
    }
}

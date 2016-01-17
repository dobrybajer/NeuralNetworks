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

        public Parameters Parameters { get; set; }

        public Network Network { get; set; }

        public BasicNeuralDataSet TrainingSet; // TODO add real training set

        public BasicNeuralDataSet ValidationSet;  // TODO add real validation set

        public double[][] ErrorSet;

        public double[][] TestSet; // TODO add real test set

        public BasicNeuralDataSet ResultTestSet;

        public OrderBook orderBook;

        public List<double[,]> vector1;

        public List<double[,]> vector2;

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


        void ParseTrainingModel( List<RawDataModel> trainingModel)
        {

            List<RawDataModel> sellOffer = new List<RawDataModel>();
            List<RawDataModel> buyOffer = new List<RawDataModel>();

            foreach( RawDataModel rec in trainingModel)
            {
                if (rec.OrderType == 1)
                    sellOffer.Add(rec);
                else
                    buyOffer.Add(rec);
            }

            orderBook = new OrderBook(sellOffer, buyOffer);

            var counter = sellOffer.Count<=buyOffer.Count? sellOffer.Count : buyOffer.Count ;
            vector1 = new List<double[,]>();
            vector2 = new List<double[,]>();

            for (int i = 10; i < counter; i++)
            {
                var newRecord1 = new double[10, 4];
                var newRecord2 = new double[10, 2];
                for (int j = 0; j < 10; j++)
                {
                    newRecord1[j,0] = sellOffer[i - j].PricePoint;
                    newRecord1[j, 1] = sellOffer[i - j].Shares;
                    newRecord1[j, 2] = buyOffer[i - j].PricePoint;
                    newRecord1[j, 3] = buyOffer[i - j].Shares;

                    newRecord2[j, 0] = sellOffer[i - j].PricePoint - buyOffer[i - j].PricePoint;
                    newRecord2[j, 1] = (sellOffer[i - j].PricePoint + buyOffer[i - j].PricePoint)/2;


                }
                vector1.Add(newRecord1);
                vector2.Add(newRecord2);
                        
            }


            parseVectorsToInputVector();
           
        }

        void parseVectorsToInputVector()
        {
            double[][] final = new double[vector1.Count][];
            double[][] state = new double[ vector1.Count][];

            for (int i = 0; i < vector1.Count; i++)
            {
                final[i]= new double[(4 + 2) * 10];
                state[i] = new double[2];
                for (int j = 0; j < 10; j++)
                {
                     final[i][j * 6 + 0] = vector1[i][j, 0];
                     final[i][j * 6 + 1] = vector1[i][j, 1];
                     final[i][j * 6 + 2] = vector1[i][j, 2];
                     final[i][j * 6 + 3] = vector1[i][j, 3];

                     final[i][j * 6 + 4] = vector2[i][j, 0];
                     final[i][j * 6 + 5] = vector2[i][j, 1];

                     state[i][ 1] = 1;
                }

                state[i][1] = 1;
            }

            TrainingSet = new BasicNeuralDataSet(final, state);


        }

        #endregion

        #region Run program

        public virtual void Execute(List<RawDataModel> trainingModel, List<RawDataModel> testModel)
        {
            //LoadTrainingData(trainingModel); // TODO calculate feature set (training)
            //przerobic wczytane dane na wektory wejściowe

            ParseTrainingModel(trainingModel);

            //LoadTestData(testModel); // TODO calculate feature set (test)

            CreateNetwork();

            TrainNetwork();

            CalculateResult();

            Save();

            EncogFramework.Instance.Shutdown();
        }

        #endregion

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

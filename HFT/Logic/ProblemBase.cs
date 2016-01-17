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

        #endregion

        #region Constructors

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

        public void CreateNetwork()
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

        public void TrainNetwork()
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

        #region Run program

        public virtual void Execute(List<RawDataModel> trainingModel, List<RawDataModel> testModel)
        {
            //LoadTrainingData(trainingModel); // TODO calculate feature set (training)

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

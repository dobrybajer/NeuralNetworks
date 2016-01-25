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

        protected Parameters Parameters { get; set; }

        protected Network Network { get; set; }

        protected BasicNeuralDataSet TrainingSet;

        protected BasicNeuralDataSet ValidationSet;

        protected double[][] ErrorSet { get; set; }

        protected double[][] TestSet;

        protected BasicNeuralDataSet ResultTestSet { get; set; }

        protected double[][] IdealTestOutput;
        
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

        public virtual void CreateNetwork(int size)
        {
            Network.UpdateSizeInfo(size, 3);

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

                Console.WriteLine(
                    @"Iteration #" + iteration++ +
                    @" Training error:" + String.Format("{0:N10}", train.Error) +
                    @", Validation error:" + String.Format("{0:N10}", validationError));

            } while ((iteration < Parameters.IterationsCount) && (train.Error > Parameters.AcceptedError));

            train.FinishTraining();

            ErrorSet = errors.ToArray();
        }

        public virtual void CalculateResult()
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
            var featureSetGenerator = new FeatureSet(trainingModel, testModel, Parameters);

            featureSetGenerator.LoadTrainingData(ref TrainingSet, ref ValidationSet);

            featureSetGenerator.LoadTestData(ref TestSet, ref IdealTestOutput);

            CreateNetwork(featureSetGenerator.Size);

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

        private void SaveErrorToFile()
        {
            var directoryPath = ConfigurationManager.AppSettings["PathToTestFiles"];
            const string errorSetPath = "error.csv";

            Matlab.ErrorPath = Path.Combine(directoryPath, errorSetPath);

            var errors = ErrorSet.Select(e =>
                e[0].ToString(CultureInfo.InvariantCulture) + "," +
                e[1].ToString("n10",CultureInfo.InvariantCulture) + "," +
                e[2].ToString("n10", CultureInfo.InvariantCulture));

            File.WriteAllLines(Matlab.ErrorPath, errors);
        }

        private void SaveResultToFile()
        {
            var directoryPath = ConfigurationManager.AppSettings["PathToTestFiles"];
            const string resultSetPath = "result.csv";

            Matlab.ResultPath = Path.Combine(directoryPath, resultSetPath);

            var result = ResultTestSet.IdealSize > 2 ? 
                ResultTestSet.Select((t, i) => 
                t.Ideal[0].ToString(CultureInfo.InvariantCulture) + "," + 
                t.Ideal[1].ToString(CultureInfo.InvariantCulture) + "," + 
                t.Ideal[2].ToString(CultureInfo.InvariantCulture) + "," + 
                MaxInArray(t.Ideal).ToString(CultureInfo.InvariantCulture) + "," + 
                IdealTestOutput[i][0].ToString(CultureInfo.InvariantCulture) + "," + 
                IdealTestOutput[i][1].ToString(CultureInfo.InvariantCulture) + "," + 
                IdealTestOutput[i][2].ToString(CultureInfo.InvariantCulture)).ToList() 
                :
                ResultTestSet.Select((t, i) =>
                t.Ideal[0].ToString(CultureInfo.InvariantCulture) + "," +
                IdealTestOutput[i][0].ToString(CultureInfo.InvariantCulture) + "," +
                IdealTestOutput[i][1].ToString(CultureInfo.InvariantCulture) + "," +
                IdealTestOutput[i][2].ToString(CultureInfo.InvariantCulture)).ToList();

            result.Insert(0,
                "Klasa 'Cena zmaleje',Klasa 'Cena bez zmian',Klasa 'Cena wzrośnie'," +
                "Znaleziona klasa," +
                "Klasa 0 - oczekiwane,Klasa 1 - oczekiwane, Klasa 2 - oczekiwane");

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

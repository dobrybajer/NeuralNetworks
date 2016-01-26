using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using HFT.FileProcessing;
using HFT.Logic;
using HFT.Model;

namespace HFT
{
    internal class Program
    {
        private static void Main()
        {
            var parameters = new Parameters
            {
                Layers = (ConfigurationManager.AppSettings["LayersCount"] ?? "6,6,6,6,6").Split(',').Select(Int32.Parse).ToList(),
                HasBias = bool.Parse(ConfigurationManager.AppSettings["HasBias"] ?? "true"),
                IterationsCount = int.Parse(ConfigurationManager.AppSettings["IterationsCount"] ?? "2500"),
                LearingCoefficient = double.Parse(ConfigurationManager.AppSettings["LearingCoefficient"] ?? "0.01", CultureInfo.InvariantCulture),
                InertiaCoefficient = double.Parse(ConfigurationManager.AppSettings["InertiaCoefficient"] ?? "0.01", CultureInfo.InvariantCulture),
                AcceptedError = double.Parse(ConfigurationManager.AppSettings["AcceptedError"] ?? "0.0000001", CultureInfo.InvariantCulture),
                GroupSize = int.Parse(ConfigurationManager.AppSettings["GroupSize"] ?? "10"),
                TimeWindow = int.Parse(ConfigurationManager.AppSettings["TimeWindow"] ?? "5"),
                SlideWindow = int.Parse(ConfigurationManager.AppSettings["SlideWindow"] ?? "1"),
                ValidationSetSize = double.Parse(ConfigurationManager.AppSettings["ValidationSetSize"] ?? "15", CultureInfo.InvariantCulture)
            };

            var trainingSetPath =
                Path.Combine(ConfigurationManager.AppSettings["PathToTestFiles"] +
                             ConfigurationManager.AppSettings["TrainingSet"]);
            var testSetPath =
                Path.Combine(ConfigurationManager.AppSettings["PathToTestFiles"] +
                             ConfigurationManager.AppSettings["TestSet"]);

            var parser = new Parser();
            var classes = new Classification();

            var trainingSetModel = parser.ReadFile(trainingSetPath);
            var testSetModel = parser.ReadFile(testSetPath);

            trainingSetModel = classes.AddClasses(trainingSetModel, parameters);
            testSetModel = classes.AddClasses(testSetModel, parameters);

            if ((ConfigurationManager.AppSettings["Type"] ?? "1") == "1")
            {
                var classification = new ProblemBase(parameters);
                classification.Execute(trainingSetModel, testSetModel);
            }
            else
            {
                var svm = new SVM(parameters);
                svm.Execute(trainingSetModel, testSetModel);
            }
        }
    }
}

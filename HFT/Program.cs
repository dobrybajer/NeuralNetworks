using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using HFT.FileProcessing;
using HFT.Logic;
using HFT.Model;

namespace HFT
{
    class Program
    {
        static void Main()
        {
            var parameters = new Parameters
            {
                Layers = new List<int> { int.Parse(ConfigurationManager.AppSettings["LayersCount"] ?? "6") },
                HasBias = bool.Parse(ConfigurationManager.AppSettings["HasBias"] ?? "true"),
                IterationsCount = int.Parse(ConfigurationManager.AppSettings["IterationsCount"] ?? "2500"),
                LearingCoefficient = double.Parse(ConfigurationManager.AppSettings["LearingCoefficient"] ?? "0.01", CultureInfo.InvariantCulture),
                InertiaCoefficient = double.Parse(ConfigurationManager.AppSettings["InertiaCoefficient"] ?? "0.01", CultureInfo.InvariantCulture),
                AcceptedError = double.Parse(ConfigurationManager.AppSettings["AcceptedError"] ?? "0.0000001", CultureInfo.InvariantCulture)
            };

            var trainingSetPath = Path.Combine(ConfigurationManager.AppSettings["PathToTestFiles"] + ConfigurationManager.AppSettings["TrainingSet"]);
            var testSetPath = Path.Combine(ConfigurationManager.AppSettings["PathToTestFiles"] + ConfigurationManager.AppSettings["TestSet"]);

            var parser = new Parser();
            var classes= new Classification();
            var trainingSetModel = parser.ReadFile(testSetPath);
            var testSetModel = parser.ReadFile(trainingSetPath);
            trainingSetModel = classes.AddClasses(trainingSetModel);




            var classification = new ProblemBase(parameters);
            var svm = new SVM(parameters);
            svm.Execute(trainingSetModel, testSetModel);
            classification.Execute(trainingSetModel, testSetModel);
        }
    }
}

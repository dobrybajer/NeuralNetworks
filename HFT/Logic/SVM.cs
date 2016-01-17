using System;
using System.Collections.Generic;
using Encog.ML.Data;
using Encog.ML.SVM;
using Encog.ML.SVM.Training;
using Encog.ML.Train;
using HFT.Model;

namespace HFT.Logic
{
    // ReSharper disable once InconsistentNaming
    class SVM : ProblemBase
    {
        private SupportVectorMachine Svm { get; set; }

        public double[][] ClassificationInput; // TODO change to real one

        public double[][] ClassificationIdeal; // TODO change to real one

        private IMLDataSet trainingSet; // TODO change to real one

        public SVM(Parameters parameters) : base(parameters)
        {
  
        }

        public override void CreateNetwork()
        {
            Svm = new SupportVectorMachine(2, false); // TODO add proper input size
        }

        public override void TrainNetwork()
        {
            IMLTrain train = new SVMSearchTrain(Svm, trainingSet);

            var iteration = 1;

            var errors = new List<double[]>();

            do
            {
                train.Iteration();

                errors.Add(new[] { iteration, train.Error });

                Console.WriteLine(@"Iteration #" + iteration++ + @" Training error:" + train.Error);

            } while ((iteration < Parameters.IterationsCount) && (train.Error > Parameters.AcceptedError));

            train.FinishTraining();

            ErrorSet = errors.ToArray();

            Console.WriteLine(@"SVM Results:");

            foreach (var pair in trainingSet)
            {
                var output = Svm.Compute(pair.Input);

                Console.WriteLine(pair.Input[0] + @", actual=" + output[0] + @",ideal=" + pair.Ideal[0]);
            }

            Console.WriteLine("Done");
        }
    }
}

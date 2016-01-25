using System;
using System.Collections.Generic;
using System.Linq;
using Encog.ML.Data;
using Encog.Neural.Data.Basic;
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

        public SVM(Parameters parameters) : base(parameters)
        {
  
        }

        public override void CreateNetwork(int size)
        {
            Network.UpdateSizeInfo(size, 3);

            Svm = new SupportVectorMachine(size, false);
        }

        public override void TrainNetwork()
        {
            IMLTrain train = new SVMSearchTrain(Svm, TrainingSet);

            var iteration = 1;

            var errors = new List<double[]>();

            do
            {
                train.Iteration();

                errors.Add(new[] { -1, iteration, train.Error });

                Console.WriteLine(@"Iteration #" + iteration++ + @" Training error:" + train.Error);

            } while ((iteration < Parameters.IterationsCount) && (train.Error > Parameters.AcceptedError));

            train.FinishTraining();

            ErrorSet = errors.ToArray();
        }

        public override void CalculateResult()
        {
            Console.WriteLine(@"SVM Results:");

            var testSet = new BasicNeuralDataSet(TestSet, IdealTestOutput);
            var values = new List<double[]>();
            var result = new List<double[]>();

            foreach (var input in testSet)
            {
                var output = Svm.Compute(input.Input);

                Console.WriteLine(input.Input[0] + @", actual=" + output[0] + @", ideal=" + input.Ideal[0]);

                var tmp = new double[input.Input.Count];
                input.Input.CopyTo(tmp,0,input.Input.Count);
                values.Add(tmp);

                var tmp2 = new double[output.Count];
                output.CopyTo(tmp2, 0, output.Count);
                result.Add(tmp2);
            }

            ResultTestSet = new BasicNeuralDataSet(values.ToArray(), result.ToArray());

            Console.WriteLine("Done");
        }
    }
}

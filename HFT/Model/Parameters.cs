using System.Collections.Generic;

namespace HFT.Model
{
    class Parameters
    {
        public List<int> Layers { get; set; }

        public bool HasBias { get; set; }

        public int IterationsCount { get; set; }

        public double LearingCoefficient { get; set; }

        public double InertiaCoefficient { get; set; }

        public double AcceptedError { get; set; }

        public void Construct(
            List<int> layers,
            bool bias,
            int iterations,
            double learning,
            double inertia,
            double error = 0.001)
        {
            Layers = layers;
            HasBias = bias;
            IterationsCount = iterations;
            LearingCoefficient = learning;
            InertiaCoefficient = inertia;
            AcceptedError = error;
        }

        public Parameters(
            List<int> layers = null,
            bool bias = true,
            int iterations = 5000,
            double learning = 0.5,
            double inertia = 0.5,
            double error = 0.01)
        {
            layers = layers ?? new List<int>();
            Construct(layers, bias, iterations, learning, inertia, error);
        }
    }
}

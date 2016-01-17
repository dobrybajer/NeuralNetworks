using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;

namespace HFT.Model
{
    class Network
    {
        public BasicNetwork Model;

        public int InputSize;

        public int OutputSize;

        public Network()
        {
            Model = new BasicNetwork();
        }

        public void UpdateSizeInfo(int? input, int? output)
        {
            InputSize = input ?? 2;
            OutputSize = output ?? 3;
        }

        public void AddHiddenLayer(BasicLayer layer)
        {
            Model.AddLayer(layer);
        }

        public void AddMainLayer(bool first)
        {
            Model.AddLayer(new BasicLayer(null, true, first ? InputSize : OutputSize));
        }
    }
}

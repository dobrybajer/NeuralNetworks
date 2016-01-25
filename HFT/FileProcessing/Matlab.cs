using System.Diagnostics;
using System.IO;

namespace HFT.FileProcessing
{
    static class Matlab
    {
        public static string ErrorPath { get; set; }

        public static string ResultPath { get; set; }

        public static void GenerateCharts()
        {
            Process.Start("MATLAB.exe", CreateErrorCommand());// + CreateDataCommand());
        }

        private static string CreateErrorCommand()
        {
            return "-nosplash -nodesktop -r \"" +
                   "Data = csvread('" + ErrorPath + "');" +
                   "plot(Data(:, 1), Data(:, 2), Data(:, 1), Data(:, 3));" +
                   "xlabel('Number of Weight Updates');" +
                   "ylabel('Error');" +
                   "legend('Training set error','Validation set error');" +
                   "print('" + Path.GetDirectoryName(ErrorPath) + "\\ErrorChart', '-dpng');";
        }

        private static string CreateDataCommand()
        {
            return "Data2 = csvread('" + ResultPath + "');" +
                   "scatter(Data2(:,1), Data2(:,2), 1, Data2(:,3));" +
                   "xlabel('X');" +
                   "ylabel('Y');" +
                   "print('" + Path.GetDirectoryName(ResultPath) + "\\ResultChart', '-dpng');" +
                   "exit";
        }
    }
}

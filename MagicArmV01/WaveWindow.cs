using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicArmV01
{
    class WaveWindow
    {
        public double[] window;
        protected int size;
        double Q;
        public WaveWindow()
        {
            this.size = 5;
            window = new double[size];
            for (int i = size - 1; i > 0; i--) {
                window[i] = 0;
            }
            Q = 0;
        }
        public double addData(double data) {
            Q = 0;
            for (int i = size-1; i > 0; i--)
            {
                window[i] = window[i - 1];
            }
            window[0] = data;
            for (int i = 0; i < size; i++)
            {
                Q += window[i] * window[i];
            }
            return Q;
        }
    }
}

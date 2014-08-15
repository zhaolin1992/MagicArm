using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicArmV01
{
    class VaildWave
    {
         public int size = 5, cross = 0;
        //public List<double> wave; 
        public double[] wave = new double[256];
        public double max, min;
        public double margin;
        
        public VaildWave(WaveWindow dataWindow) 
        {
            size = 5;
            cross = 0;
            //wave = new List<double>(0);
            for (int i = 0; i < size; i++)
            {
                //wave.Add(dataWindow.window[i]);
                wave[i] = dataWindow.window[i];
                //if (dataWindow.window[i] == 0)
                    //cross++;
            }
            max = -256;
            min = 256;
            margin = 0;
        }
        public int Add(double data){
            if (data > max)
                max = data;
            if (data < min)
                min = data;
            margin = max - min;
            //if (data == 0)
                //cross++;
            if (size < 256)
                wave[size++] = data;
            //wave.Add(data);
            else 
            {
                if (wave[0] == 0)
                    cross--;
                for (int i = 0; i < size - 1; i++)
                {
                    wave[i] = wave[i + 1];
                }
                wave[255] = data;
            }
            cross = 0;
            for (int i = 0; i < size - 1; i++) 
                if (wave[i] * wave[i + 1] < 0) 
                    cross++;
            if (size >= 128)
            {
                //Console.WriteLine("(" + size + ")");
                return size;
            }else
                return 0;
        }
        public double[] GetData(int window_size) {
            double[] data = new double[window_size];
            for (int i = 0; i < window_size; i++)
                data[i] = wave[i];
            return data;
        }
    }
}

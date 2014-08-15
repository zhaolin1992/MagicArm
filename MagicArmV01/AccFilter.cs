using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicArmV01
{
    class AccFilter
    {
        double[] value = new double[5];
        public AccFilter(){
            for (int i = 0;i<5;i++){
                value[i] = 0;
            }
        }
        public double filter(double temp) {
            double sum = 0;
            for (int i = 1;i<5;i++){
                sum += value[i];
                value[i] = value[i - 1];
            }
            value[0] = temp;
            sum += temp;
            return sum / 5;
        }
    }
}

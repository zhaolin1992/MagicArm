using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicArmV01
{
    class KalmanFilter
    {
        double A = 1.0;
        double H = 1.0;
        double Q = 0.003;
        double R = 0.01;
        double P00 = 5.0;
        double P10 = 0.0;
        double X00 = 0.0;
        //double Kp = 10.0;
        //double Kd = 2.0;
        //double Ki = 0.0;
        double AngleAcc, AngleRotation, AngleMerge, B, U, Z, X10, Kg;
        public double Filter(double AccX, double AccZ, double AngY)
        {
            double ax = AccX / 128;
            double az = AccZ / 128;
            AngleAcc = (-1.0) * Math.Atan2(ax, az) * 180 / Math.PI;
            double gy = AngY / 131.00;
            AngleRotation = AngleRotation + gy * 0.25 / 500.00;
            B = 0.25 / 1000.0;
            U = gy;
            Z = AngleAcc;
            X10 = A * X00 + B * U; //=======formula 1
            P10 = A * P00 * (A) + Q;//=============formula 2
            Kg = P10 * (H) / (H * P10 * (H) + R);//=======formula 3

            X00 = X10 + Kg * (Z - H * X10);//========formula 4

            //P00 = (1 - Kg * H) * P10;//=============formula 5
            P00 = (1 - Kg * H) * P10 * (1 - Kg * H) + Kg * R * Kg;//======根据维基百科的说法，当Kg不是最优时采用这个公式
            AngleMerge = X00;
            return AngleMerge;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicArmV01
{
    class DFT
    {
                public double[] data_r;
        double[] data_i;
        double[] fft_r;
        double[] fft_i;

        public double[] mod;
        public double[] freq;

        public DFT()
        {
            fft_r = new double[128];
            fft_i = new double[128];
            data_i = new double[128];
            data_r = new double[128];
            mod = new double[128];
            freq = new double[16];
        }
        //旋转因子法求FFT
        //对原数据组进行重排
        private void DataSort(ref double[] data_r, ref double[] data_i)
        {
            if (data_r.Length == 0 || data_i.Length == 0 || data_r.Length != data_i.Length)
                return;
            int len = data_r.Length;
            int[] count = new int[len];
            int M = (int)(Math.Log(len) / Math.Log(2));
            double[] temp_r = new double[len];
            double[] temp_i = new double[len];

            for (int i = 0; i < len; i++)
            {
                temp_r[i] = data_r[i];
                temp_i[i] = data_i[i];
            }
            for (int l = 0; l < M; l++)
            {
                int space = (int)Math.Pow(2, l);
                int add = (int)Math.Pow(2, M - l - 1);
                for (int i = 0; i < len; i++)
                {
                    if ((i / space) % 2 != 0)
                        count[i] += add;
                }
            }
            for (int i = 0; i < len; i++)
            {
                data_r[i] = temp_r[count[i]];
                data_i[i] = temp_i[count[i]];
            }
        }
        void FFT(ref double[] data_r, ref double[] data_i, ref double[] result_r, ref double[] result_i)
        {
            if (data_r.Length == 0 || data_i.Length == 0 || data_r.Length != data_i.Length)
                return;
            int len = data_r.Length;
            double[] X_r = new double[len];
            double[] X_i = new double[len];
            for (int i = 0; i < len; i++)//将源数据复制副本，避免影响源数据的安全性
            {
                X_r[i] = data_r[i];
                X_i[i] = data_i[i];
            }
            DataSort(ref X_r, ref X_i);//位置重排
            double WN_r, WN_i;//旋转因子
            int M = (int)(Math.Log(len) / Math.Log(2));//蝶形图级数
            for (int l = 0; l < M; l++)
            {
                int space = (int)Math.Pow(2, l);
                int num = space;//旋转因子个数
                double temp1_r, temp1_i, temp2_r, temp2_i;
                for (int i = 0; i < num; i++)
                {
                    int p = (int)Math.Pow(2, M - 1 - l);//同一旋转因子有p个蝶
                    WN_r = Math.Cos(2 * Math.PI / len * p * i);
                    WN_i = -Math.Sin(2 * Math.PI / len * p * i);
                    for (int j = 0, n = i; j < p; j++, n += (int)Math.Pow(2, l + 1))
                    {
                        temp1_r = X_r[n];
                        temp1_i = X_i[n];
                        temp2_r = X_r[n + space];
                        temp2_i = X_i[n + space];//为蝶形的两个输入数据作副本，对副本进行计算，避免数据被修改后参加下一次计算
                        X_r[n] = temp1_r + temp2_r * WN_r - temp2_i * WN_i;
                        X_i[n] = temp1_i + temp2_i * WN_r + temp2_r * WN_i;
                        X_r[n + space] = temp1_r - temp2_r * WN_r + temp2_i * WN_i;
                        X_i[n + space] = temp1_i - temp2_i * WN_r - temp2_r * WN_i;
                    }
                }
            }
            //for (int i = 0; i < len; i++)//将源数据复制副本，避免影响源数据的安全性
            //{
            //    result_r[i] = X_r[i];
            //    result_i[i] = X_i[i];
            //}
            result_r = X_r;
            result_i = X_i;
        }
        private void GetMod(ref double[] complex_r, ref double[] complex_i, ref double[] mod)
        {
            if (complex_r.Length == 0 || complex_i.Length == 0 || complex_r.Length != complex_i.Length)
                return;
            for (int i = 0; i < complex_r.Length; i++)
                mod[i] = Math.Sqrt(Math.Sqrt(complex_r[i] * complex_r[i] + complex_i[i] * complex_i[i])) * 5;
            mod[0] = 0; 
            for (int i = 0; i < 48; i++)
                freq[i / 6 + 8 ] += mod[i] * mod[i] / 100;
            for (int i = 8; i < 16; i++)
                freq[i] = Math.Round(freq[i] / 100, 1);
            for (int i = 7; i >= 0; i--)
                freq[i] = freq[i * 2] * freq[i * 2] + freq[2 * i + 1]*freq[2 * i + 1];
        }
        public void FFT() 
        {
            if (data_r.Length == 0 || data_i.Length == 0 || data_r.Length != data_i.Length)
                return;
            for (int i = 0; i < 8; i++)
                freq[i] = 0;
            FFT(ref data_r, ref data_i, ref fft_r, ref fft_i);
            GetMod(ref fft_r, ref fft_i, ref mod);        
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MagicArmV01
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 收发数据串口
        /// </summary>
        SerialPort serialPort = new SerialPort();
        /// <summary>
        /// 串口读取数据队列
        /// </summary>
        Queue<byte> serialDataQueue = new Queue<byte>();
        /// <summary>
        /// 肌电信号各通道数值
        /// </summary>
        int[] signalChannel = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 肌电信号各通道参考数值(便于识别波形)
        /// </summary>
        double[] signalReference = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 数据放大倍率
        /// </summary>
        double dataValueGain = 1.0;
        /// <summary>
        /// 波形窗口
        /// </summary>
        WaveWindow[] waveWindow = new WaveWindow[8];
        /// <summary>
        /// 波形窗口内能量值
        /// </summary>
        public double[] WindowPower = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 波形状态
        /// </summary>
        int[] WaveState = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 无效波变有效记录
        /// </summary>
        int[] vaildCount = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 有效波变无效记录
        /// </summary>
        int[] invaildCount = { 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// 当前波记录
        /// </summary>
        VaildWave[] currentWave = new VaildWave[8];
        /// <summary>
        /// 肌电数据队列
        /// </summary>
        Queue<double>[] emgQueue = new Queue<double>[8];
        /// <summary>
        /// 加速度传感器数据队列
        /// </summary>
        Queue<double>[] mpuQueue = new Queue<double>[6];
        /// <summary>
        /// 卡尔曼滤波器
        /// </summary>
        KalmanFilter accXFilter = new KalmanFilter(), accYFilter = new KalmanFilter(), accZFilter = new KalmanFilter();
        /// <summary>
        /// 记录肌电信号的傅里叶变换结果
        /// </summary>
        DFT[] DataFFT = new DFT[8];
        /// <summary>
        /// 画线画布
        /// </summary>
        Canvas lineCanvas = new Canvas();
        /// <summary>
        /// 傅里叶变换画布
        /// </summary>
        Canvas DFTCanvas = new Canvas();
        /// <summary>
        /// 肌电信号,传感器信号画图横坐标
        /// </summary>
        int PointX = 0;
        /// <summary>
        /// 画线的起始点和终点
        /// </summary>
        System.Windows.Point PointEnd = new System.Windows.Point(), PointStart = new System.Windows.Point();

        /// <summary>
        /// 画线定时触发器
        /// </summary>
        DispatcherTimer drawLineTimer = new DispatcherTimer();
        /// <summary>
        /// 串口是否工作队列
        /// </summary>
        Queue<bool> isSerialWork = new Queue<bool>();

        /// <summary>
        /// 临时测试用变量
        /// </summary>
        double temp_angX, temp_angY, temp_angZ, temp_accZ;

        /// <summary>
        /// 
        /// </summary>
        AccFilter AccZFilter = new AccFilter();

        public MainWindow()
        {
            InitializeComponent();

            IntializeSerialPort();//串口初始化

            for (int i = 0; i < 8; i++)
            {
                emgQueue[i] = new Queue<double>();
                waveWindow[i] = new WaveWindow();
                waveWindow[i] = new WaveWindow();
                currentWave[i] = null;
                DataFFT[i] = new DFT();
            }
            for (int i = 0; i < 6; i++)
                mpuQueue[i] = new Queue<double>();
            grid.Children.Add(lineCanvas);
            drawLineTimer.Interval = TimeSpan.FromMilliseconds(1);
            drawLineTimer.Tick += new EventHandler(DrawLineTimerTick);
            drawLineTimer.Start();//画线时钟设置
        }
        /// <summary>
        /// 初始化串口设置
        /// </summary>
        void IntializeSerialPort()
        {
            SerialPort testSerial = new SerialPort();
            //检查是否含有串口
            string[] portName = SerialPort.GetPortNames();
            //MessageBox.Show(SerialPort.GetPortNames()[0]);
            if (portName == null)
            {
                MessageBox.Show("未发现串口！", "Error");
                return;
            }
            //添加串口项目
            foreach (string serialString in System.IO.Ports.SerialPort.GetPortNames())
            {//获取有多少个COM口
                combo.Items.Add(serialString);
            }
            //串口设置默认选择项
            combo.SelectedIndex = 0;
        }

        /// <summary>
        /// 串口接收程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SerialReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            int read_temp;//临时读取数据
            if (serialPort.IsOpen)
            {
                try
                {
                    serialPort.DataReceived -= new SerialDataReceivedEventHandler(SerialReceiveData);
                    int byteNum = serialPort.BytesToRead;//缓冲区字节数
                    while (serialPort.BytesToRead > 0)
                    {
                        read_temp = serialPort.ReadByte();
                        if (read_temp != -1)
                        {
                            serialDataQueue.Enqueue((byte)read_temp);

                        }
                        else
                        {
                            //MessageBox.Show("0");
                        }
                        byteNum--;
                    }
                    serialPort.DiscardInBuffer();
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialReceiveData);
                }
                catch
                {
                    //serialDataQueue.Clear();
                    //MessageBox.Show("0");
                }
            }
        }

        /// <summary>
        /// btnClear点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
        /// <summary>
        /// 画图时间函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineTimerTick(object sender, EventArgs e)
        {
            double temp_data = 0;
            bool start_data = true;
            while (isSerialWork.Count > 0)
            {
                start_data = isSerialWork.Dequeue();
            }
            if (serialPort.IsOpen)
            {
                if (start_data)
                {
                    //for (int j = 4; j < 5; j++)
                    //{
                    //    if (point_pos[j] > 1000)
                    //    {
                    //        point_pos[j] = 5;
                    //        line_canvas[j].Children.Clear();

                    //        start[j] = new System.Windows.Point(5, j * 100+50);
                    //     }
                    //}
                    int j;
                    j = 0;
                    j = (int)channel.SelectedItem;
                    if (lineTypeSemg.IsChecked == true)//绘制的为肌电信号的图像
                    {
                        if (PointX > 1200)
                        {
                            PointX = 5;
                            lineCanvas.Children.Clear();
                            PointStart = new System.Windows.Point(5, j * 100 + 50);
                        }
                        while (emgQueue[j].Count > 200)
                        {
                            temp_data = emgQueue[j].Dequeue();
                            PointEnd = new Point(PointX, temp_data + 200);
                            DrawingLine(PointStart, PointEnd, 1, lineCanvas);
                            PointStart = PointEnd;
                            PointX++;
                        }
                        if (WindowPower[3] > -1)
                        {
                            //Console.WriteLine(Math.Round(WindowPower[3]));
                        }

                        if (currentWave[j] != null)
                        {
                            //Console.WriteLine(currentWave[3].size);
                            if (currentWave[j].size > 40)
                            {
                                DFTCanvas.Children.Clear();


                                if (currentWave[j] != null)
                                {
                                    for (int i = 0; i < currentWave[j].size - 1; i++)
                                    {
                                        Point startPoint = new Point(i + 100, currentWave[j].wave[i] + 300);
                                        Point endPoint = new Point(i + 100, currentWave[j].wave[i + 1] + 300);
                                        DrawingLine(startPoint, endPoint, 2, DFTCanvas);
                                        //startPoint = new Point(i + 300, DataFFT.mod[i] + 300);
                                        //endPoint = new Point(i + 300, DataFFT.mod[i + 1] + 300);
                                        //DrawingLine(startPoint, endPoint, 2, line_canvas[2]);
                                    }
                                    for (int i = 0; i < 128; i++)
                                    {
                                        if (i < 8)
                                            DataFFT[j].data_r[i] = 0;
                                        else
                                            DataFFT[j].data_r[i] = 50;
                                        DataFFT[j].data_r[i] = currentWave[j].wave[i];
                                    }
                                    DataFFT[j].FFT();
                                    for (int i = 0; i < 63; i++)
                                    {
                                        Point startPoint = new Point(i + 500, -DataFFT[j].mod[i] + 400);
                                        Point endPoint = new Point(i + 500, -DataFFT[j].mod[i + 1] + 400);
                                        //Point startPoint = new Point(i + 300, -DataFFT[3].freq[i/4] + 300);
                                        //Point endPoint = new Point(i + 300, -DataFFT[3].freq[(i + 1)/4] + 300);
                                        DrawingLine(startPoint, endPoint, 2, DFTCanvas);
                                        //Console.Write(DataFFT.freq[i]+" ");
                                    }
                                    if (j == 8)
                                    //if (j <= 7)
                                    {
                                        for (int i = 0; i < 16; i++)
                                        {
                                            Console.Write(DataFFT[j].freq[i] + " ");
                                        }
                                        Console.WriteLine();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (PointX > 1200)
                        {
                            PointX = 5;
                            lineCanvas.Children.Clear();
                            PointStart = new System.Windows.Point(5, j * 100 + 50);
                        }
                        if (j < 6)
                        {
                            while (mpuQueue[j].Count > 200)
                            {
                                temp_data = mpuQueue[j].Dequeue();
                                PointEnd = new Point(PointX, temp_data * 1.5 + 300);
                                DrawingLine(PointStart, PointEnd, 1, lineCanvas);
                                PointStart = PointEnd;
                                PointX++;
                            }
                        }
                    }
                    updateSenserData();
                    //showAction(currentAction);
                }
            }
        }
        /// <summary>
        /// 数据解包线程方法
        /// </summary>
        private void dataUnpackThreadMethod()
        {
            byte[] data = new byte[2];

            data[1] = 0;
            int status = 1;//1读入##或者**，2#数据读取，3*号数据读取
            int number = 0, data_int;
            double data_queue;
            int[] tempData = new int[16];
            int[] mpuTempData = new int[6];
            double angleXY, angleYZ, tanX, tanY, tanZ;
            while (true)                            //循环检测队列 
            {
                if (serialDataQueue.Count >= 100)               //队列中有数据 
                {
                    data[0] = serialDataQueue.Dequeue();  //将数据出队
                    switch (status)
                    {
                        case 1:
                            if (data[0] == '#' && data[1] == '#')
                            {
                                status = 2;
                            }
                            else
                            {
                                data[1] = data[0];
                            }
                            break;
                        case 2:
                            if (int.TryParse(data[0].ToString(), out data_int))
                            {
                                number++;
                                switch (number)
                                {

                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                        mpuTempData[number - 1] = HexToInt(data_int);
                                        //mpuQueue[number-1].Enqueue(HexToInt(data_int));
                                        if (number == 6)
                                        {
                                            double AngleY = accYFilter.Filter(mpuTempData[3], mpuTempData[5], mpuTempData[1]);
                                            double AngleZ = accZFilter.Filter(mpuTempData[4], mpuTempData[3], mpuTempData[2]);
                                            double AngleX = accXFilter.Filter(mpuTempData[5], mpuTempData[4], mpuTempData[0]);
                                            AngleX = (AngleX - 90) % 180;
                                            mpuQueue[0].Enqueue(AngleX);
                                            mpuQueue[1].Enqueue(AngleY);
                                            mpuQueue[2].Enqueue(AngleZ);
                                            tanX = Math.Tan(AngleX * Math.PI / 180);
                                            tanY = Math.Tan(AngleY * Math.PI / 180);
                                            tanZ = Math.Tan(AngleZ * Math.PI / 180);
                                            angleXY = Math.Atan(Math.Sqrt(tanY * tanY + tanX * tanX));
                                            angleYZ = Math.Atan(Math.Sqrt(tanZ * tanZ + tanY * tanY));
                                            //mpuQueue[3].Enqueue(mpuTempData[5] + 64 * Math.Cos(angleXY) * Math.Sign(AngleX));
                                            //mpuQueue[4].Enqueue();

                                            //mpuQueue[3].Enqueue(Math.Tan(AngleX * Math.PI / 180));
                                            //mpuQueue[4].Enqueue(Math.Tan(AngleY * Math.PI / 180));
                                            //mpuQueue[5].Enqueue(Math.Tan(AngleZ * Math.PI / 180));
                                            temp_angX = AngleX;
                                            temp_angY = AngleY;
                                            temp_angZ = mpuTempData[5];
                                            //temp_accZ = angleXY * 180 / Math.PI;
                                            temp_accZ = AccZFilter.filter(mpuTempData[5] + 64 * Math.Cos(angleXY) * Math.Sign(AngleX));
                                            mpuQueue[3].Enqueue(temp_accZ);

                                        }
                                        break;
                                    case 7:
                                    case 8:
                                    case 9:
                                    case 10:
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:

                                        #region 数据预处理

                                        data_queue = data_int;

                                        //data_queue = emg_filter[number-7].filter(data_int);
                                        data_int = (int)data_queue;

                                        if (signalChannel[number - 7] < 1000)
                                        {
                                            signalReference[number - 7] += data_int;
                                            signalChannel[number - 7]++;
                                            data_queue = data_int - signalReference[number - 7] / signalChannel[number - 7];
                                        }
                                        else if (signalChannel[number - 7] == 1000)
                                        {
                                            signalReference[number - 7] = Math.Round(signalReference[number - 7] / 1000.0, 2);
                                            signalChannel[number - 7]++;
                                        }
                                        else
                                        {
                                            data_queue = data_int - signalReference[number - 7];
                                        }

                                        data_queue = data_queue * dataValueGain;

                                        #endregion

                                        #region 有效波识别
                                        WindowPower[number - 7] = waveWindow[number - 7].addData(data_queue);
                                        if (WindowPower[number - 7] > 300)
                                        {
                                            if (WaveState[number - 7] == 0)
                                            {
                                                vaildCount[number - 7]++;
                                                if (vaildCount[number - 7] > 5)
                                                {
                                                    currentWave[number - 7] = new VaildWave(waveWindow[number - 7]);
                                                    WaveState[number - 7] = 1;
                                                }
                                            }
                                            if (WaveState[number - 7] == 1)
                                            {
                                                int flag = currentWave[number - 7].Add(data_queue);
                                                //dataWin = currentWave[3].GetData(64);
                                                if (flag > 0)
                                                {
                                                    if (flag < 200)
                                                    {
                                                        //for (int i = 0; i < 128; i++)
                                                        //{
                                                        //    DataFFT[number-7].data_r[i] = currentWave[3].wave[i];
                                                        //}
                                                        //DataFFT[number-7].FFT();
                                                    }
                                                    else
                                                    {
                                                        currentWave[number - 7].size = 0;
                                                        WaveState[number - 7] = 0;
                                                    }
                                                }
                                                invaildCount[number - 7] = 0;
                                            }
                                        }
                                        else
                                        {
                                            if (WindowPower[number - 7] < 600)
                                            {
                                                invaildCount[number - 7]++;
                                                if (invaildCount[number - 7] > 100)
                                                {
                                                    WaveState[number - 7] = 0;
                                                    if (currentWave[number - 7] != null)
                                                        currentWave[number - 7].size = 0;
                                                }
                                            }
                                        }
                                        #endregion

                                        try
                                        {
                                            //queue.Enqueue(data_queue);
                                            emgQueue[number - 7].Enqueue(data_queue);
                                        }
                                        catch
                                        {

                                        }
                                        if (number > 13)
                                        {
                                            data[1] = 0;
                                            number = 0;
                                            status = 1;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                status = 1; number = 0;
                            }
                            break;
                        default:
                            break;
                    }
                    if (serialDataQueue.Count > 500)
                    {
                        serialDataQueue.Clear();
                    }
                }
                else
                {
                    if ((serialDataQueue.Count + "").Length > 10)
                    {
                        serialDataQueue.Clear();
                    }
                    //
                }
            }
        }

        private void btnStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            isSerialWork.Enqueue(true);
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.PortName = combo.SelectedItem.ToString();
                    serialPort.BaudRate = 115200;
                    serialPort.Handshake = System.IO.Ports.Handshake.None;
                    serialPort.Parity = Parity.None;

                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;
                    serialPort.ReceivedBytesThreshold = 1;
                    serialPort.ReadBufferSize = 38400;//串口参数设置

                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        Thread dataUnpackThread = new Thread(new ThreadStart(dataUnpackThreadMethod));
                        dataUnpackThread.Start();

                        channel.Items.Clear();
                        for (int i = 0; i < 8; i++)
                            channel.Items.Add(i);
                        channel.SelectedIndex = 0;

                        btnStart.IsEnabled = false;
                        serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialReceiveData);
                    }
                }
                else
                {
                }
            }
            catch
            {

            }
        }
        /// <summary>
        /// 画线函数
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        /// <param name="color">颜色编号</param>
        /// <param name="canvas">画布</param>
        protected void DrawingLine(System.Windows.Point startPoint, System.Windows.Point endPoint, int color, Canvas canvas)
        {
            LineGeometry lineGeometry = new LineGeometry();
            lineGeometry.StartPoint = startPoint;
            lineGeometry.EndPoint = endPoint;

            Path myPath = new Path();
            if (color == 1)
            {
                myPath.Stroke = Brushes.Black;
            }
            else
            {
                myPath.Stroke = Brushes.Red;
            }
            myPath.StrokeThickness = 1;
            myPath.Data = lineGeometry;
            canvas.Children.Add(myPath);
        }
        /// <summary>
        /// 数据增益倍数调节
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void valueGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dataValueGain = ((double)valueGain.Value) / 5;
        }
        /// <summary>
        /// 数据转化16进制转为整型
        /// </summary>
        /// <param name="x">十六进制数据</param>
        /// <returns></returns>
        private int HexToInt(int x)
        {
            if (x >= 128)
            {
                if (x == 255)
                    return 0;
                else
                    return ((~(x - 1)) & 0x00FF) * -1;
            }
            else
            {
                return x;
            }
        }
        private void updateSenserData()
        {
            this.lblAngleX.Content = temp_angX;
            this.lblAngleY.Content = temp_angY;
            this.lblAngleZ.Content = temp_angZ;
            this.lblAccZ.Content = temp_accZ;
        }
    }
}

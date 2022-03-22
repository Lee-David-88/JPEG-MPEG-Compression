using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace COMP4932_A2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{

        BitmapImage leftBitmapImage;
        //BitmapImage rightBitmapImage;

        public static readonly int[,] Q_LUMINOSITY =
        {
            {16, 11, 10, 16, 24, 40, 51, 61},
            {12, 12, 14, 19, 26, 58, 60, 55},
            {14, 13, 16, 24, 40, 57, 69, 56},
            {14, 17, 22, 29, 51, 87, 80, 62},
            {18, 22, 37, 56, 68, 109, 103, 77},
            {24, 35, 55, 64, 81, 104, 113, 92},
            {49, 64, 78, 87, 103, 121, 120, 101},
            {72, 92, 95, 98, 112, 100, 103, 99}
        };
        public static readonly int[,] Q_CHROMINANCE =
        {
            {17, 18, 24, 47, 99, 99, 99, 99},
            {18, 21, 26, 66, 99, 99, 99, 99},
            {24, 26, 56, 99, 99, 99, 99, 99},
            {47, 66, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99}
        };
        public static readonly int[] MAP =
        {
            0, 8, 1, 2, 9, 16, 24, 17,
            10, 3, 4, 11, 18, 25, 32, 40,
            33, 26, 19, 12, 5, 6, 13, 20,
            27, 34, 41, 48, 56, 49, 42, 35,
            28, 21, 14, 7, 15, 22, 29, 36,
            43, 50, 57, 58, 51, 44, 37, 30,
            23, 31, 38, 45, 52, 59, 60, 53,
            46, 39, 47, 54, 61, 62, 55, 63
        };

        public MainWindow()
		{
			InitializeComponent();
		}

        private void AddLeftImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(openFileDialog.FileName);
                bitmapImage.EndInit();

                leftImage.Width = bitmapImage.Width;
                leftImage.Height = bitmapImage.Height;
                leftImage.Source =  new WriteableBitmap(bitmapImage);
                leftBitmapImage = bitmapImage;
            }
            double testInt = 300.2;
            Trace.WriteLine((int)testInt + " " + (byte)testInt);

        }

        private void AddRightImage_Click(object sender, RoutedEventArgs e)
        {
            String filePath = @"C:\Users\David\Desktop\test.bmp";
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)rightImage.Source));
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                encoder.Save(stream);
        }

        // For Testing
        private void Y_Click(object sender, RoutedEventArgs e)
        {
            byte[] RGB = BitmapSourceToArray(leftBitmapImage);

            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            double[] YCbCr = RGB2YCbCr(RGB, width, height);

            List<double> YChannel = new List<double>();

            for (int i = 0; i < YCbCr.Length; i += 4)
            {
                // YChannel
                YChannel.Add(YCbCr[i]);
            }

            double[] nYChannel = new double[YChannel.Count];

            for (int j = 0; j < YChannel.Count; j++)
            {
                nYChannel[j] = YChannel[j];
            }

            double[,] Y2D = OneDToTwoDDouble(nYChannel, width, height);
            double[][,] Y8x8 = To8x8Matrices(Y2D);
            double[][,] YDCTQ = DCTNQuantization(Y8x8, Q_LUMINOSITY);
            byte[] YEncode = Encode(YDCTQ);
            double[][,] YDecode = Decode(YEncode, width, height);

            /*            for (int i = 0; i < YDCTQ.Length; i++)
                        {
                            for (int j = 0; j < YDCTQ[i].GetLength(0); j++)
                            {
                                for (int k = 0; k < YDCTQ[i].GetLength(1); k++)
                                {
                                    Trace.WriteLine(Math.Round(YDCTQ[i][j, k]) + " " + YDecode[i][j, k]);
                                    Trace.WriteLineIf(Math.Round(YDCTQ[i][j, k]) != YDecode[i][j, k], "Issue");
                                }
                            }
                        }*/

            double[][,] YIDCTQ = IDCTNDequantization(YDecode, Q_LUMINOSITY);
            double[,] YUnpack = Un8x8MatricesDouble(YIDCTQ, width, height);
            double[] Y1D = TwoDToOneDDouble(YUnpack);

            List<byte> nnYChannel = new List<byte>();

            for (int k = 0; k < Y1D.Length; k++)
            {
                nnYChannel.Add((byte)Math.Round(Y1D[k]));
                nnYChannel.Add((byte)Math.Round(Y1D[k]));
                nnYChannel.Add((byte)Math.Round(Y1D[k]));
                nnYChannel.Add(255);
            }

            byte[] result = new byte[nnYChannel.Count];

            for (int j = 0; j < nnYChannel.Count; j++)
            {
                result[j] = nnYChannel[j];
            }

            rightImage.Width = Width;
            rightImage.Height = Height;

            rightImage.Source = BitmapSourceFromArray(result, width, height);
        }

        private void Cb_Click(object sender, RoutedEventArgs e)
        {
            /*byte[] RGB = BitmapSourceToArray(leftBitmapImage);
            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            byte[] YCbCr = RGB2YCbCr(RGB, width, height);

            List<byte> CbChannel = new List<byte>();

            for (int i = 0; i < YCbCr.Length; i += 4)
            {
                // CbChannel
                CbChannel.Add(YCbCr[i + 1]);
            }

            byte[] nCbChannel = new byte[CbChannel.Count];

            for (int k = 0; k < CbChannel.Count; k++)
            {
                nCbChannel[k] = CbChannel[k];
            }

            byte[] ssCb = Subsample(nCbChannel, width);

            byte[,] ssCb2D = OneDToTwoD(ssCb, width / 2, height / 2);
            byte[][,] ssCb8x8 = To8x8MatricesByte(ssCb2D);
            double[][,] ssCbDCTQ = DCTNQuantization(ssCb8x8, Q_CHROMINANCE);
            byte[] ssCbEncode = Encode(ssCbDCTQ);

            double[][,] ssCbDecode = Decode(ssCbEncode, width / 2, height / 2);
            byte[][,] ssCbIDCTQ = IDCTNDequantization(ssCbDecode, Q_CHROMINANCE);
            byte[,] ssCbUnpack = Un8x8Matrices(ssCbIDCTQ, width / 2, height / 2);
            byte[] ssCb1D = TwoDToOneD(ssCbUnpack);


            byte[] usCbChannel = UpSample(ssCb1D, width, height);

            List<byte> nnCbChannel = new List<byte>();

            for (int k = 0; k < usCbChannel.Length; k++)
            {
                nnCbChannel.Add(usCbChannel[k]);
                nnCbChannel.Add(usCbChannel[k]);
                nnCbChannel.Add(usCbChannel[k]);
                nnCbChannel.Add(255);
            }

            byte[] result = new byte[nnCbChannel.Count];

            for (int j = 0; j < nnCbChannel.Count; j++)
            {
                result[j] = nnCbChannel[j];
            }

            // Set Image Size
            rightImage.Width = Width;
            rightImage.Height = Height;

            rightImage.Source = BitmapSourceFromArray(result, width, height);*/
        }

        private void Cr_Click(object sender, RoutedEventArgs e)
        {
            /*byte[] RGB = BitmapSourceToArray(leftBitmapImage);
            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            byte[] YCbCr = RGB2YCbCr(RGB, width, height);

            List<byte> CrChannel = new List<byte>();

            for (int i = 0; i < YCbCr.Length; i += 4)
            {
                // CrChannel
                CrChannel.Add(YCbCr[i + 2]);
            }
            byte[] nCrChannel = new byte[CrChannel.Count];

            for (int k = 0; k < CrChannel.Count; k++)
            {
                nCrChannel[k] = CrChannel[k];
            }

            byte[] ssCr = Subsample(nCrChannel, width);

            List<byte> nnCrChannel = new List<byte>();

            for (int k = 0; k < ssCr.Length; k++)
            {
                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(255);
            }

            byte[] result = new byte[nnCrChannel.Count];

            for (int j = 0; j < nnCrChannel.Count; j++)
            {
                result[j] = nnCrChannel[j];
            }

            // Set Image Size
            rightImage.Width = Width / 2;
            rightImage.Height = Height / 2;

            rightImage.Source = BitmapSourceFromArray(result, width / 2, height / 2);*/
        }

        private void ToYCrCb_Click(object sender, RoutedEventArgs e)
        {
            /*byte[] RGB = BitmapSourceToArray(leftBitmapImage);

            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            byte[] YCbCr = RGB2YCbCr(RGB, width, height);

            // YCbCr??
            //byte[] resultCrCb = RGB2YCrCb(RGB, width, height);
            //byte[] resultCbCr = RGB2YCrCb(resultCrCb, width, height);

            // Check For Reverse
            //byte[] RGB2 = YCbCr2RGB(YCbCr, width, height);

            //byte[] subsample = Subsample(YCbCr, width, height);

            //byte[] reverseSubs = YCbCr2RGB(subsample, width, height);


            List<byte> YChannel = new List<byte>();
            List<byte> CbChannel = new List<byte>();
            List<byte> CrChannel = new List<byte>();
            List<byte> Alpha = new List<byte>();


            for (int i = 0; i < YCbCr.Length; i += 4)
            {
                // YChannel
                YChannel.Add(YCbCr[i]);
                YChannel.Add(YCbCr[i]);
                YChannel.Add(YCbCr[i]);
                YChannel.Add(YCbCr[i + 3]);

                // CbChannel
                CbChannel.Add(YCbCr[i + 1]);

                // CrChannel
                CrChannel.Add(YCbCr[i + 2]);

                // Alpha
                Alpha.Add(YCbCr[i + 3]);
                Alpha.Add(YCbCr[i + 3]);
                Alpha.Add(YCbCr[i + 3]);
                Alpha.Add(YCbCr[i + 3]);
            }

            byte[] nYChannel = new byte[YChannel.Count];
            byte[] nCbChannel = new byte[CbChannel.Count];
            byte[] nCrChannel = new byte[CrChannel.Count];
            byte[] nAlpha = new byte[Alpha.Count];


            for (int j = 0; j < YChannel.Count; j++)
            {
                nYChannel[j] = YChannel[j];
                nAlpha[j] = Alpha[j];
            }

            for (int k = 0; k < CbChannel.Count; k++)
            {
                nCbChannel[k] = CbChannel[k];
                nCrChannel[k] = CrChannel[k];
            }

            byte[] ssCb = Subsample(nCbChannel, width);
            byte[] ssCr = Subsample(nCrChannel, width);

            List<byte> nnCbChannel = new List<byte>();
            List<byte> nnCrChannel = new List<byte>();


            for (int k = 0; k < ssCb.Length; k++)
            {
                nnCbChannel.Add(ssCb[k]);
                nnCbChannel.Add(ssCb[k]);
                nnCbChannel.Add(ssCb[k]);
                nnCbChannel.Add(Alpha[k]);

                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(ssCr[k]);
                nnCrChannel.Add(Alpha[k]);
            }

            byte[] result = new byte[nnCbChannel.Count];
            byte[] result1 = new byte[nnCrChannel.Count];


            for (int j = 0; j < nnCbChannel.Count; j++)
            {
                result[j] = nnCbChannel[j];
                result1[j] = nnCrChannel[j];
            }

            // Set Image Size
            rightImage.Width = Width;
            rightImage.Height = Height;

            rightImage.Source = BitmapSourceFromArray(YCbCr, width, height);
            //rightImage.Source = BitmapSourceFromArray(nYChannel, width, height);
            //rightImage.Source = BitmapSourceFromArray(result, width/2, height/2);
            //rightImage.Source = BitmapSourceFromArray(result1, width/2, height/2);
            //String filePath = @"C:\Users\David\Desktop\test.txt";
            //File.WriteAllBytes(filePath, ssCb);*/
        }

        private void RGB_Click(object sender, RoutedEventArgs e)
        {
            /*byte[] RGB = BitmapSourceToArray(leftBitmapImage);

            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            double[] YCbCr = RGB2YCbCr(RGB, width, height);

            byte[] RGBA = YCbCr2RGB(YCbCr, width, height);

            rightImage.Width = Width;
            rightImage.Height = Height;

            rightImage.Source = BitmapSourceFromArray(RGBA, width, height);*/
        }

        // Compression and Decompression
        private void Compression_Click(object sender, RoutedEventArgs e)
        {
            String filePath = @"C:\Users\David\Desktop\Compressed.dat";            

            // Get Width and Height of the Image
            int width = leftBitmapImage.PixelWidth;
            int height = leftBitmapImage.PixelHeight;

            byte[] RGB = BitmapSourceToArray(leftBitmapImage);

            // Convert RGB to YCrCb
            double[] YCbCr = RGB2YCbCr(RGB, width, height);

            List<double> YChannel = new List<double>();
            List<double> CbChannel = new List<double>();
            List<double> CrChannel = new List<double>();
            List<double> Alpha = new List<double>();

            // Seperate the Channels
            for (int i = 0; i < YCbCr.Length; i += 4)
            {
                // YChannel
                YChannel.Add(YCbCr[i]);

                // CbChannel
                CbChannel.Add(YCbCr[i + 1]);

                // CrChannel
                CrChannel.Add(YCbCr[i + 2]);

            }

            double[] nYChannel = new double[YChannel.Count];
            double[] nCbChannel = new double[CbChannel.Count];
            double[] nCrChannel = new double[CrChannel.Count];


            for (int j = 0; j < YChannel.Count; j++)
            {
                nYChannel[j] = YChannel[j];
            }

            for (int k = 0; k < CbChannel.Count; k++)
            {
                nCbChannel[k] = CbChannel[k];
                nCrChannel[k] = CrChannel[k];
            }

            // Subsample Cr and Cb
            double[] ssCb = Subsample(nCbChannel, width);
            double[] ssCr = Subsample(nCrChannel, width);

            // Turn YCrCb into 2D array
            double[,] Y2D = OneDToTwoDDouble(nYChannel, width, height);
            double[,] Cb2D = OneDToTwoDDouble(ssCb, width / 2, height / 2);
            double[,] Cr2D = OneDToTwoDDouble(ssCr, width / 2, height / 2);

            // Convert To 8x8 Blocks
            double[][,] Y8x8 = To8x8Matrices(Y2D);
            double[][,] Cb8x8 = To8x8Matrices(Cb2D);
            double[][,] Cr8x8 = To8x8Matrices(Cr2D);

            Stopwatch stopwatch = Stopwatch.StartNew();
            // Apply DCT and Quantization
            double[][,] YDCTQ = DCTNQuantization(Y8x8, Q_LUMINOSITY);
            double[][,] CbDCTQ = DCTNQuantization(Cb8x8, Q_CHROMINANCE);
            double[][,] CrDCTQ = DCTNQuantization(Cr8x8, Q_CHROMINANCE);
            stopwatch.Stop();
            Trace.WriteLine(stopwatch.ElapsedMilliseconds);

            // Encoding
            byte[] YEncode = Encode(YDCTQ);
            byte[] CbEncode = Encode(CbDCTQ);
            byte[] CrEncode = Encode(CrDCTQ);

            // Header Info
            int YLength = YEncode.Length;
            int CbLength = CbEncode.Length;
            int CrLength = CrEncode.Length;
            int totalHeader = 5 * 4;

            // Always Length Of 4
            byte[] widthByteArray = BitConverter.GetBytes(width);
            byte[] heightByteArray = BitConverter.GetBytes(height);
            byte[] YLengthByteArray = BitConverter.GetBytes(YLength);
            byte[] CbLengthByteArray = BitConverter.GetBytes(CbLength);
            byte[] CrLengthByteArray = BitConverter.GetBytes(CrLength);

            // Final Byte Array For The Compressed File
            byte[] finalByteArray = new byte[totalHeader + YLength + CbLength + CrLength];

            // Copy Byte Arrays Into finalByteArray
            Array.Copy(widthByteArray, 0, finalByteArray, 0, widthByteArray.Length);
            Array.Copy(heightByteArray, 0, finalByteArray, 4, heightByteArray.Length);
            Array.Copy(YLengthByteArray, 0, finalByteArray, 8, YLengthByteArray.Length);
            Array.Copy(CbLengthByteArray, 0, finalByteArray, 12, CbLengthByteArray.Length);
            Array.Copy(CrLengthByteArray, 0, finalByteArray, 16, CrLengthByteArray.Length);
            Array.Copy(YEncode, 0, finalByteArray, 20, YEncode.Length);
            Array.Copy(CbEncode, 0, finalByteArray, 20 + YEncode.Length, CbEncode.Length);
            Array.Copy(CrEncode, 0, finalByteArray, 20 + YEncode.Length + CbEncode.Length, CrEncode.Length);

            // Store file
            File.WriteAllBytes(filePath, finalByteArray);
        }

        private void Decompression_Click(object sender, RoutedEventArgs e)
        {
            int headerInfoSize = 4;
            String filePath = @"C:\Users\David\Desktop\Decompressed.jpeg";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {   
                // Load File
                byte[] file = File.ReadAllBytes(openFileDialog.FileName);

                // Extract Header Information

                // Get Width
                byte[] widthByteArr = new byte[headerInfoSize];
                Array.Copy(file, 0, widthByteArr, 0, 4);
                int width = BitConverter.ToInt32(widthByteArr, 0);
                Trace.WriteLine(width);

                // Get Height
                byte[] heightByteArr = new byte[headerInfoSize];
                Array.Copy(file, 4, heightByteArr, 0, 4);
                int height = BitConverter.ToInt32(heightByteArr, 0);
                Trace.WriteLine(height);

                // Get Y Length
                byte[] YByteArr = new byte[headerInfoSize];
                Array.Copy(file, 8, YByteArr, 0, 4);
                int YLength = BitConverter.ToInt32(YByteArr, 0);
                Trace.WriteLine(YLength);

                // Get Cb Length
                byte[] CbByteArr = new byte[headerInfoSize];
                Array.Copy(file, 12, CbByteArr, 0, 4);
                int CbLength = BitConverter.ToInt32(CbByteArr, 0);
                Trace.WriteLine(CbLength);

                // Get Cb Length
                byte[] CrByteArr = new byte[headerInfoSize];
                Array.Copy(file, 16, CrByteArr, 0, 4);
                int CrLength = BitConverter.ToInt32(CrByteArr, 0);
                Trace.WriteLine(CrLength);

                // Get Y Channel
                byte[] YEncode = new byte[YLength];
                Array.Copy(file, 20, YEncode, 0, YLength);

                // Get Cb Channel
                byte[] CbEncode = new byte[CbLength];
                Array.Copy(file, 20 + YLength, CbEncode, 0, CbLength);

                // Get Cr Channel
                byte[] CrEncode = new byte[CrLength];
                Array.Copy(file, 20 + YLength + CbLength, CrEncode, 0, CrLength);

                // Decode MRLE
                double[][,] YDecode = Decode(YEncode, width, height);
                double[][,] CbDecode = Decode(CbEncode, width, height);
                double[][,] CrDecode = Decode(CrEncode, width, height);

                // Apply IDCT And Quantization
                double[][,] YIDCTQ = IDCTNDequantization(YDecode, Q_LUMINOSITY);
                double[][,] CbIDCTQ = IDCTNDequantization(CbDecode, Q_CHROMINANCE);
                double[][,] CrIDCTQ = IDCTNDequantization(CrDecode, Q_CHROMINANCE);

                // Unpack 8x8
                double[,] YUnpack = Un8x8MatricesDouble(YIDCTQ, width, height);
                double[,] CbUnpack = Un8x8MatricesDouble(CbIDCTQ, width / 2, height / 2);
                double[,] CrUnpack = Un8x8MatricesDouble(CrIDCTQ, width / 2, height / 2);

                // 2D to 1D
                double[] Y1D = TwoDToOneDDouble(YUnpack);
                double[] Cb1D = TwoDToOneDDouble(CbUnpack);
                double[] Cr1D = TwoDToOneDDouble(CrUnpack);

                // UpSample Cb and Cr
                double[] usCbChannel = UpSample(Cb1D, width, height);
                double[] usCrChannel = UpSample(Cr1D, width, height);

                // Reassemble Image Byte Array
                List<double> ImageByteList = new List<double>();

                for (int k = 0; k < Y1D.Length; k++)
                {
                    ImageByteList.Add(Y1D[k]);
                    ImageByteList.Add(usCbChannel[k]);
                    ImageByteList.Add(usCrChannel[k]);
                    ImageByteList.Add(255);
                }
                double[] resultYCrCb = listToArrayDouble(ImageByteList);

                // Convert From YCbCr to RGB
                byte[] resultRGB = YCbCr2RGB(resultYCrCb, width, height);

                rightImage.Width = Width;
                rightImage.Height = Height;

                rightImage.Source = BitmapSourceFromArray(resultRGB, width, height);
                File.WriteAllBytes(filePath, resultRGB);
            }
        }

        private double[][,] IDCTNDequantization(double[][,] input, int[,] Q)
        {
            double[][,] output = new double[input.Length][,];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = IDCT(Dequantization(input[i], Q));
            }

            return output;
        }

        private byte[,] Un8x8Matrices(byte[][,] input, int width, int height)
        {   
            byte[,] output = new byte[height, width];
            int p = 0;
            for (int j = 0; j < height; j += 8)
            {
                for (int i = 0; i < width; i += 8)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            if ((j + x) < height && (i + y) < width)
                            {
                                output[j + x, i + y] = input[p][x, y];
                            }
                        }
                    }
                    p++;
                }
            }
            return output;
        }

        private double[,] Un8x8MatricesDouble(double[][,] input, int width, int height)
        {
            double[,] output = new double[height, width];
            int p = 0;
            for (int j = 0; j < height; j += 8)
            {
                for (int i = 0; i < width; i += 8)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            if ((j + x) < height && (i + y) < width)
                            {
                                output[j + x, i + y] = input[p][x, y];
                            }
                        }
                    }
                    p++;
                }
            }
            return output;
        }

        private double[][,] DCTNQuantization(double[][,] input, int[,] Q)
        {   
            double[][,] output = new double[input.Length][,];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = Quantization(DCT(input[i]), Q);
            }
            return output;
        }

        private byte[] Encode(double[][,] input)
        {
            int[][] outputDouble = new int[input.Length][];

            for (int i = 0; i < input.Length; i++)
            {
                outputDouble[i] = EntropyEncoding(input[i]);
            }

            List<byte> result = new List<byte>();

            for (int i = 0; i < outputDouble.Length; i++)
            {
                for (int j = 0; j < outputDouble[i].Length; j++)
                {
                    result.Add((byte)Math.Round((double)outputDouble[i][j] + 128));
                }
            }

            return listToArrayByte(result);
        }

        private double[][,] Decode(byte[] input, int width, int height)
        {
            int[] decode1D = new int[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                decode1D[i] = input[i] - 128;
            }

            // Decoding MRLE
            List<double> outputList = new List<double>();
            int index = 0;

            while (index < decode1D.Length)
            {
                if (decode1D[index] != 0)
                {
                    outputList.Add(decode1D[index]);
                }
                else
                {
                    double key = decode1D[index + 2];
                    double length = decode1D[index + 1];
                    for (int j = 0; j < length; j++)
                    {
                        outputList.Add(key);
                    }
                    index += 2;
                }
                index++;
            }

            double[] output = listToArrayDouble(outputList);

            double[][] test = new double[output.Length/64][];

            for (int i = 0; i < test.Length; i++)
            {
                test[i] = new double[64];
            }

            for (int i = 0; i < test.Length; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    test[i][j] = output[i * 64 + j];
                }
            }

            double[][,] decode = new double[test.Length][,];

            for (int i = 0; i < test.Length; i++)
            {
                decode[i] = EntropyDecoding(test[i]);
            }

            return decode;
        }
        
        private byte[][,] To8x8MatricesByte(byte[,] input)
        {
            int width = input.GetLength(0) / 8;
            int height = input.GetLength(1) / 8;

            if (input.GetLength(0) % 8 != 0)
            {
                width = (input.GetLength(0) / 8) + 1;
            }

            if (input.GetLength(1) % 8 != 0)
            {
                height = (input.GetLength(1) / 8) + 1;
            }

            int numOfMatrices = width * height;

            byte[][,] output = new byte[numOfMatrices][,];

            for (int i = 0; i < numOfMatrices; i++)
            {
                output[i] = new byte[8, 8];
            }

            int p = 0;
            for (int j = 0; j < input.GetLength(0); j += 8)
            {
                for (int i = 0; i < input.GetLength(1); i += 8)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {   
                            if ((j + y) < input.GetLength(0) && (i + x) < input.GetLength(1))
                            {
                                output[p][y, x] = input[j + y, i + x];
                            } else
                            {
                                output[p][y, x] = 0;
                            }
                        }
                    }
                    p++;
                }
            }
            return output;
        }

        private double[][,] To8x8Matrices(double[,] input)
        {
            int width = input.GetLength(0) / 8;
            int height = input.GetLength(1) / 8;

            if (input.GetLength(0) % 8 != 0)
            {
                width = (input.GetLength(0) / 8) + 1;
            }

            if (input.GetLength(1) % 8 != 0)
            {
                height = (input.GetLength(1) / 8) + 1;
            }

            int numOfMatrices = width * height;

            double[][,] output = new double[numOfMatrices][,];

            for (int i = 0; i < numOfMatrices; i++)
            {
                output[i] = new double[8, 8];
            }

            int p = 0;
            for (int j = 0; j < input.GetLength(0); j += 8)
            {
                for (int i = 0; i < input.GetLength(1); i += 8)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            if ((j + y) < input.GetLength(0) && (i + x) < input.GetLength(1))
                            {
                                output[p][y, x] = input[j + y, i + x];
                            }
                            else
                            {
                                output[p][y, x] = 0;
                            }
                        }
                    }
                    p++;
                }
            }
            return output;
        }

        // Subsample Compression
        private double[] UpSample(double[] input, int originalWidth, int originalHeight)
        {
            double[] expanded1D = new double[originalWidth * originalHeight];

            int expandFactor = 2;

            int index = 0;
            for (int i = 0; i < expanded1D.Length; i++)
            {
                if (i % expandFactor == 0 && (int)Math.Floor(i / (double)originalWidth) % expandFactor == 0)
                {
                    for (int subH = 0; subH < expandFactor; subH++)
                    {
                        for (int subL = 0; subL < expandFactor; subL++)
                        {
                            if (i + subL < originalWidth * (int)(Math.Floor((double)i / originalWidth + 1)) &&
                                i + subL + subH * originalWidth < originalWidth * originalHeight)
                            {
                                expanded1D[i + subL + subH * originalWidth] = input[index];
                            }
                        }
                    }
                    index++;
                }
            }
            return expanded1D;
        }

        private double[] Subsample(double[] bArr, int width)
        {
            List<double> subsample = new List<double>();

            for (int i = 0; i < bArr.Length; i++)
            {
                if (i % 2 == 0 && Math.Floor((double)i / width) % 2 == 0)
                {
                    subsample.Add(bArr[i]);
                }  
            }

            return listToArrayDouble(subsample);
        }

        // Colour Space Conversion
        private double[] RGB2YCbCr(byte[] RGB, int width, int height)
        {
            double [] YCbCr = new double[RGB.Length];
            for (int i = 0; i < RGB.Length; i += 4)
            {
                double Y = (0.299 * RGB[i + 2] + 0.587 * RGB[i + 1] + 0.114 * RGB[i]);
                double Cb = (128 - (0.168736 * RGB[i + 2]) - (0.331264 * RGB[i + 1]) + (0.5 * RGB[i]));
                double Cr = (128 + (0.5 * RGB[i + 2]) - (0.418688 * RGB[i + 1]) - (0.081312 * RGB[i]));

                // YCrCb
                YCbCr[i] = Y;
                YCbCr[i + 1] = Cb;
                YCbCr[i + 2] = Cr;
                YCbCr[i + 3] = RGB[i + 3];
            }

            return YCbCr;
        }

        private byte[] RGB2YCrCb(byte[] RGB, int width, int height)
        {
            int bytesPerPixel = 4;
            byte[] YCrCb = new byte[RGB.Length];

            for (int i = 0; i < RGB.Length; i += bytesPerPixel)
            {
                byte Y = (byte)(0.299 * RGB[i + 2] + 0.587 * RGB[i + 1] + 0.114 * RGB[i]);
                byte Cb = (byte)(128 - (0.168736 * RGB[i + 2]) - (0.331264 * RGB[i + 1]) + (0.5 * RGB[i]));
                byte Cr = (byte)(128 + (0.5 * RGB[i + 2]) - (0.418688 * RGB[i + 1]) - (0.081312 * RGB[i]));
                YCrCb[i] = Y;
                YCrCb[i + 1] = Cr;
                YCrCb[i + 2] = Cb;
                YCrCb[i + 3] = RGB[i + 3];
            }
            return YCrCb;
        }

        private byte[] YCbCr2RGB(double[] YCbCr, int width, int height)
        {
            byte[] RGB = new byte[YCbCr.Length];
            int bytesPerPixel = 4;

            for (int i = 0; i < RGB.Length; i += bytesPerPixel)
            {
                double R = YCbCr[i] + 1.402 * (YCbCr[i + 2] - 128);
                double G = YCbCr[i] - 0.344136 * (YCbCr[i + 1] - 128) - 0.714136 * (YCbCr[i + 2] - 128);
                double B = YCbCr[i] + 1.772 * (YCbCr[i + 1] - 128);

                byte RByte = (byte)(R < 0 ? 0 : R > 255 ? 255 : R);
                byte GByte = (byte)(G < 0 ? 0 : G > 255 ? 255 : G);
                byte BByte = (byte)(B < 0 ? 0 : B > 255 ? 255 : B);

                RGB[i] = BByte;
                RGB[i + 1] = GByte;
                RGB[i + 2] = RByte;
                RGB[i + 3] = 255;
            }
            return RGB;
        }

        // MV
        private int mad(byte[][] C, byte[][] R, int x, int y, int p, int i, int j)
        {
            int result = 0;
            for (int k = 0; k < 8; k++)
            {
                for (int l = 0; l < 8; l++)
                {
                    result += Math.Abs(C[x + k ][y + l] - R[x + i + k][y + j + l]);
                }
            }
            return result;
        }

        private Point sequentialSearch(byte[][] source, byte[][] dest, int x, int y, int p)
        {
            int min = 2147483647;//large value
            for (int i = -p; i < p; i++)
                for (int j = -p; j < p; j++)
                {
                    int cur_MAD = mad(source, dest, x, y, p, i, j);
                    if (cur_MAD < min)
                    {
                        min = cur_MAD;
                        int u = i;
                        int v = j;
                    }
                }
            return new Point(x, y);
        }

        // Helper Functions
        private byte[] listToArrayByte(List<byte> list)
        {
            byte[] result = new byte[list.Count];

            for (int j = 0; j < list.Count; j++)
            {
                result[j] = list[j];
            }

            return result;
        }

        private double[] listToArrayDouble(List<double> list)
        {
            double[] result = new double[list.Count];

            for (int j = 0; j < list.Count; j++)
            {
                result[j] = list[j];
            }

            return result;
        }

        private int[] listToArrayInt(List<int> list)
        {
            int[] result = new int[list.Count];

            for (int j = 0; j < list.Count; j++)
            {
                result[j] = list[j];
            }

            return result;
        }

        private byte[,] OneDToTwoD(byte[] input, int width, int height)
        {   
            byte[,] output = new byte[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    output[i, j] = input[i * width + j];
                }
            }
            return output;
        }

        private double[,] OneDToTwoDDouble(double[] input, int width, int height)
        {
            double[,] output = new double[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    output[i, j] = input[i * width + j];
                }
            }
            return output;
        }

        private byte[] TwoDToOneD(byte[,] input)
        {   
            int width = input.GetLength(1);
            int height = input.GetLength(0);
            byte[] output = new byte[height * width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    output[i * width + j] = input[i, j];
                }
            }
            return output;
        }

        private double[] TwoDToOneDDouble(double[,] input)
        {
            int width = input.GetLength(1);
            int height = input.GetLength(0);
            double[] output = new double[height * width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    output[i * width + j] = input[i, j];
                }
            }
            return output;
        }

        // Convert Bitmap to Array and back
        private byte[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            int stride = (int)bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private BitmapSource BitmapSourceFromArray(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }

        // Calculation for JPEG Compression
        private double[,] DCT(double[,] input)
        {   
            int N = input.GetLength(1);
            int M = input.GetLength(0);
            double[,] output = new double[M, N];
            for (int u = 0; u < M; u++)
            {
                for (int v = 0; v < N; v++)
                {
                    double sum = 0;
                    double cOfU = (u == 0) ? (1 / Math.Sqrt(2)) : 1;
                    double cOfV = (v == 0) ? (1 / Math.Sqrt(2)) : 1;
                    for (int x = 0; x < M; x++)
                    {
                        for (int y = 0; y < N; y++)
                        {
                            sum += Math.Cos((double)((2 * x + 1) * u * Math.PI / (2 * (double)M))) *
                                   Math.Cos((double)((2 * y + 1) * v * Math.PI / (2 * (double)N))) *
                                   input[x, y];
                        }
                    }
                    sum *= (2 / Math.Sqrt(M * N)) * cOfU * cOfV;
                    output[u, v] = sum;
                }
            }
            return output;
        }

        private double[,] IDCT(double[,] input)
        {
            int N = input.GetLength(0);
            double[,] output = new double[N, N];
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    double sum = 0;
                    for (int u = 0; u < N; u++)
                    {
                        for (int v = 0; v < N; v++)
                        {
                            double cOfU = u == 0 ? 1 / Math.Sqrt(2) : 1;
                            double cOfV = v == 0 ? 1 / Math.Sqrt(2) : 1;
                            sum += cOfU * cOfV *
                                   Math.Cos((2 * x + 1) * (u * Math.PI) / (2 * (double)N)) *
                                   Math.Cos((2 * y + 1) * (v * Math.PI) / (2 * (double)N)) *
                                   input[u, v];
                        }
                    }
                    sum *= (2 / (double)N);
                    output[x, y] = sum;
                }
            }
            return output;
        }

        private double[,] Quantization(double[,] input, int[,] Q)
        {
            int N = input.GetLength(0);
            double[,] output = new double[N, N];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                   output[i, j] = input[i, j] / Q[i, j];
                }
            }
            return output;
        }

        private double[,] Dequantization(double[,] input, int[,] Q)
        {
            int N = input.GetLength(0);
            double[,] output = new double[N, N];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    output[i, j] = input[i, j] * Q[i, j];
                }
            }
            return output;
        }

        private int[] EntropyEncoding(double[,] input)
        {
            int matrixSize = 64;
            int N = input.GetLength(0);
            int[] input1D = new int[matrixSize];
            int[] straight = new int[matrixSize];
            List<int> outputList = new List<int>();

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    input1D[i * N + j] = (int)Math.Round(input[i, j]);
                }
            }

            for (int i = 0; i < matrixSize; i++)
            {
                straight[i] = input1D[MAP[i]];
            }

            for (int i = 0; i < straight.Length; i++)
            {
                int count = 1;
                while (i < straight.Length - 1 && straight[i] == straight[i + 1])
                {
                    count++;
                    i++;
                }
                if (count == 1 && straight[i] != 0)
                {
                    outputList.Add(straight[i]);
                } else
                {
                    outputList.Add(0);
                    outputList.Add(count);
                    outputList.Add(straight[i]);
                }
            }

            return listToArrayInt(outputList);
        }
        
        private byte[] EntropyEncodingTest(byte[,] input)
        {
            int M = input.GetLength(0);
            int N = input.GetLength(1);
            byte[] output = new byte[M * N];
            int x = 0;
            int y = 0;
            int i = 0;
            bool mode = false;

            while (x != N && y != M)
            {
                output[i] = input[y, x];
                if (y == M - 1)
                {
                    x += 1;
                    mode = false;
                    output[i] = input[y, x];
                }
                if (x == 0)
                {
                    y += 1;
                    mode = false;
                    output[i] = input[y, x];
                }
                if (y == 0 && x != N - 1)
                {
                    x += 1;
                    mode = true;
                    output[i] = input[y, x];
                }
                if (x == N - 1)
                {
                    y += 1;
                    mode = true;
                    output[i] = input[y, x];
                }
                if (!mode)
                {
                    x += 1;
                    y -= 1;
                }
                else
                {
                    x -= 1;
                    y += 1;
                }
                i++;
            }
            return output;
        }

        private double[,] EntropyDecoding(double[] input)
        {
            int matrixSize = 64;
           
            double[] zigZag = new double[matrixSize];

            for (int i = 0; i < matrixSize; i++)
            {
                zigZag[MAP[i]] = input[i];
            }
            return OneDToTwoDDouble(zigZag, 8, 8);
        }

    }
}

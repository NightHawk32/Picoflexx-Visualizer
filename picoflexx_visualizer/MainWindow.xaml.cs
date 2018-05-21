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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RoyaleDotNet;
using System.IO;
using System.Windows.Media.Media3D;

namespace cam_cs
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraManager manager = new CameraManager();
        private List<String> camlist;
        private CameraDevice cameraDevice;
        private bool capturing = false;
        private bool save_next_img = false;

        public MainWindow()
        {
            InitializeComponent();
            btnDetectCameras.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (btnOpenCamera.IsEnabled == true)
            {
                btnOpenCamera.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            btnStartCapture.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        }

        private void btnDetectCameras_Click(object sender, RoutedEventArgs e)
        {
            camlist = manager.GetConnectedCameraList();
            cbCameras.ItemsSource = camlist;
            if (camlist.Count != 0)
            {
                cbCameras.SelectedIndex = 0;
                btnOpenCamera.IsEnabled = true;
            }
            else
            {
                cbCameras.SelectedIndex = -1;
                btnOpenCamera.IsEnabled = false;
            }
        }

        private void btnOpenCamera_Click(object sender, RoutedEventArgs e)
        {
            cameraDevice = manager.CreateCamera(camlist[cbCameras.SelectedIndex]);
            if (cameraDevice == null)
            {
                MessageBox.Show("Failed to create camera device.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var ret = cameraDevice.Initialize();
            if (ret != CameraStatus.SUCCESS)
            {
                MessageBox.Show("Cannot initiralize the camera device. Error code:"+(int)ret, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string str;
            cameraDevice.GetCameraName(out str);
            tbCameraName.Text = str;

            ushort width;
            cameraDevice.GetMaxSensorWidth(out width);
            tbMaxWidth.Text = width.ToString();

            ushort height;
            cameraDevice.GetMaxSensorHeight(out height);
            tbMaxHeight.Text = height.ToString();

            List <string> useCases;
            cameraDevice.GetUseCases(out useCases);
            useCases.ForEach(item => cbUseCases.Items.Add(item));
            cbUseCases.SelectedIndex = 0;

            btnStartCapture.IsEnabled = true;
            cameraDevice.RegisterDepthDataListener(new DepthDataListener(this));
        }

        private void cbCameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCameras.SelectedIndex != -1)
            {
                btnOpenCamera.IsEnabled = true;
            }
            else
            {
                btnOpenCamera.IsEnabled = false;
            }
        }

        private void cbUseCases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cameraDevice != null)
            {
                CameraStatus stat = cameraDevice.SetUseCase(cbUseCases.SelectedValue.ToString());         
            }
        }

        private void btnStartCapture_Click(object sender, RoutedEventArgs e)
        {
            if (cameraDevice != null)
            {
                bool isCapturing;
                cameraDevice.IsCapturing(out isCapturing);
                if (!isCapturing)
                {
                    cameraDevice.StartCapture();
                    capturing = true;
                    btnStartCapture.Content = "Stop Capturing";
                }
                else
                {
                    cameraDevice.StopCapture();
                    capturing = false;
                    btnStartCapture.Content = "Start Capturing";
                }
                
            }
        }
        public void UpdateImage(DepthData data)
        {
            WriteableBitmap bmp = new WriteableBitmap(data.width, data.height, 96, 96, PixelFormats.Gray16, null);
            int stride = bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8);

            WriteableBitmap bmpDepthColor = new WriteableBitmap(data.width, data.height, 96, 96, PixelFormats.Bgr32, null);
            int strideDepthColor = bmpDepthColor.PixelWidth * (bmp.Format.BitsPerPixel / 8);


            ushort[] gray = new ushort[data.width * data.height];
            int i = 0;
            foreach (var x in data.points)
            {
                gray[i] = x.grayValue;
                i++;
            }
            int max = gray.Max();
            int min = gray.Min();
            for (int j = 0; j < gray.Count(); j++)
            {
                gray[j] = (ushort)map(gray[j], min, max, 0, ushort.MaxValue);
            }
            bmp.WritePixels(new Int32Rect(0, 0, data.width, data.height), gray, stride, 0);
            imgDataGray.Source = bmp;



            float[] depthF = new float[data.width * data.height];
            i = 0;
            foreach (var x in data.points)
            {
                depthF[i] = x.z;
                i++;
            }

            float maxDepth = depthF.Max();
            float minDepth = depthF.Min();

            byte[] colorPixels;
            colorPixels = new byte[depthF.Count() * sizeof(int)];
            int colorPixelIndex = 0;
            for (i = 0; i < depthF.Count(); ++i)
            {

                ushort intensity = (ushort)mapF(depthF[i], minDepth, maxDepth, 0, ushort.MaxValue);
                Color c = MapRainbowColor(intensity,ushort.MaxValue,0);
                colorPixels[colorPixelIndex++] = c.B;
                colorPixels[colorPixelIndex++] = c.G;
                colorPixels[colorPixelIndex++] = c.R;
                ++colorPixelIndex;
            }

            bmpDepthColor.WritePixels(new Int32Rect(0, 0, data.width, data.height), colorPixels, bmpDepthColor.PixelWidth *sizeof(int), 0);
            imgDataDepth.Source = bmpDepthColor;

            if (save_next_img)
            {
                save_next_img = false;
                SaveImage(bmp, "img/gray.jpg");
                SaveImage(bmpDepthColor, "img/color.jpg");

                SaveArrayAsCSV(gray, "img/gray.csv");
                SaveArrayAsCSV(depthF, "img/depth.csv");
            }

        }
        private long map(long x, long in_min, long in_max, long out_min, long out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
        private float mapF(float x, float in_min, float in_max, float out_min, float out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public static void SaveArrayAsCSV<T>(T[] arrayToSave, string fileName)
        {
            using (StreamWriter file = new StreamWriter(fileName))
            {
                foreach (T item in arrayToSave)
                {
                    file.Write(item + ";");
                }
            }
        }

        private Color MapRainbowColor(float value, float red_value, float blue_value)
        {
            // Convert into a value between 0 and 1023.
            int int_value = (int)(1023 * (value - red_value) /
                (blue_value - red_value));

            // Map different color bands.
            if (int_value < 256)
            {
                // Red to yellow. (255, 0, 0) to (255, 255, 0).
                return Color.FromRgb(255, (byte)int_value, 0);
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return Color.FromRgb((byte)(255 - int_value), 255, 0);
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return Color.FromRgb(0, 255, (byte)int_value);
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return Color.FromRgb(0, (byte)(255 - int_value), 255);
            }
        }

        private void SaveImage(WriteableBitmap img, string path)
        {
            FileStream stream = new FileStream(path, FileMode.Create);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));
            encoder.Save(stream);
            stream.Close();
        }

        private void btnSaveImagesClick(object sender, RoutedEventArgs e)
        {
            save_next_img = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

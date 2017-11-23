using Encog.ML.Data.Basic;
using Encog.ML.Data.Image;
using Encog.ML.Train.Strategy;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Persist;
using Encog.Util.Simple;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ODR
{
    public partial class MainWindow : Window
    {
        public const int DIGITS_COUNT = 10;
        public const int MNIST_LIMIT = 100;
        public const int DIGIT_HEIGHT = 8;
        public const int DIGIT_WIDTH = 6;

        private const string APP_NAME = "Optical Digit Recognition";

        public ObservableCollection<ImageCollection> Images { get; set; } = new ObservableCollection<ImageCollection>();
        public ObservableCollection<ResultObject> Results { get; set; } = new ObservableCollection<ResultObject>();

        private BasicNetwork network;

        public MainWindow()
        {
            for (var i = 0; i < DIGITS_COUNT; ++i)
            {
                Images.Insert(i, new ImageCollection());
                Results.Insert(i, new ResultObject {
                    Char = i.ToString(),
                    Result = 0
                });
            }
            InitializeComponent();
        }

        Point addCanvas_currentPoint = new Point();
        bool addCanvas_drawing = false;

        private void AddCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                addCanvas_currentPoint = e.GetPosition(AddCanvas);
        }

        private void AddCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            addCanvas_drawing = false;
        }

        private void AddCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            SolidColorBrush color = null;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                color = Brushes.Black;
            } else if (e.RightButton == MouseButtonState.Pressed)
            {
                color = Brushes.White;
            }
            else
            {
                return;
            }

            if (color != null)
            {
                Polyline polyLine;
                if (!addCanvas_drawing)
                {
                    polyLine = new Polyline
                    {
                        Stroke = color,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeThickness = 30
                    };
                    AddCanvas.Children.Add(polyLine);
                    addCanvas_drawing = true;
                }
                polyLine = (Polyline)AddCanvas.Children[AddCanvas.Children.Count - 1];
                addCanvas_currentPoint = e.GetPosition(AddCanvas);
                polyLine.Points.Add(addCanvas_currentPoint);
            }
        }

        private void AddCanvas_CleanClick(object sender, RoutedEventArgs e)
        {
            AddCanvas.Children.Clear();
        }

        private void AddCanvas_AddClick(object sender, RoutedEventArgs e)
        {
            var text = AddDigit.Text;
            if (text.Length != 1)
            {
                MessageBox.Show(this,
                    "Введите одну цифру.",
                    APP_NAME,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (text[0] < '0' || text[0] > '9')
            {
                MessageBox.Show(this,
                    "Введите цифру.",
                    APP_NAME,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var index = text[0] - '0';

            var size = new Size(AddCanvas.ActualWidth, AddCanvas.ActualHeight);
            // Measure and arrange the surface
            // VERY IMPORTANT
            AddCanvas.Measure(size);
            AddCanvas.Arrange(new Rect(size));
            var renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(AddCanvas);

            Images[index].Add(renderBitmap);
            AddCanvas_CleanClick(null, null);
        }

        private void AddMnist(object sender, RoutedEventArgs e)
        {
            var full = MessageBox.Show(this,
                    "Вы хотите загрузить MNIST целиком? Это займёт некоторое время.\nЕсли вы выберите \"Нет\", то будет загружена лишь часть набора данных.",
                    APP_NAME,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
            bool fullLoad;
            if (full == MessageBoxResult.Yes)
            {
                fullLoad = true;
            }
            else if (full == MessageBoxResult.No)
            {
                fullLoad = false;
            }
            else
            {
                return;
            }
            var dlg = new Forms.FolderBrowserDialog();
            Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            if (result != Forms.DialogResult.OK)
            {
                return;
            }
            var di = new DirectoryInfo(dlg.SelectedPath);
            foreach (DirectoryInfo d in di.GetDirectories())
            {
                var name = d.Name;
                if (name.Length != 1)
                {
                    continue;
                }
                
                if (name[0] < '0' || name[0] > '9')
                {
                    continue;
                }

                var index = name[0] - '0';
                var collection = Images[index];
                IEnumerable<FileInfo> files = d.GetFiles();
                if (!fullLoad)
                {
                    files = files.Take(MNIST_LIMIT);
                }
                foreach (FileInfo f in files)
                {
                    var img = new BitmapImage(new Uri(f.FullName));
                    collection.Add(img);
                }
            }
        }

        Point recCanvas_currentPoint = new Point();
        bool recCanvas_drawing = false;

        private void RecCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                recCanvas_currentPoint = e.GetPosition(RecCanvas);
        }

        private void RecCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            recCanvas_drawing = false;

            var size = new Size(RecCanvas.ActualWidth, RecCanvas.ActualHeight);
            // Measure and arrange the surface
            // VERY IMPORTANT
            RecCanvas.Measure(size);
            RecCanvas.Arrange(new Rect(size));
            var renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(RecCanvas);

            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            encoder.Save(stream);

            var bitmap = new Drawing.Bitmap(stream);

            var downsample = new Downsampler();
            var result = downsample.DownSample(bitmap, DIGIT_HEIGHT, DIGIT_WIDTH);
            var image = new Drawing.Bitmap(DIGIT_WIDTH, DIGIT_HEIGHT);
            for (var i = 0; i < DIGIT_HEIGHT; ++i)
            {
                for (var j = 0; j < DIGIT_WIDTH; ++j)
                {
                    image.SetPixel(j, i, Drawing.Color.FromArgb(
                        255,
                        (int)result[i * DIGIT_WIDTH + j],
                        (int)result[i * DIGIT_WIDTH + j],
                        (int)result[i * DIGIT_WIDTH + j]
                        ));
                }
            }
            RecComp.Source = Imaging.CreateBitmapSourceFromHBitmap(
                image.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (network == null)
            {
                return;
            }

            var input = new ImageMLData(bitmap);
            input.Downsample(downsample, false, DIGIT_HEIGHT, DIGIT_WIDTH, 1, -1);

            int winner = network.Winner(input);
            RecDigit.Text = winner.ToString();

            var data = network.Compute(input);

            for (var i = 0; i < DIGITS_COUNT; ++i)
            {
                Results[i] = new ResultObject
                {
                    Char = i.ToString(),
                    Result = (data[i] + 1) / 2
                };
            }
        }

        private void RecCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            SolidColorBrush color = null;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                color = Brushes.Black;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                color = Brushes.White;
            }
            else
            {
                return;
            }

            if (color != null)
            {
                Polyline polyLine;
                if (!recCanvas_drawing || RecCanvas.Children.Count == 0)
                {
                    polyLine = new Polyline
                    {
                        Stroke = color,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeThickness = 30
                    };
                    RecCanvas.Children.Add(polyLine);
                    recCanvas_drawing = true;
                }
                polyLine = (Polyline)RecCanvas.Children[RecCanvas.Children.Count - 1];
                recCanvas_currentPoint = e.GetPosition(RecCanvas);
                polyLine.Points.Add(recCanvas_currentPoint);
            }
        }

        private void RecCanvas_CleanClick(object sender, RoutedEventArgs e)
        {
            RecCanvas.Children.Clear();
        }

        private void Learn_Click(object sender, RoutedEventArgs e)
        {
            var downsample = new Downsampler();
            var training = new ImageMLDataSet(downsample, true, 1, -1);

            for (var i = 0; i < Images.Count; ++i)
            {
                var ideal = new BasicMLData(DIGITS_COUNT);
                for (int j = 0; j < DIGITS_COUNT; ++j)
                {
                    if (j == i)
                    {
                        ideal[j] = 1;
                    }
                    else
                    {
                        ideal[j] = -1;
                    }
                }
                foreach (var img in Images[i])
                {
                    MemoryStream stream = new MemoryStream();
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(img));
                    encoder.Save(stream);

                    var bitmap = new Drawing.Bitmap(stream);
                    var data = new ImageMLData(bitmap);
                    training.Add(data, ideal);
                }
            }

            training.Downsample(DIGIT_HEIGHT, DIGIT_WIDTH);

            network = EncogUtility.SimpleFeedForward(training.InputSize, 35, 0, training.IdealSize, true);

            double strategyError = 0.01;
            int strategyCycles = 2000;

            var train = new ResilientPropagation(network, training);
            //train.AddStrategy(new ResetStrategy(strategyError, strategyCycles));
            EncogUtility.TrainDialog(train, network, training);

            EncogDirectoryPersistence.SaveObject(new FileInfo("network.eg"), network);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            network = (BasicNetwork)EncogDirectoryPersistence.LoadObject(new FileInfo("network.eg"));
        }
    }
}

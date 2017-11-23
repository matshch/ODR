using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;

namespace ODR
{
    public partial class MainWindow : Window
    {
        public const int DIGITS_COUNT = 10;
        public const int MNIST_LIMIT = 100;

        public ObservableCollection<ImageCollection> Images { get; set; } = new ObservableCollection<ImageCollection>();

        public MainWindow()
        {
            for (var i = 0; i < DIGITS_COUNT; ++i)
            {
                Images.Insert(i, new ImageCollection());
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

            if (color != null)
            {
                Polyline polyLine;
                if (!addCanvas_drawing)
                {
                    polyLine = new Polyline
                    {
                        Stroke = color,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeThickness = 15
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
                    Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (text[0] < '0' || text[0] > '9')
            {
                MessageBox.Show(this,
                    "Введите цифру.",
                    Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var index = text[0] - '0';

            var size = new Size(AddCanvas.ActualWidth, AddCanvas.ActualHeight);
            var renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(AddCanvas);

            Images[index].Add(renderBitmap);
            AddCanvas_CleanClick(null, null);
        }

        private void AddMnist(object sender, RoutedEventArgs e)
        {
            var full = MessageBox.Show(this,
                    "Вы хотите загрузить MNIST целиком? Это займёт некоторое время.\nЕсли вы выберите \"Нет\", то будет загружена лишь часть набора данных.",
                    Title,
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
    }
}

using Encog.Util.DownSample;
using System.Drawing;

namespace ODR
{
    public class Downsampler : RgbDownsampler
    {
        public override double[] DownSample(Bitmap image, int height, int width)
        {
            Image = image;
            ProcessImage(image);
            FindBounds();
            SetSize(height, width);

            var result = new double[height * width];
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    DownSampleRegion(x, y);
                    result[index++] = (CurrentRed + CurrentBlue + CurrentGreen) / 3;
                }
            }

            return result;
        }
    }
}

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Utils
{
    public static class BitmapHelper
    {
        private static readonly ImageCodecInfo myImageCodecInfo = GetEncoderInfo("image/jpeg");
        private static readonly Encoder myEncoder = Encoder.Quality;
        //private static readonly MemoryStream Buffer = new MemoryStream();

        public static BitmapImage ToBitmapImage(this Bitmap bitmap, int quality = 75)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream Buffer = new MemoryStream();
            EncoderParameters myEncoderParameters = new EncoderParameters(1) { Param = new[] { new EncoderParameter(myEncoder, quality) } };
            bitmap.Save(Buffer, myImageCodecInfo, myEncoderParameters);
            Buffer.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = Buffer;
            bi.EndInit();
            return bi;
        }
        public static byte[] ToJpegBytes(this Bitmap bitmap, int quality = 75)
        {
            MemoryStream Buffer = new MemoryStream();
            EncoderParameters myEncoderParameters = new EncoderParameters(1) { Param = new[] { new EncoderParameter(myEncoder, quality) } };
            bitmap.Save(Buffer, myImageCodecInfo, myEncoderParameters);
            return Buffer.ToArray();
        }

        public static BitmapImage ToBitmapImage(this Bitmap bitmap, int width, int height)
        {
            if (width == 0 || height == 0)
                return bitmap.ToBitmapImage();
            if (bitmap.Width == width && bitmap.Height == height)
                return bitmap.ToBitmapImage();

            Bitmap newBitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(newBitmap))
                graphics.DrawImage(bitmap, 0, 0, width, height);
            return newBitmap.ToBitmapImage();
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            foreach (var item in ImageCodecInfo.GetImageEncoders())
            {
                if (item.MimeType == mimeType)
                    return item;
            }
            return null;
        }
    }
}
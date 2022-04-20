using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Shared
{
    public static class Globals
    {
        public static Models.Settings Settings = new Models.Settings();

        public static BitmapImage BitmapToImageSource(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        /// <summary>
        /// Reads data (structs and primitive types (except strings)) into a struct.
        /// </summary>
        /// <typeparam name="T">The type of struct.</typeparam>
        /// <param name="data">The data to be fitted.</param>
        /// <returns>The data interpreted as the given struct.</returns>
        static T ReadByteArrayIntoStruct<T>(byte[] data) where T : struct
        {
            unsafe // needed to use pointers
            {
                fixed (byte* p = &data[0]) // Fixed so GC doesn't move shit, point to the first element
                {
                    return (T)System.Runtime.InteropServices.Marshal.PtrToStructure(new IntPtr(p), typeof(T))!;
                }
            }
        }

        public static float Map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SteamShared
{
    public static class Globals
    {
        public static Models.Settings Settings = new Models.Settings();
        public static readonly string ArgumentPattern = "[\\\"\"].+?[\\\"\"]|[^ ]+";

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

        public static bool ComparePaths(string path1, string path2)
        {
            // If it's a file that exists, remove the name from either to get the directory
            if(File.Exists(path1))
                path1 = Path.GetDirectoryName(path1)!;
            if (File.Exists(path2))
                path1 = Path.GetDirectoryName(path2)!;

            if (path1 == null && path2 == null)
                // They're technically the same
                return true;
            else if (path1 == null || path2 == null)
                return false;

            // Take care of back and forward slashes
            path1 = Path.GetFullPath(path1);
            path2 = Path.GetFullPath(path2);

            // Add another temp folder at the back and get the name of its parent directory,
            // thus removing that temp folder again,
            // basically getting rid of a trailing \\ at the end, if existent
            path1 = Path.Combine(path1, "temp");
            path2 = Path.Combine(path2, "temp");

            path1 = Path.GetDirectoryName(path1)?.ToLower()!;
            path2 = Path.GetDirectoryName(path2)?.ToLower()!;

            return path1 == path2;
        }

        public static float Map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
    }
}

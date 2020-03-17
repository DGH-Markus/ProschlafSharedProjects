using Logging;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    /// <summary>
    /// Provides various compression methods for compressing images and files.
    /// For files, either the built-in System.IO.Compression.GZipStream class or the NuGet package "LZMA-SDK" is used to provide easy-to-use and scalable compression operations. 
    /// The LZMA algorithm offers far better compression levels compared to the built-in System.IO.Compression.GZipStream class, but is also slower. 
    /// </summary>
    public static class Compression
    {
        #region Vars
        const int DECOMPRESS_BUFFER_SIZE_BYTES = 8192;
        #endregion

        #region Enums
        /// <summary>
        /// <para>GZIP: The fastest, but also least effective algorithm.</para> 
        /// <para>"LZMA: Best compression ratio on the costs of speed.</para> 
        /// </summary>
        public enum CompressionTypes { GZIP, LZMA }
        #endregion

        /// <summary>
        /// Compresses the provided image using the specified target resolution and returns the compressed and resized image as JPEG.
        /// The aspect ratio is kept during the resizing process.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <returns></returns>
        public static byte[] CompressImage(byte[] img, int targetWidth, int targetHeight)
        {
            try
            {
                Stream stream = new MemoryStream(img);
                Bitmap originalImg = new Bitmap(stream);

                Size newSize = new Size(originalImg.Width, originalImg.Height);

                if (newSize.Width > targetWidth)
                {
                    double scale = (double)targetWidth / newSize.Width;
                    newSize = new Size((int)(newSize.Width * scale), (int)(newSize.Height * scale));

                    if (newSize.Width == targetWidth - 1) //correct the rounding "error" to get the correct target width
                        newSize.Width = targetWidth;
                }

                if (newSize.Height > targetHeight)
                {
                    double scale = (double)targetHeight / newSize.Height;
                    newSize = new Size((int)(newSize.Width * scale), (int)(newSize.Height * scale));

                    if (newSize.Height == targetHeight - 1) //correct the rounding "error" to get the exact target height
                        newSize.Height = targetHeight;
                }

                Bitmap resizedImg = new Bitmap(originalImg, newSize);

                MemoryStream outStream = new MemoryStream();
                resizedImg.Save(outStream, ImageFormat.Jpeg);

                return outStream.ToArray();
            }
            catch (Exception)
            {
                return img;
            }
        }

        /// <summary>
        /// Compresses the byte array using the specified method.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns>NULL if 'data' is invalid or something went wrong OR the compressed byte array.</returns>
        public static byte[] Compress(byte[] data, CompressionTypes type)
        {
            if (data == null)
                return data;

            if (data.Length > 100000000) //100MB
                return null;

            if (type == CompressionTypes.GZIP)
                return CompressFileGzip(data);
            else if (type == CompressionTypes.LZMA)
                return CompressFileLZMA(data);
            else
                return null;
        }

        /// <summary>
        /// Decompresses the byte array using the specified method.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns>NULL if 'data' is invalid or something went wrong OR the decompressed byte array.</returns>
        public static byte[] Decompress(byte[] data, CompressionTypes type)
        {
            if (data == null)
                return data;

            if (data.Length > 100000000) //100MB
                return null;

            if (type == CompressionTypes.GZIP)
                return DecompressFileGzip(data);
            else if (type == CompressionTypes.LZMA)
                return DecompressFileLZMA(data);
            else
                return null;
        }

        /// <summary>
        /// Compresses the provided file using the builtin GZipStream class with maximum compression level.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static byte[] CompressFileGzip(byte[] file)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionLevel.Optimal))
                {
                    gzip.Write(file, 0, file.Length);
                }

                return memory.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the provided file.
        /// </summary>
        /// <param name="compressedFile"></param>
        /// <returns></returns>
        private static byte[] DecompressFileGzip(byte[] compressedFile)
        {
            try
            {
                using (GZipStream stream = new GZipStream(new MemoryStream(compressedFile), CompressionMode.Decompress))
                {
                    byte[] buffer = new byte[DECOMPRESS_BUFFER_SIZE_BYTES];

                    using (MemoryStream memory = new MemoryStream())
                    {
                        int count = 0;

                        do
                        {
                            count = stream.Read(buffer, 0, buffer.Length);

                            if (count > 0)
                                memory.Write(buffer, 0, count);
                        }
                        while (count > 0);

                        return memory.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logging.Logger.LogEntryCategories.Warning, "Could not decompress file with length: " + compressedFile.Length + ". Returning original file.", ex, "Utils");
                return compressedFile;
            }
        }

        private static byte[] CompressFileLZMA(byte[] fileToCompress)
        {
            if (fileToCompress == null)
                return fileToCompress;

            if (fileToCompress.Length > 100000000) //100MB
                return null;

            return SevenZipHelper.Compress(fileToCompress);
        }

        private static byte[] DecompressFileLZMA(byte[] compressedFile)
        {
            if (compressedFile == null)
                return compressedFile;

            if (compressedFile.Length > 100000000) //100MB
                return null;

            return SevenZipHelper.Decompress(compressedFile);
        }
    }

    /// <summary>
    /// Helper class retrieved from https://sourceforge.net/p/sevenzip/discussion/45797/thread/d2235f09/?limit=25.
    /// Note that the version in use is self-compiled because the version contained in the NuGet-package 'LZMA-SDK' is not signed.
    /// To update the DLL, a new version has to be downloaded from GitHub, compiled and signed.
    /// Provides simple methods to compress/decompress byte arrays.
    /// </summary>
    internal static class SevenZipHelper
    {
        static int dictionary = 1 << 23;

        // static Int32 posStateBits = 2;
        // static  Int32 litContextBits = 3; // for normal files
        // UInt32 litContextBits = 0; // for 32-bit data
        // static  Int32 litPosBits = 0;
        // UInt32 litPosBits = 2; // for 32-bit data
        // static   Int32 algorithm = 2;
        // static    Int32 numFastBytes = 128;

        static bool eos = false;

        static CoderPropID[] propIDs =
                {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };

        // these are the default properties, keeping it simple for now:
        static object[] properties =
                {
                    (Int32)(dictionary),
                    (Int32)(2),
                    (Int32)(3),
                    (Int32)(0),
                    (Int32)(2),
                    (Int32)(128),
                    "bt4",
                    eos
                };

        public static byte[] Compress(byte[] inputBytes)
        {
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            MemoryStream inStream = new MemoryStream(inputBytes);
            MemoryStream outStream = new MemoryStream();

            encoder.WriteCoderProperties(outStream);

            long fileSize = inStream.Length;
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(fileSize >> (8 * i)));

            encoder.Code(inStream, outStream, -1, -1, null);
            return outStream.ToArray();
        }

        public static byte[] Decompress(byte[] inputBytes)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

            MemoryStream newInStream = new MemoryStream(inputBytes);
            newInStream.Position = 0;

            MemoryStream newOutStream = new MemoryStream();

            byte[] properties2 = new byte[5];
            if (newInStream.Read(properties2, 0, 5) != 5)
                throw (new Exception("Input .lzma is too short"));

            long outSize = 0;

            for (int i = 0; i < 8; i++)
            {
                int v = newInStream.ReadByte();

                if (v < 0)
                    throw (new Exception("Can't Read 1"));

                outSize |= ((long)(byte)v) << (8 * i);
            }

            decoder.SetDecoderProperties(properties2);

            long compressedSize = newInStream.Length - newInStream.Position;
            decoder.Code(newInStream, newOutStream, compressedSize, outSize, null);

            return newOutStream.ToArray();
        }
    }
}

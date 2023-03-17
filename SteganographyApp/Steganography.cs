using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace SteganographyApp
{
    public static class Steganography
    {
        public static void HideBytes(WriteableBitmap writableBitmap, byte[] bytesToHide, string fileExtension, string secretKey)
        {
            int seed = GetSeedFromSecretKey(secretKey);

            Random random = new Random(seed);
            int[] randomPositions = Enumerable.Range(0, writableBitmap.PixelWidth * writableBitmap.PixelHeight).OrderBy(x => random.Next()).ToArray();

            if (4 * bytesToHide.Length + 32 > randomPositions.Length)
            {
                throw new Exception("The image is too small to hide the file.");
            }

            writableBitmap.Lock();

            unsafe
            {
                byte* ptr = (byte*)writableBitmap.BackBuffer;
                int byteIndex = 0;

                // Hide the file extension
                byte[] extensionBytes = Encoding.UTF8.GetBytes(fileExtension.PadRight(8, ' '));
                for (int i = 0; i < extensionBytes.Length; i++)
                {
                    byte currentByte = extensionBytes[i];

                    for (int j = 0; j < 4; j++)
                    {
                        int position = randomPositions[byteIndex];
                        byte* pixel = ptr + position * 4;

                        pixel[0] = (byte)((pixel[0] & 0xFC) | (currentByte & 0x03));
                        currentByte >>= 2;

                        byteIndex++;
                    }
                }

                // Hide the file contents
                for (int i = 0; i < bytesToHide.Length; i++)
                {
                    byte currentByte = bytesToHide[i];

                    for (int j = 0; j < 4; j++)
                    {
                        int position = randomPositions[byteIndex];
                        byte* pixel = ptr + position * 4;

                        pixel[0] = (byte)((pixel[0] & 0xFC) | (currentByte & 0x03));
                        currentByte >>= 2;

                        byteIndex++;
                    }
                }
            }

            writableBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, writableBitmap.PixelWidth, writableBitmap.PixelHeight));
            writableBitmap.Unlock();
        }

        public static (string, byte[]) ExtractBytes(WriteableBitmap writableBitmap, string secretKey)
        {
            int seed = GetSeedFromSecretKey(secretKey);

            Random random = new Random(seed);
            int[] randomPositions = Enumerable.Range(0, writableBitmap.PixelWidth * writableBitmap.PixelHeight).OrderBy(x => random.Next()).ToArray();

            writableBitmap.Lock();

            unsafe
            {
                byte* ptr = (byte*)writableBitmap.BackBuffer;
                int byteIndex = 0;

                // Extract the file extension
                byte[] extensionBytes = new byte[8];
                for (int i = 0; i < extensionBytes.Length; i++)
                {
                    byte currentByte = 0;

                    for (int j = 0; j < 4; j++)
                    {
                        if (byteIndex >= randomPositions.Length)
                        {
                            throw new Exception("The image does not contain a hidden file.");
                        }

                        int position = randomPositions[byteIndex];
                        byte* pixel = ptr + position * 4;

                        currentByte |= (byte)((pixel[0] & 0x03) << (2 * j));

                        byteIndex++;
                    }

                    extensionBytes[i] = currentByte;
                }

                string fileExtension = Encoding.UTF8.GetString(extensionBytes).TrimEnd();

                // Extract the file contents
                int maxBytesToExtract = (randomPositions.Length - byteIndex - 32) / 4;
                byte[] extractedBytes = new byte[maxBytesToExtract];

                for (int i = 0; i < extractedBytes.Length; i++)
                {
                    byte currentByte = 0;

                    for (int j = 0; j < 4; j++)
                    {
                        if (byteIndex >= randomPositions.Length)
                        {
                            throw new Exception("The image does not contain a hidden file.");
                        }

                        int position = randomPositions[byteIndex];
                        byte* pixel = ptr + position * 4;

                        currentByte |= (byte)((pixel[0] & 0x03) << (2 * j));

                        byteIndex++;
                    }

                    extractedBytes[i] = currentByte;
                }

                writableBitmap.Unlock();

                return (fileExtension, extractedBytes);
            }
        }


        private static int GetSeedFromSecretKey(string secretKey)
        {
            byte[] hashedKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            return BitConverter.ToInt32(hashedKey, 0);
        }
    }
}

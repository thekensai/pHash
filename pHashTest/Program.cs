using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;

namespace ConsoleApplication4
{
    class Program
    {
        //[DllImport(@"C:\Users\Jerry.Wang\Downloads\pHash-0.9.4\Debug\pHash.dll", CharSet = CharSet.Ansi)]
        [DllImport(@"pHash.dll", CharSet = CharSet.Ansi)]
        public static extern int ph_dct_imagehash([MarshalAs(UnmanagedType.LPStr)] string file, ref ulong hash);

        //[DllImport(@"pHash.dll", CharSet = CharSet.Ansi)]
        //public static extern int ph_hamming_distance(ulong hashA, ulong hashB); 


        static void Main(string[] args)
        {

            var path = args.Length <= 0 ? Directory.GetCurrentDirectory() : args[0];

            var files = Directory.EnumerateFiles(path, "*.jpg").OrderBy(f => f).ToArray();

            ulong lastHash = 0;
            ph_dct_imagehash(files[0], ref lastHash);

            ulong hash = 0;

            for (int i = 1; i < 600; i++)
            {
                ph_dct_imagehash(files[i], ref hash);

                var dist = ph_hamming_distance(lastHash, hash);
                if (dist >= 26)
                {
                    Console.WriteLine($"{i / 60}:{i % 60}");
                }
                
                lastHash = hash;
            }
        }

        private static int ph_hamming_distance(ulong hash1, ulong hash2)
        {
            ulong x = hash1 ^ hash2;
            const ulong m1 = 0x5555555555555555UL;
            const ulong m2 = 0x3333333333333333UL;
            const ulong h01 = 0x0101010101010101UL;
            const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;
            x -= (x >> 1) & m1;
            x = (x & m2) + ((x >> 2) & m2);
            x = (x + (x >> 4)) & m4;
            return (int)((x * h01) >> 56);
        }

        private static void GetImages(DateTime dt)
        {
            List<IndexFileRecord> _indexFile = new List<IndexFileRecord>();

            var indexFilePath = String.Format(@"{0}{1}\{1}{2}000.sem",
                @"\\ljstorage\ice\HZHImageFiles\", "S1", GetHZHDateCode(dt));

            string _imageFile = Path.GetDirectoryName(indexFilePath) + "\\" + Path.GetFileNameWithoutExtension(indexFilePath) + ".pic";

            long lastOffset = 0;

            if (File.Exists(indexFilePath) && File.Exists(_imageFile))
            {
                using (FileStream fs = new FileStream(indexFilePath,
                                  FileMode.Open,
                                  FileAccess.Read,
                                  FileShare.ReadWrite))
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(_imageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            while (sr.Peek() >= 0)
                            {
                                var line = sr.ReadLine();
                                if (line != "")
                                {
                                    var rec = new IndexFileRecord(line, dt);
                                    
                                    reader.BaseStream.Seek(rec.ImageByteOffset - lastOffset, SeekOrigin.Current);

                                    lastOffset = rec.ImageByteOffset + rec.ImageByteLength;

                                    var jpegContent = reader.ReadBytes(rec.ImageByteLength);
                                    if (jpegContent.Length <= 0)
                                    {
                                        reader.Close();
                                        break;
                                    }

                                    File.WriteAllBytes(@"d:\perflogs\phash.jpg", jpegContent);

                                    ulong hash = 0;
                                    ph_dct_imagehash(@"d:\perflogs\phash.jpg", ref hash);
                                    rec.Hash = hash;

                                    _indexFile.Add(rec);
                                    //Bitmap.FromStream(new MemoryStream(jpegContent));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string GetHZHDateCode(DateTime dt)
        {
            var yearPart = dt.Year % 100;
            var monthPart = dt.Month;
            var dayPart = dt.Day;

            return String.Format("{0}{1}{2}", GetIntToBase36(yearPart), GetIntToBase36(monthPart), GetIntToBase36(dayPart));
        }

        private static char GetIntToBase36(int value)
        {
            if (value >= 0 && value <= 9)
                return (char)(value + 48);
            else if (value > 9 && value < 36)
                return (char)(value + 55);
            return '~';
        }

    }

    public class IndexFileRecord
    {
        public double SecondsFromMidnight { get; set; }
        public long ImageByteOffset { get; set; }
        public int ImageByteLength { get; set; }

        public DateTime Date { get; set; }

        public ulong Hash { get; set; }

        public IndexFileRecord()
        {

        }

        public IndexFileRecord(string indexFileLine, DateTime dt)
        {
            var fields = indexFileLine.Split(' ');

            SecondsFromMidnight = int.Parse(fields[0]) / 1000.0;
            ImageByteOffset = long.Parse(fields[2]);
            ImageByteLength = int.Parse(fields[3]);
            Date = dt;
        }
    }
}

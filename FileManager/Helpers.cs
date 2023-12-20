using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager
{
    /* Класс с полезными функциями */
    public class Helpers
    {
        /* Функция рекурсивно вычисляет размер папки */
        public static long DirSize(DirectoryInfo d)
        {
            long Size = 0;

            try
            {
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    Size += fi.Length;
                }

                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    Size += DirSize(di);
                }
            }
            catch { }


            return Size;
        }

        /* Перегрузка предыдущей функции с возможностью отмены асинхронной задачи по токену */
        public static long DirSize(DirectoryInfo d, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                return 0;
            }
            
            long Size = 0;

            try
            {
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    Size += fi.Length;
                }

                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    Size += DirSize(di, token);
                }
            }
            catch { }


            return Size;
        }

        /* Функция для вычисления форматированной строки размера файла/папки */
        private static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
            {
                // Нет цифр в дробной части
                return value.ToString("0,0");
            }
            else if (value >= 10)
            {
                // Одна цифра в дробной части
                return value.ToString("0.0");
            }
            else
            {
                // Две цифры в дробной части
                return value.ToString("0.00");
            }
        }

        /* Функция для вычисления понятного значения размера файла/папки */
        public static string ToFileSize(double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value /
                        Math.Pow(1024, i)) +
                        " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value /
                Math.Pow(1024, suffixes.Length - 1)) +
                " " + suffixes[suffixes.Length - 1];
        }

    }
}

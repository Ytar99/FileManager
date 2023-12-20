using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    /* Перечисление типов для элементов списка файлов */
    public enum ListItemType
    {
        None,       // заглушка
        Backward,   // кнопка назад
        File,       // файл
        Folder,     // папка
    }
}

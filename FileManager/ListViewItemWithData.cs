using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    /* Класс для сохранения дополнительной информации в элементах списка файлов */
    public class ListViewItemWithData : ListViewItem
    {
        private FileInfo fileData; // информация о файле (если это файл)
        private DirectoryInfo folderData; // информация о папке (если это папка)

        public ListItemType Type; // информация о типе – папка, файл или кнопка назад в списке

        public ListViewItemWithData()
        {
            this.fileData = null;
            this.folderData = null;
            this.Type = ListItemType.None;
        }

        public ListViewItemWithData(FileInfo info)
        {
            this.fileData = info;
            this.Type = ListItemType.File;
        }

        public ListViewItemWithData(DirectoryInfo info)
        {
            this.folderData = info;
            this.Type = ListItemType.Folder;
        }

        /* Метод для получения дополнительной информации в зависимости от контекста */
        /* требует явного приведения, например: DirectoryInfo d = (DirectoryInfo)item.GetData(); */
        public object GetData()
        {
            if (this.Type == ListItemType.File)
                return fileData;


            if (this.Type == ListItemType.Folder)
                return folderData;


            return null;
        }
    }
}

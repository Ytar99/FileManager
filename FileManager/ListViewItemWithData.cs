using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public class ListViewItemWithData : ListViewItem
    {
        private FileInfo fileData;
        private DirectoryInfo folderData;

        public ListItemType Type;

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

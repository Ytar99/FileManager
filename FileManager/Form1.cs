using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public enum Side
    {
        Left,
        Right,
    }

    public enum ListItemType
    {
        None,
        File,
        Folder,
    }

    public partial class Form1 : Form
    {
        private const string separator = "  |";
        private string currentPath1 = "";
        private DirectoryInfo currentDirectory1 = null;

        private string currentPath2 = "";
        private DirectoryInfo currentDirectory2 = null;

        private Task currentTaskLeft;
        CancellationTokenSource cancelTokenSourceLeft;

        private Task currentTaskRight;
        CancellationTokenSource cancelTokenSourceRight;

        public Form1()
        {
            InitializeComponent();
        }


        private void openFolder(string path, Side side)
        {
            try
            {

                DirectoryInfo info = new DirectoryInfo(path);

                if (!info.Exists) throw new ArgumentNullException("info");

                if (side == Side.Left)
                {
                    currentPath1 = path;
                    path1_TextBox.Text = currentPath1;
                    listView1.Items.Clear();
                    currentDirectory1 = info;
                }

                if (side == Side.Right)
                {
                    currentPath2 = path;
                    path2_TextBox.Text = currentPath2;
                    listView2.Items.Clear();
                    currentDirectory2 = info;
                }

                DirectoryInfo[] dir = info.GetDirectories();

                if (info.Parent != null && info.Parent.Name != "")
                {
                    ListViewItem listViewItem = new ListViewItemWithData();
                    listViewItem.Text = @"\..";
                    listViewItem.SubItems.Add("Папка");
                    listViewItem.SubItems.Add("");
                    listViewItem.SubItems.Add("");

                    if (side == Side.Left) listView1.Items.Add(listViewItem);
                    if (side == Side.Right) listView2.Items.Add(listViewItem);
                }

                foreach (DirectoryInfo item in dir)
                {
                    ListViewItem listViewItem = new ListViewItemWithData(item);
                    listViewItem.Text = item.Name; // Имя
                    listViewItem.SubItems.Add("Папка"); // Тип
                    listViewItem.SubItems.Add(""); // Размер
                    listViewItem.SubItems.Add(item.CreationTime.ToString()); // Дата создания
                    listViewItem.SubItems.Add(item.LastWriteTime.ToString()); // Дата изменения

                    if (side == Side.Left) listView1.Items.Add(listViewItem);
                    if (side == Side.Right) listView2.Items.Add(listViewItem);
                }

                FileInfo[] files = info.GetFiles();

                foreach (FileInfo item in files)
                {
                    ListViewItem listViewItem = new ListViewItemWithData(item);
                    listViewItem.Text = item.Name; // Имя
                    listViewItem.SubItems.Add(item.Extension); // Тип
                    listViewItem.SubItems.Add(Helpers.ToFileSize(item.Length)); // Размер
                    listViewItem.SubItems.Add(item.CreationTime.ToString()); // Дата создания
                    listViewItem.SubItems.Add(item.LastWriteTime.ToString()); // Дата изменения

                    if (side == Side.Left) listView1.Items.Add(listViewItem);
                    if (side == Side.Right) listView2.Items.Add(listViewItem);
                }

            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Недостаточно прав для выполнения операции: \n" + e.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void doubleClickHandler(ListView listView, Side side)
        {
            string currentPath = "";
            string nextPath = "";

            if (side == Side.Left) currentPath = currentPath1;
            if (side == Side.Right) currentPath = currentPath2;
            nextPath = currentPath;

            if (listView.SelectedItems[0].Text == @"\..")
            {
                int lastSlashIdx = nextPath.LastIndexOf(@"\");
                if (lastSlashIdx > -1)
                {
                    string prevPath = nextPath.Remove(lastSlashIdx);
                    if (prevPath.EndsWith(":")) prevPath += @"\";

                    openFolder(prevPath, side);
                }
                else
                {
                    openFolder(nextPath, side);
                }
            }
            else
            {
                if (!nextPath.EndsWith(@":\")) nextPath += @"\";

                if (listView.SelectedItems[0].SubItems[1].Text == "Папка")
                {
                    openFolder(nextPath + listView.SelectedItems[0].Text, side);
                }
            }
        }

        private void selectionChangeHandlerLeft(ListViewItemWithData item)
        {
            if (currentTaskLeft != null)
            {
                try
                {
                    cancelTokenSourceLeft.Cancel();
                    currentTaskLeft.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message.ToString());
                }
                currentTaskLeft.Dispose();
                cancelTokenSourceLeft.Dispose();

                currentTaskLeft = null;
                cancelTokenSourceLeft = null;
            }


            if (item == null || item.Type == ListItemType.None)
            {
                toolStripStatusLabel1_name.Text = "Имя: –" + separator;
                toolStripStatusLabel1_type.Text = "Тип: –" + separator;
                toolStripStatusLabel1_size.Text = "Размер: –" + separator;
                toolStripStatusLabel1_dateCreated.Text = "Создан: –" + separator;
                toolStripStatusLabel1_dateChanged.Text = "Изменён: –" + separator;

                return;
            }

            if (item.Type == ListItemType.Folder)
            {
                DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                toolStripStatusLabel1_name.Text = "Имя: " + directoryInfo.Name + separator;
                toolStripStatusLabel1_type.Text = "Тип: " + "Папка" + separator;
                toolStripStatusLabel1_size.Text = "Размер: " + "загрузка..." + separator;
                toolStripStatusLabel1_dateCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString() + separator;
                toolStripStatusLabel1_dateChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString() + separator;

                cancelTokenSourceLeft = new CancellationTokenSource();
                currentTaskLeft = Task.Run(() =>
                {
                    if (cancelTokenSourceLeft.Token.IsCancellationRequested)
                        cancelTokenSourceLeft.Token.ThrowIfCancellationRequested(); // генерируем исключение

                    toolStripStatusLabel1_size.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSourceLeft.Token)) + separator;
                }, cancelTokenSourceLeft.Token);
            }

            if (item.Type == ListItemType.File)
            {
                FileInfo fileInfo = (FileInfo)item.GetData();

                toolStripStatusLabel1_name.Text = "Имя: " + fileInfo.Name + separator;
                toolStripStatusLabel1_type.Text = "Тип: " + fileInfo.Extension + separator;
                toolStripStatusLabel1_size.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length) + separator;
                toolStripStatusLabel1_dateCreated.Text = "Создан: " + fileInfo.CreationTime.ToString() + separator;
                toolStripStatusLabel1_dateChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString() + separator;
            }
        }

        private void selectionChangeHandlerRight(ListViewItemWithData item)
        {
            if (currentTaskRight != null)
            {
                try
                {
                    cancelTokenSourceRight.Cancel();
                    currentTaskRight.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message.ToString());
                }
                currentTaskRight.Dispose();
                cancelTokenSourceRight.Dispose();

                currentTaskRight = null;
                cancelTokenSourceRight = null;
            }


            if (item == null || item.Type == ListItemType.None)
            {
                toolStripStatusLabel2_name.Text = "Имя: – |";
                toolStripStatusLabel2_type.Text = "Тип: – |";
                toolStripStatusLabel2_size.Text = "Размер: – |";
                toolStripStatusLabel2_dateCreated.Text = "Создан: – |";
                toolStripStatusLabel2_dateChanged.Text = "Изменён: – |";

                return;
            }

            if (item.Type == ListItemType.Folder)
            {
                DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                toolStripStatusLabel2_name.Text = "Имя: " + directoryInfo.Name + separator;
                toolStripStatusLabel2_type.Text = "Тип: " + "Папка" + separator;
                toolStripStatusLabel2_size.Text = "Размер: " + "загрузка..." + separator;
                toolStripStatusLabel2_dateCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString() + separator;
                toolStripStatusLabel2_dateChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString() + separator;

                cancelTokenSourceRight = new CancellationTokenSource();
                currentTaskRight = Task.Run(() =>
                {
                    if (cancelTokenSourceRight.Token.IsCancellationRequested)
                        cancelTokenSourceRight.Token.ThrowIfCancellationRequested(); // генерируем исключение

                    toolStripStatusLabel2_size.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSourceRight.Token)) + separator;
                }, cancelTokenSourceRight.Token);
            }

            if (item.Type == ListItemType.File)
            {
                FileInfo fileInfo = (FileInfo)item.GetData();

                toolStripStatusLabel2_name.Text = "Имя: " + fileInfo.Name + separator;
                toolStripStatusLabel2_type.Text = "Тип: " + fileInfo.Extension + separator;
                toolStripStatusLabel2_size.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length) + separator;
                toolStripStatusLabel2_dateCreated.Text = "Создан: " + fileInfo.CreationTime.ToString() + separator;
                toolStripStatusLabel2_dateChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString() + separator;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFolder(@"D:\", Side.Left);
            openFolder(@"D:\", Side.Right);

            toolStripStatusLabel1_name.Text = "Имя: –" + separator;
            toolStripStatusLabel1_type.Text = "Тип: –" + separator;
            toolStripStatusLabel1_size.Text = "Размер: –" + separator;
            toolStripStatusLabel1_dateCreated.Text = "Создан: –" + separator;
            toolStripStatusLabel1_dateChanged.Text = "Изменён: –" + separator;

            toolStripStatusLabel2_name.Text = "Имя: –" + separator;
            toolStripStatusLabel2_type.Text = "Тип: –" + separator;
            toolStripStatusLabel2_size.Text = "Размер: –" + separator;
            toolStripStatusLabel2_dateCreated.Text = "Создан: –" + separator;
            toolStripStatusLabel2_dateChanged.Text = "Изменён: –" + separator;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            doubleClickHandler(listView1, Side.Left);
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            doubleClickHandler(listView2, Side.Right);
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            ListViewItemWithData item = (ListViewItemWithData)e.Item;

            if (!e.IsSelected)
                item = null;


            selectionChangeHandlerLeft(item);
        }

        private void listView2_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            ListViewItemWithData item = (ListViewItemWithData)e.Item;

            if (!e.IsSelected)
                item = null;


            selectionChangeHandlerRight(item);
        }

        private void path1_TextBox_Leave(object sender, EventArgs e)
        {
            try
            {
                openFolder(path1_TextBox.Text, Side.Left);
            }
            catch
            {
                MessageBox.Show("Заданный путь не существует, введите корректное значение", "Некорректный путь", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                openFolder(currentPath1, Side.Left);
            }
        }

        private void path1_TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                splitContainer2.ActiveControl = null;
            }
        }

        private void path2_TextBox_Leave(object sender, EventArgs e)
        {
            try
            {
                openFolder(path2_TextBox.Text, Side.Right);
            }
            catch
            {
                MessageBox.Show("Заданный путь не существует, введите корректное значение", "Некорректный путь", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                openFolder(currentPath2, Side.Right);
            }
        }

        private void path2_TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                splitContainer3.ActiveControl = null;
            }
        }
    }
}

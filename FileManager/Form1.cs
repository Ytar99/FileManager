using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

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
        private string currentPath1 = "";
        private DirectoryInfo currentDirectory1 = null;

        private string currentPath2 = "";
        private DirectoryInfo currentDirectory2 = null;

        private Task currentTaskLeft;
        private CancellationTokenSource cancelTokenSourceLeft;

        private Task currentTaskRight;
        private CancellationTokenSource cancelTokenSourceRight;

        private TableLayoutSettings tableLayoutSettingsLeft;
        private TableLayoutSettings tableLayoutSettingsRight;

        private List<string> extensionsList1;
        private List<string> extensionsList2;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clearStatusStrip(Side.Left);
            clearStatusStrip(Side.Right);

            tableLayoutSettingsLeft = (TableLayoutSettings)statusStrip1.LayoutSettings;
            tableLayoutSettingsRight = (TableLayoutSettings)statusStrip2.LayoutSettings;

            extensionsList1 = new List<string>();
            extensionsList2 = new List<string>();

            openFolder(@"D:\", Side.Left);
            openFolder(@"D:\", Side.Right);
        }


        private void openFolder(string path, Side side, bool isRefresh = false)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(path);
                DirectoryInfo[] dir = info.GetDirectories();
                clearStatusStrip(side);
                if (!isRefresh) clearExtensions(side);

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

                comparePaths();

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

                    if (item.Attributes.HasFlag(FileAttributes.Hidden)) listViewItem.ForeColor = Color.Gray; // Скрытый
                    if (item.Attributes.HasFlag(FileAttributes.System)) listViewItem.ForeColor = Color.Blue; // Системный

                    if (side == Side.Left)
                    {
                        if (!extensionsList1.Contains("Папка"))
                            extensionsList1.Add("Папка");

                        if ((string)comboBox1_ext.SelectedItem == null || (string)comboBox1_ext.SelectedItem == "–" || (string)comboBox1_ext.SelectedItem == "Папка")
                        {
                            listView1.Items.Add(listViewItem);
                        }
                    }

                    if (side == Side.Right)
                    {
                        if (!extensionsList2.Contains("Папка"))
                            extensionsList2.Add("Папка");


                        if ((string)comboBox2_ext.SelectedItem == null || (string)comboBox2_ext.SelectedItem == "–" || (string)comboBox2_ext.SelectedItem == "Папка")
                        {
                            listView2.Items.Add(listViewItem);
                        }
                    }
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

                    if (item.Attributes.HasFlag(FileAttributes.Hidden)) listViewItem.ForeColor = Color.Gray; // Скрытый
                    if (item.Attributes.HasFlag(FileAttributes.System)) listViewItem.ForeColor = Color.Blue; // Системный

                    if (side == Side.Left)
                    {
                        if (!extensionsList1.Contains(item.Extension))
                            extensionsList1.Add(item.Extension);

                        if ((string)comboBox1_ext.SelectedItem == null || (string)comboBox1_ext.SelectedItem == "–" || (string)comboBox1_ext.SelectedItem == item.Extension)
                        {
                            listView1.Items.Add(listViewItem);
                        }
                    }
                    if (side == Side.Right)
                    {
                        if (!extensionsList2.Contains(item.Extension))
                            extensionsList2.Add(item.Extension);

                        if ((string)comboBox2_ext.SelectedItem == null || (string)comboBox2_ext.SelectedItem == "–" || (string)comboBox2_ext.SelectedItem == item.Extension)
                        {
                            listView2.Items.Add(listViewItem);
                        }
                    }
                }

                if (!isRefresh) fillComboBox(side);
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


        private void clearStatusStrip(Side side)
        {
            if (side == Side.Left)
            {
                toolStripStatusLabel1_name.Text = "Имя: –";
                toolStripStatusLabel1_type.Text = "Тип: –";
                toolStripStatusLabel1_size.Text = "Размер: –";
                toolStripStatusLabel1_dateCreated.Text = "Создан: –";
                toolStripStatusLabel1_dateChanged.Text = "Изменён: –";
                toolStripStatusLabel1_attr.Text = "Атрибуты: –";
            }

            if (side == Side.Right)
            {
                toolStripStatusLabel2_name.Text = "Имя: –";
                toolStripStatusLabel2_type.Text = "Тип: –";
                toolStripStatusLabel2_size.Text = "Размер: –";
                toolStripStatusLabel2_dateCreated.Text = "Создан: –";
                toolStripStatusLabel2_dateChanged.Text = "Изменён: –";
                toolStripStatusLabel2_attr.Text = "Атрибуты: –";
            }
        }

        private void comparePaths()
        {
            if (currentPath1 == currentPath2)
            {
                button1_moveToRight.Enabled = false;
                button1_copyToRight.Enabled = false;
                button2_moveToLeft.Enabled = false;
                button2_copyToLeft.Enabled = false;
            }
            else
            {
                button1_moveToRight.Enabled = true;
                button1_copyToRight.Enabled = true;
                button2_moveToLeft.Enabled = true;
                button2_copyToLeft.Enabled = true;
            }
        }

        private void clearExtensions(Side side)
        {
            if (side == Side.Left)
            {
                extensionsList1.Clear();
            }

            if (side == Side.Right)
            {
                extensionsList2.Clear();
            }
        }

        private void fillComboBox(Side side)
        {
            if (side == Side.Left)
            {
                comboBox1_ext.Items.Clear();
                comboBox1_ext.Items.Add("–");
                foreach (var item in extensionsList1)
                {
                    comboBox1_ext.Items.Add(item);
                }
                comboBox1_ext.SelectedIndex = 0;
            }

            if (side == Side.Right)
            {
                comboBox2_ext.Items.Clear();
                comboBox2_ext.Items.Add("–");
                foreach (var item in extensionsList2)
                {
                    comboBox2_ext.Items.Add(item);
                }
                comboBox2_ext.SelectedIndex = 0;
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
                clearStatusStrip(Side.Left);
                button1_delete.Enabled = false;

                return;
            }
            button1_delete.Enabled = true;

            if (item.Type == ListItemType.Folder)
            {
                DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                toolStripStatusLabel1_name.Text = "Имя: " + directoryInfo.Name;
                toolStripStatusLabel1_type.Text = "Тип: " + "Папка";
                toolStripStatusLabel1_size.Text = "Размер: " + "загрузка...";
                toolStripStatusLabel1_dateCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString();
                toolStripStatusLabel1_dateChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString();
                toolStripStatusLabel1_attr.Text = "Атрибуты: " + directoryInfo.Attributes.ToString();

                cancelTokenSourceLeft = new CancellationTokenSource();
                currentTaskLeft = Task.Run(() =>
                {
                    if (cancelTokenSourceLeft.Token.IsCancellationRequested)
                        cancelTokenSourceLeft.Token.ThrowIfCancellationRequested(); // генерируем исключение

                    toolStripStatusLabel1_size.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSourceLeft.Token));
                }, cancelTokenSourceLeft.Token);
            }

            if (item.Type == ListItemType.File)
            {
                FileInfo fileInfo = (FileInfo)item.GetData();

                toolStripStatusLabel1_name.Text = "Имя: " + fileInfo.Name;
                toolStripStatusLabel1_type.Text = "Тип: " + fileInfo.Extension;
                toolStripStatusLabel1_size.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length);
                toolStripStatusLabel1_dateCreated.Text = "Создан: " + fileInfo.CreationTime.ToString();
                toolStripStatusLabel1_dateChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString();
                toolStripStatusLabel1_attr.Text = "Атрибуты: " + fileInfo.Attributes.ToString();
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
                clearStatusStrip(Side.Right);
                button2_delete.Enabled = false;

                return;
            }
            button2_delete.Enabled = true;

            if (item.Type == ListItemType.Folder)
            {
                DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                toolStripStatusLabel2_name.Text = "Имя: " + directoryInfo.Name;
                toolStripStatusLabel2_type.Text = "Тип: " + "Папка";
                toolStripStatusLabel2_size.Text = "Размер: " + "загрузка...";
                toolStripStatusLabel2_dateCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString();
                toolStripStatusLabel2_dateChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString();
                toolStripStatusLabel2_attr.Text = "Атрибуты: " + directoryInfo.Attributes.ToString();


                cancelTokenSourceRight = new CancellationTokenSource();
                currentTaskRight = Task.Run(() =>
                {
                    if (cancelTokenSourceRight.Token.IsCancellationRequested)
                        cancelTokenSourceRight.Token.ThrowIfCancellationRequested(); // генерируем исключение

                    toolStripStatusLabel2_size.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSourceRight.Token));
                }, cancelTokenSourceRight.Token);
            }

            if (item.Type == ListItemType.File)
            {
                FileInfo fileInfo = (FileInfo)item.GetData();

                toolStripStatusLabel2_name.Text = "Имя: " + fileInfo.Name;
                toolStripStatusLabel2_type.Text = "Тип: " + fileInfo.Extension;
                toolStripStatusLabel2_size.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length);
                toolStripStatusLabel2_dateCreated.Text = "Создан: " + fileInfo.CreationTime.ToString();
                toolStripStatusLabel2_dateChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString();
                toolStripStatusLabel2_attr.Text = "Атрибуты: " + fileInfo.Attributes.ToString();
            }
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
                if (path1_TextBox.Text != currentPath1)
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
                e.Handled = true;
            }
        }

        private void path2_TextBox_Leave(object sender, EventArgs e)
        {
            try
            {
                if (path2_TextBox.Text != currentPath2)
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
                e.Handled = true;
            }
        }

        private void path1_TextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            int selection = textbox.SelectionStart;
            textbox.Text = textbox.Text.Replace("/", @"\");
            textbox.SelectionStart = selection;
            textbox.SelectionLength = 0;
        }

        private void path2_TextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            int selection = textbox.SelectionStart;
            textbox.Text = textbox.Text.Replace("/", @"\");
            textbox.SelectionStart = selection;
            textbox.SelectionLength = 0;
        }

        private void statusStrip1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            StatusStrip statusStrip = sender as StatusStrip;

            statusStrip.ResumeLayout(false);
            if (statusStrip.LayoutStyle == ToolStripLayoutStyle.Table)
            {
                statusStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
            }
            else if (statusStrip.LayoutStyle == ToolStripLayoutStyle.VerticalStackWithOverflow)
            {
                statusStrip.LayoutSettings = tableLayoutSettingsLeft;
                statusStrip.LayoutStyle = ToolStripLayoutStyle.Table;
            }
            statusStrip.PerformLayout();
        }

        private void statusStrip2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            StatusStrip statusStrip = sender as StatusStrip;

            statusStrip.ResumeLayout(false);
            if (statusStrip.LayoutStyle == ToolStripLayoutStyle.Table)
            {
                statusStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
            }
            else if (statusStrip.LayoutStyle == ToolStripLayoutStyle.VerticalStackWithOverflow)
            {
                statusStrip.LayoutSettings = tableLayoutSettingsRight;
                statusStrip.LayoutStyle = ToolStripLayoutStyle.Table;
            }
            statusStrip.PerformLayout();
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            openFolder(currentPath1, Side.Left, true);
        }

        private void comboBox2_ext_SelectedValueChanged(object sender, EventArgs e)
        {
            openFolder(currentPath2, Side.Right, true);
        }
    }
}

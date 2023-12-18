﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    internal class FileManagerController
    {
        public TextBox PathTextbox;
        public ComboBox ExtensionsDropdown;
        public DateTimePicker DateFrom;
        public DateTimePicker DateTo;
        public TextBox SearchTextbox;
        public Button DeleteButton;
        public Button MoveButton;
        public Button CopyButton;
        public ListView FilesList;
        public StatusStrip StatusBar;
        public ToolStripStatusLabel StatusName;
        public ToolStripStatusLabel StatusType;
        public ToolStripStatusLabel StatusSize;
        public ToolStripStatusLabel StatusCreated;
        public ToolStripStatusLabel StatusChanged;
        public ToolStripStatusLabel StatusAttr;
        public List<string> ExtensionsList;

        private Form1 mainForm;
        private FileManagerController anotherController;

        public FileManagerController AnotherController
        {
            get
            {
                return anotherController;
            }

            set
            {
                anotherController = value;
                OpenFolder(@"D:\");
            }
        }

        private TableLayoutSettings tableLayoutSettings;
        private string currentPath;

        private Task currentTask;
        private CancellationTokenSource cancelTokenSource;

        public FileManagerController(
            Form1 mainForm,
            TextBox pathTextbox,
            ComboBox extDropdown,
            DateTimePicker dateFrom,
            DateTimePicker dateTo,
            TextBox searchTextbox,
            Button deleteButton,
            Button moveButton,
            Button copyButton,
            ListView listView,
            StatusStrip statusStrip
            )
        {
            this.mainForm = mainForm;
            this.PathTextbox = pathTextbox;
            this.ExtensionsDropdown = extDropdown;
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
            this.SearchTextbox = searchTextbox;
            this.DeleteButton = deleteButton;
            this.MoveButton = moveButton;
            this.CopyButton = copyButton;
            this.FilesList = listView;
            this.StatusBar = statusStrip;
            this.tableLayoutSettings = (TableLayoutSettings)statusStrip.LayoutSettings;
            this.StatusName = (ToolStripStatusLabel)statusStrip.Items[0];
            this.StatusType = (ToolStripStatusLabel)statusStrip.Items[1];
            this.StatusSize = (ToolStripStatusLabel)statusStrip.Items[2];
            this.StatusCreated = (ToolStripStatusLabel)statusStrip.Items[3];
            this.StatusChanged = (ToolStripStatusLabel)statusStrip.Items[4];
            this.StatusAttr = (ToolStripStatusLabel)statusStrip.Items[5];
            this.ExtensionsList = new List<string>();

            ClearStatusStrip();

            this.PathTextbox.TextChanged += new System.EventHandler(this.PathTextbox_TextChangedHandler);
            this.PathTextbox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PathTextbox_KeyUpHandler);
            this.PathTextbox.Leave += new System.EventHandler(this.PathTextbox_LeaveHandler);

            this.ExtensionsDropdown.SelectedValueChanged += new System.EventHandler(this.ExtensionDropdown_SelectedValueChanged);

            this.FilesList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.File_SelectionChangeHandler);
            this.FilesList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.File_DoubleClickHandler);

            this.StatusBar.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.StatusBar_MouseDoubleClick);
        }

        private void ClearStatusStrip()
        {
            StatusName.Text = "Имя: –";
            StatusType.Text = "Тип: –";
            StatusSize.Text = "Размер: –";
            StatusCreated.Text = "Создан: –";
            StatusChanged.Text = "Изменён: –";
            StatusAttr.Text = "Атрибуты: –";
        }

        private void ClearExtensionsList()
        {
            ExtensionsList.Clear();
        }

        private void ClearFilesList()
        {
            FilesList.Items.Clear();
        }

        private void FillExtensionsDropdown()
        {
            ExtensionsDropdown.Items.Clear();
            ExtensionsDropdown.Items.Add("–");
            foreach (var item in ExtensionsList)
            {
                ExtensionsDropdown.Items.Add(item);
            }
            ExtensionsDropdown.SelectedIndex = 0;
        }

        private void ComparePaths()
        {
            if (currentPath == AnotherController.currentPath)
            {
                MoveButton.Enabled = false;
                CopyButton.Enabled = false;
                AnotherController.MoveButton.Enabled = false;
                AnotherController.CopyButton.Enabled = false;
            }
            else
            {
                MoveButton.Enabled = true;
                CopyButton.Enabled = true;
                AnotherController.MoveButton.Enabled = true;
                AnotherController.CopyButton.Enabled = true;
            }
        }

        private void OpenFolder(string path, bool isRefresh = false)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(path);
                DirectoryInfo[] dir = info.GetDirectories();
                ClearStatusStrip();
                if (!isRefresh) ClearExtensionsList();

                if (!info.Exists) throw new ArgumentNullException("info");

                currentPath = path;
                PathTextbox.Text = currentPath;
                ClearFilesList();

                ComparePaths();

                if (info.Parent != null && info.Parent.Name != "")
                {
                    ListViewItem listViewItem = new ListViewItemWithData();
                    listViewItem.Text = @"\..";
                    listViewItem.SubItems.Add("Папка");
                    listViewItem.SubItems.Add("");
                    listViewItem.SubItems.Add("");

                    FilesList.Items.Add(listViewItem);
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


                    if (!ExtensionsList.Contains("Папка"))
                        ExtensionsList.Add("Папка");

                    if ((string)ExtensionsDropdown.SelectedItem == null || (string)ExtensionsDropdown.SelectedItem == "–" || (string)ExtensionsDropdown.SelectedItem == "Папка")
                    {
                        FilesList.Items.Add(listViewItem);
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


                    if (!ExtensionsList.Contains(item.Extension))
                        ExtensionsList.Add(item.Extension);

                    if ((string)ExtensionsDropdown.SelectedItem == null || (string)ExtensionsDropdown.SelectedItem == "–" || (string)ExtensionsDropdown.SelectedItem == item.Extension)
                    {
                        FilesList.Items.Add(listViewItem);
                    }

                }

                if (!isRefresh) FillExtensionsDropdown();
            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Недостаточно прав для выполнения операции: \n" + e.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void File_SelectionChangeHandler(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            ListViewItemWithData item = (ListViewItemWithData)e.Item;

            if (!e.IsSelected)
                item = null;

            if (currentTask != null)
            {
                try
                {
                    cancelTokenSource.Cancel();
                    currentTask.Wait();
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.Message.ToString());
                }
                currentTask.Dispose();
                cancelTokenSource.Dispose();

                currentTask = null;
                cancelTokenSource = null;
            }


            if (item == null || item.Type == ListItemType.None)
            {
                ClearStatusStrip();
                DeleteButton.Enabled = false;

                return;
            }
            DeleteButton.Enabled = true;

            if (item.Type == ListItemType.Folder)
            {
                DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                StatusName.Text = "Имя: " + directoryInfo.Name;
                StatusType.Text = "Тип: " + "Папка";
                StatusSize.Text = "Размер: " + "загрузка...";
                StatusCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString();
                StatusChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString();
                StatusAttr.Text = "Атрибуты: " + directoryInfo.Attributes.ToString();

                cancelTokenSource = new CancellationTokenSource();
                currentTask = Task.Run(() =>
                {
                    if (cancelTokenSource.Token.IsCancellationRequested)
                        cancelTokenSource.Token.ThrowIfCancellationRequested(); // генерируем исключение

                    StatusSize.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSource.Token));
                }, cancelTokenSource.Token);
            }

            if (item.Type == ListItemType.File)
            {
                FileInfo fileInfo = (FileInfo)item.GetData();

                StatusName.Text = "Имя: " + fileInfo.Name;
                StatusType.Text = "Тип: " + fileInfo.Extension;
                StatusSize.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length);
                StatusCreated.Text = "Создан: " + fileInfo.CreationTime.ToString();
                StatusChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString();
                StatusAttr.Text = "Атрибуты: " + fileInfo.Attributes.ToString();
            }
        }

        private void File_DoubleClickHandler(object sender, MouseEventArgs e)
        {
            string curPath = currentPath;
            string nextPath = currentPath;

            if (FilesList.SelectedItems[0].Text == @"\..")
            {
                int lastSlashIdx = nextPath.LastIndexOf(@"\");
                if (lastSlashIdx > -1)
                {
                    string prevPath = nextPath.Remove(lastSlashIdx);
                    if (prevPath.EndsWith(":")) prevPath += @"\";

                    OpenFolder(prevPath);
                }
                else
                {
                    OpenFolder(nextPath);
                }
            }
            else
            {
                if (!nextPath.EndsWith(@":\")) nextPath += @"\";

                if (FilesList.SelectedItems[0].SubItems[1].Text == "Папка")
                {
                    OpenFolder(nextPath + FilesList.SelectedItems[0].Text);
                }
            }
        }

        private void PathTextbox_LeaveHandler(object sender, EventArgs e)
        {
            try
            {
                if (PathTextbox.Text != currentPath)
                    OpenFolder(PathTextbox.Text);
            }
            catch
            {
                MessageBox.Show("Заданный путь не существует, введите корректное значение", "Некорректный путь", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                OpenFolder(currentPath);
            }
        }

        private void PathTextbox_KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                mainForm.ActiveControl = null;
                e.Handled = true;
            }
        }

        private void PathTextbox_TextChangedHandler(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            int selection = textbox.SelectionStart;
            textbox.Text = textbox.Text.Replace("/", @"\");
            textbox.SelectionStart = selection;
            textbox.SelectionLength = 0;
        }

        private void StatusBar_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            StatusStrip statusStrip = sender as StatusStrip;

            statusStrip.ResumeLayout(false);
            if (statusStrip.LayoutStyle == ToolStripLayoutStyle.Table)
            {
                statusStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
            }
            else if (statusStrip.LayoutStyle == ToolStripLayoutStyle.VerticalStackWithOverflow)
            {
                statusStrip.LayoutSettings = tableLayoutSettings;
                statusStrip.LayoutStyle = ToolStripLayoutStyle.Table;
            }
            statusStrip.PerformLayout();
        }

        private void ExtensionDropdown_SelectedValueChanged(object sender, EventArgs e)
        {
            OpenFolder(currentPath, true);
        }
    }
}
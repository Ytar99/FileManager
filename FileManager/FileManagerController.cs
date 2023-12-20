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
    /* Контроллер управляет необходимыми элементами управления основной формы */
    internal class FileManagerController
    {
        private Form1 mainForm;
        private FileManagerController anotherController;

        private TableLayoutSettings tableLayoutSettings;
        private string currentPath;

        private Task currentTask;
        private CancellationTokenSource cancelTokenSource;

        private bool isSearchMode = false;

        /* Блок описания всех элементов контроллера*/
        public TextBox PathTextbox;
        public ComboBox ExtensionsDropdown;
        public DateTimePicker DateFrom;
        public DateTimePicker DateTo;
        public CheckBox DateFilter;
        public RadioButton RadioDateCreated;
        public RadioButton RadioDateChanged;
        public TextBox SearchTextbox;
        public Button DeleteButton;
        public Button SearchButton;
        public Button MoveButton;
        public Button CopyButton;
        public Button CreateButton;
        public Button RefreshButton;
        public ListView FilesView;
        public StatusStrip StatusBar;
        public ToolStripStatusLabel StatusName;
        public ToolStripStatusLabel StatusType;
        public ToolStripStatusLabel StatusSize;
        public ToolStripStatusLabel StatusCreated;
        public ToolStripStatusLabel StatusChanged;
        public ToolStripStatusLabel StatusAttr;
        public List<string> ExtensionsList;
        public List<ListViewItemWithData> FilesList;

        public FileManagerController AnotherController // привязка одного контроллера к другому
        {
            get { return anotherController; }
            set
            {
                anotherController = value;
                OpenFolder(@"D:\");
            }
        }

        public bool IsSearchMode // активирован ли режим поиска
        {
            get { return isSearchMode; }
            set { isSearchMode = value; }
        }

        /* Иницилизирующий конструктор контроллера */
        /* принимает все нужные элементы формы и производит начальную конфигурацию */
        public FileManagerController(
            Form1 mainForm,
            TextBox pathTextbox,
            ComboBox extDropdown,
            DateTimePicker dateFrom,
            DateTimePicker dateTo,
            CheckBox dateFilter,
            RadioButton radioDateCreated,
            RadioButton radioDateChanged,
            TextBox searchTextbox,
            Button searchButton,
            Button deleteButton,
            Button moveButton,
            Button copyButton,
            Button createButton,
            Button refreshButton,
            ListView listView,
            StatusStrip statusStrip
            )
        {
            this.mainForm = mainForm;
            this.PathTextbox = pathTextbox;
            this.ExtensionsDropdown = extDropdown;
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
            this.DateFilter = dateFilter;
            this.RadioDateCreated = radioDateCreated;
            this.RadioDateChanged = radioDateChanged;
            this.SearchTextbox = searchTextbox;
            this.SearchButton = searchButton;
            this.DeleteButton = deleteButton;
            this.MoveButton = moveButton;
            this.CopyButton = copyButton;
            this.CreateButton = createButton;
            this.RefreshButton = refreshButton;
            this.FilesView = listView;
            this.StatusBar = statusStrip;
            this.tableLayoutSettings = (TableLayoutSettings)statusStrip.LayoutSettings;
            this.StatusName = (ToolStripStatusLabel)statusStrip.Items[0];
            this.StatusType = (ToolStripStatusLabel)statusStrip.Items[1];
            this.StatusSize = (ToolStripStatusLabel)statusStrip.Items[2];
            this.StatusCreated = (ToolStripStatusLabel)statusStrip.Items[3];
            this.StatusChanged = (ToolStripStatusLabel)statusStrip.Items[4];
            this.StatusAttr = (ToolStripStatusLabel)statusStrip.Items[5];
            this.ExtensionsList = new List<string>();
            this.FilesList = new List<ListViewItemWithData>();

            ClearStatusStrip();

            /* РУЧНАЯ УСТАНОВКА СОБЫТИЙ НА ЭЛЕМЕНТЫ */
            this.SearchTextbox.TextChanged += new System.EventHandler(this.SearchTextbox_TextChangedHandler);
            this.PathTextbox.TextChanged += new System.EventHandler(this.PathTextbox_TextChangedHandler);
            this.PathTextbox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PathTextbox_KeyUpHandler);
            this.PathTextbox.Leave += new System.EventHandler(this.PathTextbox_LeaveHandler);
            this.ExtensionsDropdown.SelectedValueChanged += new System.EventHandler(this.ExtensionDropdown_SelectedValueChanged);
            this.FilesView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.FilesView_SelectionChangeHandler);
            this.FilesView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.FilesView_DoubleClickHandler);
            this.StatusBar.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.StatusBar_MouseDoubleClick);
            this.SearchButton.Click += new System.EventHandler(this.SearchButton_ClickHandler);
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_ClickHandler);
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_ClickHandler);
            this.MoveButton.Click += new System.EventHandler(this.MoveButton_ClickHandler);
            this.CopyButton.Click += new System.EventHandler(this.CopyButton_ClickHandler);
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_ClickHandler);
            this.DateFilter.CheckedChanged += new System.EventHandler(this.DateFilter_CheckHandler);
            this.DateFrom.ValueChanged += new System.EventHandler(this.DateFrom_ChangedHandler);
            this.DateTo.ValueChanged += new System.EventHandler(this.DateTo_ChangedHandler);
            this.RadioDateCreated.CheckedChanged += new System.EventHandler(this.RadioDate_ChangedHandler);
        }

        /* Функция очистки нижней статусной строки */
        private void ClearStatusStrip()
        {
            StatusName.Text = "Имя: –";
            StatusType.Text = "Тип: –";
            StatusSize.Text = "Размер: –";
            StatusCreated.Text = "Создан: –";
            StatusChanged.Text = "Изменён: –";
            StatusAttr.Text = "Атрибуты: –";
        }

        /* Функция очистки листа типов файлов */
        private void ClearExtensionsList()
        {
            ExtensionsList.Clear();
        }

        /* Функция очистки скрытого списка файлов */
        private void ClearFilesList()
        {
            FilesList.Clear();
        }

        /* Функция заполнения выпадающего списка найденными типами файлов */
        private void FillExtensionsDropdown()
        {
            ExtensionsDropdown.Items.Clear();
            ExtensionsDropdown.Items.Add("–");
            ExtensionsList.Sort();
            foreach (var item in ExtensionsList)
            {
                ExtensionsDropdown.Items.Add(item);
            }
            ExtensionsDropdown.SelectedIndex = 0;
        }

        /* Функция для проверки возможности копирования и перемещения файлов */
        /* Блокирует кнопки, если копирование/перемещение запрещено */
        private void ComparePaths(string path = "")
        {
            string curPath = currentPath;

            if (path != "") curPath = path;

            if (curPath == AnotherController.currentPath)
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
                CheckFilesSelection();

                AnotherController.MoveButton.Enabled = true;
                AnotherController.CopyButton.Enabled = true;
                AnotherController.CheckFilesSelection();
            }

        }

        /* Функция проверки количества выбранных строк в списке файлов */
        public void CheckFilesSelection()
        {
            if (FilesView.SelectedItems.Count == 0)
            {
                DeleteButton.Enabled = false;
                CopyButton.Enabled = false;
                MoveButton.Enabled = false;
            }
        }

        /* Функция перерисовки списка файлов + фильтрация по типу и датам */
        public void RenderList()
        {
            FilesView.Items.Clear();

            foreach (ListViewItemWithData item in FilesList)
            {
                if (item.Type == ListItemType.Folder || item.Type == ListItemType.Backward)
                {
                    DirectoryInfo info = (DirectoryInfo)item.GetData();

                    if ((string)ExtensionsDropdown.SelectedItem == null
                        || (string)ExtensionsDropdown.SelectedItem == "–"
                        || (string)ExtensionsDropdown.SelectedItem == "Папка"
                    )
                    {
                        if (item.Type != ListItemType.Backward && DateFilter.Checked)
                        {
                            if (RadioDateCreated.Checked && info.CreationTime >= DateFrom.Value && info.CreationTime <= DateTo.Value) { FilesView.Items.Add(item); }
                            if (RadioDateChanged.Checked && info.LastWriteTime >= DateFrom.Value && info.LastWriteTime <= DateTo.Value) { FilesView.Items.Add(item); }
                        }
                        else
                        {
                            FilesView.Items.Add(item);

                        }
                    }
                }

                if (item.Type == ListItemType.File)
                {
                    FileInfo info = (FileInfo)item.GetData();

                    if ((string)ExtensionsDropdown.SelectedItem == null || (string)ExtensionsDropdown.SelectedItem == "–" || (string)ExtensionsDropdown.SelectedItem == info.Extension)
                    {
                        if (DateFilter.Checked)
                        {
                            if (RadioDateCreated.Checked && info.CreationTime >= DateFrom.Value && info.CreationTime <= DateTo.Value) { FilesView.Items.Add(item); }
                            if (RadioDateChanged.Checked && info.LastWriteTime >= DateFrom.Value && info.LastWriteTime <= DateTo.Value) { FilesView.Items.Add(item); }
                        }
                        else
                        {
                            FilesView.Items.Add(item);

                        }
                    }

                }
            }
        }

        /* Функция для полной перерисовки обоих списков файлов (даже в режиме поиска) */
        public void FullRerender(bool isCaller = true)
        {
            if (isSearchMode)
            {
                SearchByKeyword();
            }
            else
            {
                OpenFolder(currentPath);
            }

            if (isCaller) AnotherController.FullRerender(false);
        }

        /* Функция для получения списка файлов и папок */
        /* (в режиме поиска выполняется рекурсивно) */
        private void GetFolderFiles(string path, bool isAddBackward = false)
        {
            bool dirSuccess = false;

            DirectoryInfo info = new DirectoryInfo(path);
            DirectoryInfo[] dir = new DirectoryInfo[0];

            try
            {
                dir = info.GetDirectories();
                dirSuccess = true;
            }
            catch { }

            if (dirSuccess)
            {

                if (info.Parent != null && info.Parent.Name != "")
                {
                    ListViewItemWithData listViewItem = new ListViewItemWithData();
                    listViewItem.Text = @"\..";
                    listViewItem.SubItems.Add("Папка");
                    listViewItem.SubItems.Add("");
                    listViewItem.SubItems.Add("");
                    listViewItem.SubItems.Add("");

                    listViewItem.Type = ListItemType.Backward;

                    if (isAddBackward) FilesList.Add(listViewItem);
                }

                foreach (DirectoryInfo item in dir)
                {
                    ListViewItemWithData listViewItem = new ListViewItemWithData(item);
                    listViewItem.Text = item.Name; // Имя
                    listViewItem.SubItems.Add("Папка"); // Тип
                    listViewItem.SubItems.Add(""); // Размер
                    listViewItem.SubItems.Add(item.CreationTime.ToString()); // Дата создания
                    listViewItem.SubItems.Add(item.LastWriteTime.ToString()); // Дата изменения
                    listViewItem.SubItems.Add(item.Parent.FullName); // Путь до директории

                    if (item.Attributes.HasFlag(FileAttributes.Hidden)) listViewItem.ForeColor = Color.Gray; // Скрытый
                    if (item.Attributes.HasFlag(FileAttributes.System)) listViewItem.ForeColor = Color.Blue; // Системный


                    if (!ExtensionsList.Contains("Папка"))
                        ExtensionsList.Add("Папка");

                    if (isSearchMode)
                    {
                        if (item.Name.ToLower().Contains(SearchTextbox.Text.ToLower()))
                        {
                            FilesList.Add(listViewItem);
                        }
                        GetFolderFiles(item.FullName); // момент рекурсии
                    }
                    else
                    {
                        FilesList.Add(listViewItem);
                    }
                }

                bool fileSuccess = false;
                FileInfo[] files = new FileInfo[0];


                try
                {
                    files = info.GetFiles();
                    fileSuccess = true;
                }
                catch { }

                if (fileSuccess)
                {
                    foreach (FileInfo item in files)
                    {
                        ListViewItemWithData listViewItem = new ListViewItemWithData(item);
                        listViewItem.Text = item.Name; // Имя
                        if (listViewItem.Text == "") listViewItem.Text = "(unknown_name)";
                        listViewItem.SubItems.Add(item.Extension); // Тип
                        listViewItem.SubItems.Add(Helpers.ToFileSize(item.Length)); // Размер
                        listViewItem.SubItems.Add(item.CreationTime.ToString()); // Дата создания
                        listViewItem.SubItems.Add(item.LastWriteTime.ToString()); // Дата изменения
                        listViewItem.SubItems.Add(item.DirectoryName); // Путь до директории

                        if (item.Attributes.HasFlag(FileAttributes.Hidden)) listViewItem.ForeColor = Color.Gray; // Скрытый
                        if (item.Attributes.HasFlag(FileAttributes.System)) listViewItem.ForeColor = Color.Blue; // Системный

                        if (isSearchMode)
                        {
                            if (item.Name.ToLower().Contains(SearchTextbox.Text.ToLower()))
                            {
                                if (!ExtensionsList.Contains(item.Extension))
                                    ExtensionsList.Add(item.Extension);

                                FilesList.Add(listViewItem);
                            }
                        }
                        else
                        {
                            if (!ExtensionsList.Contains(item.Extension))
                                ExtensionsList.Add(item.Extension);

                            FilesList.Add(listViewItem);
                        }
                    }
                }
            }
        }

        /* Функция для открытия папки с обновлением информации о типах */
        private void OpenFolder(string path)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(path);
                DirectoryInfo[] dir = info.GetDirectories();
                ClearStatusStrip();

                ClearExtensionsList();

                if (!info.Exists) throw new ArgumentNullException("info");

                currentPath = path;
                PathTextbox.Text = currentPath;

                ClearFilesList();

                GetFolderFiles(path, true);

                CreateButton.Enabled = true;

                FillExtensionsDropdown();

                RenderList();
                ComparePaths();
            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Недостаточно прав для выполнения операции: \n" + e.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /* Функция для активации режим поиска по ключевому слову */
        /* (может занимать много времени из-за рекурсивного обхода) */
        private void SearchByKeyword()
        {
            mainForm.Cursor = Cursors.WaitCursor;
            mainForm.Enabled = false;

            ClearFilesList();
            DirectoryInfo currentDirectory = new DirectoryInfo(currentPath);

            GetFolderFiles(currentDirectory.FullName, false);

            FillExtensionsDropdown();
            RenderList();

            mainForm.Cursor = Cursors.Default;
            mainForm.Enabled = true;
        }

        /* Обработчик смены выбранного файла в списке файлов */
        private void FilesView_SelectionChangeHandler(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            ListViewItemWithData item = (ListViewItemWithData)e.Item;

            /* Проверка на наличие асинхронной задачи на расчёт размера папки */
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

            /* Если подходящего выбранного элемента нет, вырубаем кнопки для файлов */
            if (FilesView.SelectedItems.Count < 1 || item.Type == ListItemType.None || item.Type == ListItemType.Backward)
            {
                ClearStatusStrip();
                DeleteButton.Enabled = false;
                CopyButton.Enabled = false;
                MoveButton.Enabled = false;

                return;
            }
            DeleteButton.Enabled = true;

            /* Если выбран ровно один файл/папка, отобразить информацию о нём в нижней строке */
            if (FilesView.SelectedItems.Count == 1)
            {
                /* логика для папки */
                if (item.Type == ListItemType.Folder)
                {
                    DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                    ComparePaths(directoryInfo.Parent.FullName);
                    StatusName.Text = "Имя: " + directoryInfo.Name;
                    StatusType.Text = "Тип: " + "Папка";
                    StatusSize.Text = "Размер: " + "загрузка..."; // Размер папки вычисляется долго, потому заглушка
                    StatusCreated.Text = "Создан: " + directoryInfo.CreationTime.ToString();
                    StatusChanged.Text = "Изменён: " + directoryInfo.LastWriteTime.ToString();
                    StatusAttr.Text = "Атрибуты: " + directoryInfo.Attributes.ToString();

                    // Создаём асинхронную задачу на вычисление размера папки
                    cancelTokenSource = new CancellationTokenSource();
                    currentTask = Task.Run(() =>
                    {
                        try
                        {
                            if (cancelTokenSource.Token.IsCancellationRequested)
                                cancelTokenSource.Token.ThrowIfCancellationRequested(); // генерируем исключение

                            StatusSize.Text = "Размер: " + Helpers.ToFileSize(Helpers.DirSize(directoryInfo, cancelTokenSource.Token));
                        }
                        catch { }
                    }, cancelTokenSource.Token);
                }

                /* логика для файла */
                if (item.Type == ListItemType.File)
                {
                    FileInfo fileInfo = (FileInfo)item.GetData();
                    ComparePaths(fileInfo.DirectoryName);

                    StatusName.Text = "Имя: " + fileInfo.Name;
                    StatusType.Text = "Тип: " + fileInfo.Extension;
                    StatusSize.Text = "Размер: " + Helpers.ToFileSize(fileInfo.Length);
                    StatusCreated.Text = "Создан: " + fileInfo.CreationTime.ToString();
                    StatusChanged.Text = "Изменён: " + fileInfo.LastWriteTime.ToString();
                    StatusAttr.Text = "Атрибуты: " + fileInfo.Attributes.ToString();
                }
            }
            else { ClearStatusStrip(); }
        }

        /* Обработчик двойного клика по файлу */
        private void FilesView_DoubleClickHandler(object sender, MouseEventArgs e)
        {
            string nextPath = currentPath;
            ListViewItemWithData item = (ListViewItemWithData)FilesView.SelectedItems[0];

            /* Если кликаем по "назад", перемещаемся на уровень выше */
            if (item.Text == @"\..")
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
            else // иначе переходим в выбранную папку
            {
                if (!nextPath.EndsWith(@":\")) nextPath += @"\";


                if (item.Type == ListItemType.Folder)
                {
                    DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();

                    if (isSearchMode)
                    {
                        OpenFolder(directoryInfo.Parent.FullName);
                        CancelSearchMode();
                    }
                    else
                    {
                        OpenFolder(directoryInfo.FullName);
                    }
                }

                if (item.Type == ListItemType.File)
                {
                    FileInfo fileInfo = (FileInfo)item.GetData();

                    if (isSearchMode)
                    {
                        OpenFolder(fileInfo.DirectoryName);
                        CancelSearchMode();
                    }
                    else
                    {

                    }
                }
            }
        }

        /* Обработчик окончания редактирования адресной строки сверху */
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

        /* Обработчик нажатия на Enter для ручного ввода нового адреса сверху */
        private void PathTextbox_KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                mainForm.ActiveControl = null;
                e.Handled = true;
            }
        }

        /* Обработчик редактирования текста в адресной строке, заменяет слеши на обратные */
        private void PathTextbox_TextChangedHandler(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            int selection = textbox.SelectionStart;
            textbox.Text = textbox.Text.Replace("/", @"\");
            textbox.SelectionStart = selection;
            textbox.SelectionLength = 0;
        }

        /* Обработчик редактирования текста в строке поиска */
        private void SearchTextbox_TextChangedHandler(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            if (textbox.Text.Length == 0)
            {
                SearchButton.Enabled = false;
            }
            else
            {
                SearchButton.Enabled = true;
            }
        }

        /* Обработчик двойного клика по нижней строке для разворачивания */
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

        /* Обработчик изменения выбранного типа файла (перерисовка + фильтрация листа файлов) */
        private void ExtensionDropdown_SelectedValueChanged(object sender, EventArgs e)
        {
            RenderList();
        }

        /* Обработчик нажатия на кнопку удаления */
        private void DeleteButton_ClickHandler(object sender, EventArgs e)
        {
            try
            {
                string message = "Вы уверены, что хотите безвозвратно удалить";

                if (FilesView.SelectedItems.Count > 1)
                {
                    message += " выбранные элементы";
                }
                else
                {
                    message += " выбранный элемент";
                }
                message += "?";



                var answer = MessageBox.Show(message, "Подтвердите действие", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (answer == DialogResult.Yes)
                {

                    foreach (var selectedItem in FilesView.SelectedItems)
                    {


                        ListViewItemWithData item = (ListViewItemWithData)selectedItem;

                        if (item.Type == ListItemType.Folder)
                        {
                            DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();

                            Directory.Delete(directoryInfo.FullName, true);
                            FullRerender();

                        }

                        if (item.Type == ListItemType.File)
                        {
                            FileInfo fileInfo = (FileInfo)item.GetData();

                            File.Delete(fileInfo.FullName);
                            FullRerender();
                        }

                    }
                }
            }
            catch (UnauthorizedAccessException error)
            {
                MessageBox.Show("Недостаточно прав для выполнения операции: \n" + error.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /* Обработчик нажатия на кнопку создания */
        private void CreateButton_ClickHandler(object sender, EventArgs e)
        {
            // TODO: создание файлов
        }

        /* Обработчик нажатия на кнопку перемещения */
        private void MoveButton_ClickHandler(object sender, EventArgs e)
        {
            try
            {
                string message = "Вы уверены, что хотите перместить";

                if (FilesView.SelectedItems.Count > 1)
                {
                    message += " выбранные элементы";
                }
                else
                {
                    message += " выбранный элемент";
                }
                message += "?";



                var answer = MessageBox.Show(message, "Подтвердите действие", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (answer == DialogResult.Yes)
                {

                    foreach (var selectedItem in FilesView.SelectedItems)
                    {


                        ListViewItemWithData item = (ListViewItemWithData)selectedItem;

                        if (item.Type == ListItemType.Folder)
                        {
                            DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();
                            Directory.Move(directoryInfo.FullName, AnotherController.currentPath + @"\" + directoryInfo.Name);
                            FullRerender();

                        }

                        if (item.Type == ListItemType.File)
                        {
                            FileInfo fileInfo = (FileInfo)item.GetData();

                            File.Move(fileInfo.FullName, AnotherController.currentPath + @"\" + fileInfo.Name);
                            FullRerender();
                        }

                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /* Обработчик нажатия на кнопку копирования */
        private void CopyButton_ClickHandler(object sender, EventArgs e)
        {
            try
            {
                string message = "Вы уверены, что хотите скопировать";

                if (FilesView.SelectedItems.Count > 1)
                {
                    message += " выбранные элементы";
                }
                else
                {
                    message += " выбранный элемент";
                }
                message += "?";



                var answer = MessageBox.Show(message, "Подтвердите действие", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (answer == DialogResult.Yes)
                {

                    foreach (var selectedItem in FilesView.SelectedItems)
                    {


                        ListViewItemWithData item = (ListViewItemWithData)selectedItem;

                        if (item.Type == ListItemType.Folder)
                        {
                            DirectoryInfo directoryInfo = (DirectoryInfo)item.GetData();

                            // Создать идентичную структуру папок
                            foreach (string dirPath in Directory.GetDirectories(directoryInfo.FullName, "*", SearchOption.AllDirectories))
                            {

                                Directory.CreateDirectory(dirPath.Replace(directoryInfo.FullName, AnotherController.currentPath));

                            }

                            // Копировать все файлы и перезаписать файлы с идентичным именем
                            foreach (string newPath in Directory.GetFiles(directoryInfo.FullName, "*.*", SearchOption.AllDirectories))
                            {
                                File.Copy(newPath, newPath.Replace(directoryInfo.FullName, AnotherController.currentPath), true);
                            }

                            FullRerender();

                        }

                        if (item.Type == ListItemType.File)
                        {
                            FileInfo fileInfo = (FileInfo)item.GetData();

                            File.Copy(fileInfo.FullName, AnotherController.currentPath + @"\" + fileInfo.Name);
                            FullRerender();
                        }

                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Отказано в доступе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /* Обработчик нажатия на кнопку обновления */
        private void RefreshButton_ClickHandler(object sender, EventArgs e)
        {
            if (!isSearchMode)
            {
                OpenFolder(currentPath);
            }
        }

        /* Функция для выключения режима поиска */
        private void CancelSearchMode()
        {
            isSearchMode = false;
            SearchTextbox.Enabled = true;
            PathTextbox.Enabled = true;
            RefreshButton.Enabled = true;

            SearchTextbox.BackColor = SystemColors.Window;
            SearchTextbox.Text = "";
            SearchButton.Text = "Поиск";
            OpenFolder(currentPath);
        }

        /* Обработчик нажатия на кнопку поиска */
        private void SearchButton_ClickHandler(object sender, EventArgs e)
        {
            if (!isSearchMode)
            {
                isSearchMode = true;
                SearchTextbox.Enabled = false;
                PathTextbox.Enabled = false;
                CreateButton.Enabled = false;
                RefreshButton.Enabled = false;

                SearchTextbox.BackColor = Color.PaleGoldenrod;
                SearchButton.Text = "Отмена";
                SearchByKeyword();
            }
            else
            {
                CancelSearchMode();
            }
        }

        /* Обработчик включения/выключения фильтрации по датам */
        private void DateFilter_CheckHandler(object sender, EventArgs e)
        {
            RenderList();
        }

        /* Обработчик изменения значения начальной даты (левая) */
        private void DateFrom_ChangedHandler(object sender, EventArgs e)
        {
            try
            {
                if (DateFrom.Value > DateTo.Value)
                {
                    DateTo.Value = DateFrom.Value;
                }

                if (DateFilter.Checked) RenderList();
            }
            catch { }
        }

        /* Обработчик изменения значения конечной даты (правая) */
        private void DateTo_ChangedHandler(object sender, EventArgs e)
        {
            try
            {
                if (DateTo.Value < DateFrom.Value)
                {
                    DateFrom.Value = DateTo.Value;
                }

                if (DateFilter.Checked) RenderList();
            }
            catch { }
        }

        /* Обработчик выбора режима фильтрации по дате (дата создания / дата изменения) */
        private void RadioDate_ChangedHandler(object sender, EventArgs e)
        {
            if (DateFilter.Checked) RenderList();
        }
    }
}

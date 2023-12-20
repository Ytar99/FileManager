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
    public partial class Form1 : Form
    {
        private FileManagerController controllerLeft;
        private FileManagerController controllerRight;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /* Создаём контроллей левой панели */
            controllerLeft = new FileManagerController(
                this,
                path1_TextBox,
                comboBox1_ext,
                dateTimePicker1_from,
                dateTimePicker1_to,
                checkBox1_dateFilterCheck,
                radioButton1_dateCreated,
                radioButton1_dateChanged,
                search1_TextBox,
                button1_search,
                button1_delete,
                button1_moveToRight,
                button1_copyToRight,
                button1_create,
                button1_refresh,
                listView1,
                statusStrip1
                );

            /* Создаём контроллей правой панели */
            controllerRight = new FileManagerController(
                this,
                path2_TextBox,
                comboBox2_ext,
                dateTimePicker2_from,
                dateTimePicker2_to,
                checkBox2_dateFilterCheck,
                radioButton2_dateCreated,
                radioButton2_dateChanged,
                search2_TextBox,
                button2_search,
                button2_delete,
                button2_moveToLeft,
                button2_copyToLeft,
                button2_create,
                button2_refresh,
                listView2,
                statusStrip2
                );

            /* Связываем контроллеры между собой */
            controllerLeft.AnotherController = controllerRight;
            controllerRight.AnotherController = controllerLeft;
        }
    }
}

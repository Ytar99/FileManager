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
            controllerLeft = new FileManagerController(
                this,
                path1_TextBox, 
                comboBox1_ext, 
                dateTimePicker1_from, 
                dateTimePicker1_to, 
                search1_TextBox, 
                button1_delete, 
                button1_moveToRight, 
                button1_copyToRight, 
                listView1, 
                statusStrip1
                );

            controllerRight = new FileManagerController(
                this,
                path2_TextBox,
                comboBox2_ext,
                dateTimePicker2_from,
                dateTimePicker2_to,
                search2_TextBox,
                button2_delete,
                button2_moveToLeft,
                button2_copyToLeft,
                listView2,
                statusStrip2
                );

            controllerLeft.AnotherController = controllerRight;
            controllerRight.AnotherController = controllerLeft;
        }
    }
}

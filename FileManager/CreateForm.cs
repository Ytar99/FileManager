using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public partial class CreateForm : Form
    {
        private string path;

        public CreateForm(string path)
        {
            InitializeComponent();

            this.path = path;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == string.Empty)
            {
                button_create.Enabled = false;
            }
            else
            {
                button_create.Enabled = true;
            }
        }

        private void button_create_Click(object sender, EventArgs e)
        {
            try
            {
                if (radioButton_file.Checked)
                {
                    File.Create(path + @"/" + textBox1.Text);
                }

                if (radioButton_folder.Checked)
                {
                    Directory.CreateDirectory(path + @"\" + textBox1.Text);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}

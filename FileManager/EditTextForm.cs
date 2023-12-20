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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileManager
{
    public partial class EditTextForm : Form
    {
        private FileInfo file;
        private int fontSize;
        private FontFamily fontFamily;
        private FontFamily[] fontsList;
        private bool isDirty;
        private bool isSubmitted;
        private bool initialLoading;

        public EditTextForm(FileInfo file)
        {
            InitializeComponent();

            this.initialLoading = true;
            this.isDirty = false;
            this.isSubmitted = false;
            this.file = file;
            this.fontFamily = textBox1.Font.FontFamily;
            this.fontSize = trackBar1.Value;
            fontsList = FontFamily.Families;
        }

        private void EditTextForm_Load(object sender, EventArgs e)
        {
            textBox1.Lines = File.ReadAllLines(file.FullName);
            label_fontSize.Text = "Размер шрифта:\n\n" + fontSize.ToString() + " пт.";


            foreach (FontFamily f in fontsList)
            {
                comboBox1.Items.Add(f.Name);
            }

            comboBox1.SelectedItem = fontFamily.Name;
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = 0;
            isDirty = false;
            isSubmitted = false;
            initialLoading = false;
        }

        private void EditTextForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (isDirty && !isSubmitted)
            {
                var answer = MessageBox.Show(
                    "У вас есть несохранённые изменения, которые будут утеряны при закрытии.\n\nЗакрыть окно редактирования без сохранения?",
                    "Подтвердите действие",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                e.Cancel = (answer == DialogResult.No);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            fontSize = trackBar1.Value;
            textBox1.Font = new Font(fontFamily, fontSize);
            label_fontSize.Text = "Размер шрифта:\n\n" + fontSize.ToString() + " пт.";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.WordWrap = checkBox1.Checked;

            if (checkBox1.Checked)
            {
                textBox1.ScrollBars = ScrollBars.Vertical;
            }
            else
            {
                textBox1.ScrollBars = ScrollBars.Both;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            fontFamily = new FontFamily((string)comboBox1.SelectedItem);
            textBox1.Font = new Font(fontFamily, fontSize);
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            try
            {
                isSubmitted = true;
                File.WriteAllLines(file.FullName, textBox1.Lines);
                DialogResult = DialogResult.OK;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Ошибка щаписи", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isSubmitted = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!initialLoading)
            {
                isDirty = true;
                button_save.Enabled = true;
            }
        }
    }
}

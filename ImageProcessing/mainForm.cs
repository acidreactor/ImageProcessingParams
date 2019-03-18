using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Test
{
    public partial class mainForm : Form
    {
        //Запрещает применение фильтра, когда никакое изображение не загружено
        bool imageOpened = false;
        //Математическое ожидание (используется в логарифмической коррекции)

        Image image;
        ProcessingImage processingImage;
        ToolTip ToolTipGamma = new System.Windows.Forms.ToolTip();
        ToolTip ToolTipLinearMin = new System.Windows.Forms.ToolTip();
        ToolTip ToolTipLinearMax = new System.Windows.Forms.ToolTip();
        public mainForm()
        {
            InitializeComponent();
            processingImage = new ProcessingImage(ChartSecond, toolStripProgressBar1, statusLabel, pictureBox1);
            image = new Image(processingImage, null);

            ToolTipGamma.SetToolTip(this.textBoxGamma, "Вещественное значение");
            ToolTipLinearMin.SetToolTip(this.textBoxLinearMin, "Минимальный порог (целое значение)");
            ToolTipLinearMax.SetToolTip(this.textBoxLinearMax, "Максимальный порог (целое значение)");
            ToolTipGamma.InitialDelay = ToolTipLinearMin.InitialDelay = ToolTipLinearMax.InitialDelay = 10;
        }       

        //Обновление картинки
        private void refreshImage()
        {
            image.drawOriginal(pictureBox1);
            ChartSecond.Series["Яркость"].Points.Clear();
            statusLabel.Text = "Изображение обновлено";
        }

        private void openImage()
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if (res != DialogResult.Cancel)
            {
                //Преобразование изображения в полутон
                Bitmap BitmapImage = new Bitmap(openFileDialog1.FileName);
                image = new Image(processingImage, BitmapImage);
                image.process(PROCESSING_METHODS.TOGRAY);
                image.draw(pictureBox1, ChartFirst);
            }
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openImage();
        }

        private void обновитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            image.drawOriginal(pictureBox1);
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            openImage();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            image.drawOriginal(pictureBox1);
        } 

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "image.jpg";
            if (imageOpened)
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
                    pictureBox1.Image.Save(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            else
                MessageBox.Show("Изображение не загружено", "Загрузите изображение", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void сохранитьГистограммудоВФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "histBefore.jpg";
            if (imageOpened)
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
                    ChartFirst.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            else
                MessageBox.Show("Изображение не загружено", "Загрузите изображение", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void сохранитьГистограммупослеВФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "histAfter.jpg";
            if (imageOpened)
            {
                if (ChartSecond.Series["Яркость"].Points.Count > 0)
                {
                    if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
                        ChartSecond.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                else
                    MessageBox.Show("Гистограмма \"после\" не построена, примените один из фильтров к изображению", "Гистограмма пуста", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else 
                MessageBox.Show("Изображение не загружено", "Загрузите изображение", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        private void buttonExecute_Click(object sender, EventArgs e)
        {
            if (checkBoxUpdate.Checked)
                refreshImage();

            if (radioNormal.Checked)
                image.process(PROCESSING_METHODS.NORMAL);
            else if (radioEQ.Checked)
                image.process(PROCESSING_METHODS.EQ);
            else if (radioGamma.Checked)
            {
                if (!float.TryParse(textBoxGamma.Text, out processingImage.gamma))
                {
                    processingImage.gamma = 0.5f;
                    textBoxGamma.Text = "0.5";
                }
                image.process(PROCESSING_METHODS.GAMMA);
            }
            else if (radioLog.Checked)
                image.process(PROCESSING_METHODS.LOG);
            else if (radioContr.Checked)
            {
                if (!int.TryParse(textBoxLinearMin.Text, out processingImage.linearMin))
                {
                    processingImage.linearMin = 10;
                    textBoxLinearMin.Text = "10";
                }
                if (!int.TryParse(textBoxLinearMax.Text, out processingImage.linearMax))
                {
                    processingImage.linearMax = 150;
                    textBoxLinearMax.Text = "150";
                }
                image.process(PROCESSING_METHODS.LINEAR);
            }
            else if (radioNegative.Checked)
                image.process(PROCESSING_METHODS.NEGATIVE);
        }
        

    }
}

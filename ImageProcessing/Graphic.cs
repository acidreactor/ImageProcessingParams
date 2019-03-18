using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    public enum PROCESSING_METHODS
    {
        TOGRAY,
        NORMAL,
        EQ,
        GAMMA,
        LOG,
        LINEAR,
        NEGATIVE
    }

    public abstract class Graphic
    {
        public abstract void draw(PictureBox control, Chart chartUpdate);
    }

    interface Processable
    {
        void process(PROCESSING_METHODS method);
    }

    public class Image : Graphic, Processable
    {
        private Bitmap image;
        private Bitmap originalImage;
        private ProcessingImage processing;

        public Image(ProcessingImage pi, Bitmap i)
        {
            processing = pi;
            setImage(i);
        }

        public void setImage(Bitmap i)
        {
            image = i;
            if (i != null)
                originalImage = new Bitmap(image);
        }

        public void drawOriginal(PictureBox control)
        {
            if (originalImage != null)
            {
                control.Image = originalImage;
                processing.setStatus("Изображение обновлено");
            }
        }

        public override void draw(PictureBox control, Chart chartUpdate = null)
        {
            control.Image = image;
            processing.setImageOpened(true);
            if (chartUpdate != null)
                processing.chartUpdate(chartUpdate);
        }

        public void process(PROCESSING_METHODS method)
        {
            switch (method)
            {
                case PROCESSING_METHODS.EQ:
                    processing.equalization();
                    break;
                case PROCESSING_METHODS.GAMMA:
                    processing.gammaCorrection();
                    break;
                case PROCESSING_METHODS.LINEAR:
                    processing.linearContrasting();
                    break;
                case PROCESSING_METHODS.LOG:
                    processing.logarithmicCorrection();
                    break;
                case PROCESSING_METHODS.NEGATIVE:
                    processing.negative();
                    break;
                case PROCESSING_METHODS.NORMAL:
                    processing.normalization();
                    break;
                case PROCESSING_METHODS.TOGRAY:
                    image = processing.gray(image);
                    originalImage = new Bitmap(image);
                    break;
            }
        }
    }

    public class ProcessingImage
    {
        ToolStripProgressBar toolStripProgressBar;
        ToolStripStatusLabel statusLabel;
        PictureBox pictureBox;
        Chart chartSecond;
        bool imageOpened;
        float Mean = 0;
        public float gamma;
        public int linearMin, linearMax;

        public ProcessingImage(Chart chartSec, ToolStripProgressBar tspb, ToolStripStatusLabel tssl, PictureBox pb, bool io = false)
        {
            toolStripProgressBar = tspb;
            pictureBox = pb;
            imageOpened = io;
            chartSecond = chartSec;
            statusLabel = tssl;
        }

        public void setStatus(String status)
        {
            statusLabel.Text = status;
        }

        public void setImageOpened(bool io)
        {
            imageOpened = io;
            if (imageOpened)
                statusLabel.Text = "Изображение загружено";
        }

        //  Считывание яркости пикселя
        private int getPixelBrightness(Color Input)
        {
            return (int)(0.3 * Input.R + 0.59 * Input.G + 0.11 * Input.B);
        }

        //  Установка яркости пикселя
        public Color setPixelBrightness(Color InputColor, int OldBrightness, int NewBrightness)
        {
            //Вычисляем значение, на которое нужно "сместить" яркость 
            int Value = (OldBrightness - NewBrightness);
            //Вычисляем R G B составляющие относительно новой яркости
            int Red = InputColor.R + Value, Green = InputColor.G + Value, Blue = InputColor.B + Value;

            //Проверка, на выход за границы диапазона (тернарный опреатор ветвления)
            Red = Red > 255 ? 255 : Red;
            Green = Green > 255 ? 255 : Green;
            Blue = Blue > 255 ? 255 : Blue;

            Red = Red < 0 ? 0 : Red;
            Green = Green < 0 ? 0 : Green;
            Blue = Blue < 0 ? 0 : Blue;

            return Color.FromArgb(Red, Green, Blue);
        }

        public void setProgressOptions(int max)
        {
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Maximum = max;
        }

        public void gammaCorrection()
        {
            if (imageOpened)
            {
                //Константы, фигурирующие в формуле! 
                float C = 1, f0 = 0, Gamma = gamma; //0.5f;
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width);

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        //Вычисляем старое значение яркости (x) и новое по формуле С * (x +f0)^Gamma.
                        int OldBrightness = getPixelBrightness(BitmapImage.GetPixel(i, j)),
                            NewBrightness = (int)(C * Math.Pow((OldBrightness + f0), Gamma));
                        //Устанавливаем новое значение яркости
                        BitmapImage.SetPixel(i, j, setPixelBrightness(BitmapImage.GetPixel(i, j),
                                                                        OldBrightness,
                                                                        NewBrightness));
                    }
                    toolStripProgressBar.PerformStep();
                }
                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Гамма-коррекция";
            }
        }

        //  Получаем негативный пиксель
        public Color getNegativePixel(Color InputColor)
        {
            int Red = 255 - InputColor.R, Green = 255 - InputColor.G, Blue = 255 - InputColor.B;
            return Color.FromArgb(Red, Green, Blue);
        }

        //  Поиск максимального значения яркости
        private int findMaxBrightnessValue()
        {
            Bitmap BitmapImage = new Bitmap(pictureBox.Image);
            int Max = getPixelBrightness(BitmapImage.GetPixel(0, 0));
            for (int i = 0; i < BitmapImage.Width; i++)
            {
                for (int j = 0; j < BitmapImage.Height; j++)
                {
                    if (getPixelBrightness(BitmapImage.GetPixel(i, j)) > Max)
                        Max = getPixelBrightness(BitmapImage.GetPixel(i, j));
                }
            }
            return Max;
        }

        //Поиск минимального значения яркости
        private int findMinBrightnessValue()
        {
            Bitmap BitmapImage = new Bitmap(pictureBox.Image);
            int Min = getPixelBrightness(BitmapImage.GetPixel(0, 0));
            for (int i = 0; i < BitmapImage.Width; i++)
            {
                for (int j = 0; j < BitmapImage.Height; j++)
                {
                    if (getPixelBrightness(BitmapImage.GetPixel(i, j)) < Min)
                        Min = getPixelBrightness(BitmapImage.GetPixel(i, j));
                }
            }
            return Min;
        }

        //Логарифмическая коррекция
        public void logarithmicCorrection()
        {
            if (imageOpened)
            {
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width);
                //Вычисление максимального и минимального параметра яркости
                int FMax = findMaxBrightnessValue(), FMin = findMinBrightnessValue();

                //Алгоритм представлен в документе (СМ. документ!)
                float PositiveRange = (2 > FMax - Mean ? 2 : FMax - Mean),
                    NegativeRange = (2 > Mean - FMin ? 2 : Mean - FMin),
                    PositiveAlpha = (float)(255 / Math.Log(PositiveRange)),
                    NegativeAlpha = (float)(255 / Math.Log(NegativeRange)),
                    Bufer;

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        //Вычисление старой и новой яркостей
                        float NewBrightness, OldBrightness = getPixelBrightness(BitmapImage.GetPixel(i, j));
                        //Попиксельно применяем шаги алгоритма
                        Bufer = OldBrightness - Mean;
                        if (Bufer >= 1)
                            NewBrightness = Mean + (float)Math.Round(PositiveAlpha * Math.Log(Bufer));
                        else if (Bufer <= -1)
                            NewBrightness = Mean - (float)Math.Round(NegativeAlpha * Math.Log(-Bufer));
                        else
                            NewBrightness = Mean;

                        //Обновляем пиксель
                        BitmapImage.SetPixel(i, j, setPixelBrightness(BitmapImage.GetPixel(i, j),
                                                                        (int)OldBrightness,
                                                                        (int)NewBrightness));
                    }
                    toolStripProgressBar.PerformStep();
                }

                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Логарифмическая коррекция";
            }
        }

        //Негатив
        public void negative()
        {
            if (imageOpened)
            {
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width);

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                        BitmapImage.SetPixel(i, j, getNegativePixel(BitmapImage.GetPixel(i, j)));
                    toolStripProgressBar.PerformStep();
                }

                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Негатив";
            }
        }

        //Линейное контрастирование
        public void linearContrasting()
        {
            if (imageOpened)
            {
                //Желаемые значения
                int YMin = linearMin, YMax = linearMax;
                //Вычисление максимального и минимального параметра яркости
                int FMax = findMaxBrightnessValue(), FMin = findMinBrightnessValue();
                //Вычисление констант согласно алгоритму (СМ. документ!) 
                int a = (YMax - YMin) / (FMax - FMin),
                    b = (YMin * FMax - FMin * YMax) / (FMax - FMin);
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width);

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        int OldBrightness = (int)getPixelBrightness(BitmapImage.GetPixel(i, j)),
                            NewBrightness = a * OldBrightness + b;
                        BitmapImage.SetPixel(i, j, setPixelBrightness(BitmapImage.GetPixel(i, j),
                                                                       (int)OldBrightness,
                                                                       (int)NewBrightness));
                    }
                    toolStripProgressBar.PerformStep();
                }

                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Линейное контрастирование";
            }
        }

        //Нормализация
        public void normalization()
        {
            if (imageOpened)
            {
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width);

                //Вычисление максимального и минимального параметра яркости
                int FMax = findMaxBrightnessValue(), FMin = findMinBrightnessValue();

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        float OldBrightness = getPixelBrightness(BitmapImage.GetPixel(i, j)),
                            NewBrightness = (OldBrightness - FMin) / (FMax - FMin);
                        BitmapImage.SetPixel(i, j, setPixelBrightness(BitmapImage.GetPixel(i, j),
                                                                        (int)OldBrightness,
                                                                        (int)NewBrightness));
                    }
                    toolStripProgressBar.PerformStep();
                }
                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Нормализация";
            }
        }

        //Эквализация
        public void equalization()
        {
            if (imageOpened)
            {
                Bitmap BitmapImage = new Bitmap(pictureBox.Image);
                setProgressOptions(BitmapImage.Width * 2);

                //Создаем специальный массив, хранящий информацию о яркости и количестве пикселей данной яркости
                //Информация об этом массиве на сайте (в алгоритме)
                int[] Hist = new int[256];
                for (int i = 0; i < 256; i++)
                    Hist[i] = 0;
                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        Hist[(int)getPixelBrightness(BitmapImage.GetPixel(i, j))]++;
                    }
                    toolStripProgressBar.PerformStep();
                }
                //В каждую ячейку мы добавляем значения всех предыдущих
                for (int i = 1; i < 256; i++)
                    Hist[i] += Hist[i - 1];

                for (int i = 0; i < BitmapImage.Width; i++)
                {
                    for (int j = 0; j < BitmapImage.Height; j++)
                    {
                        int OldBrightness = (int)getPixelBrightness(BitmapImage.GetPixel(i, j)),
                            Equalize = (int)(256 * Hist[OldBrightness] / (BitmapImage.Width * BitmapImage.Height));
                        //Выбираем минимальное
                        float NewBrightness = Equalize < 255 ? Equalize : 255;

                        BitmapImage.SetPixel(i, j, setPixelBrightness(BitmapImage.GetPixel(i, j),
                                                   (int)OldBrightness,
                                                   (int)NewBrightness));
                    }
                    toolStripProgressBar.PerformStep();
                }
                pictureBox.Image = BitmapImage;
                chartUpdate(chartSecond);
                statusLabel.Text = "Эквализация";
            }
        }

        //Обновление гистограммы
        public void chartUpdate(System.Windows.Forms.DataVisualization.Charting.Chart chart)
        {
            //Очищаем гистограмму
            chart.Series["Яркость"].Points.Clear();
            //Массив, индексом - значение яркости, а значением - количесвтво пикселей, имеющие данню яркость
            float[] BrightnessArray = new float[256];
            Bitmap BitmapImage = new Bitmap(pictureBox.Image);

            //Заполнение массива
            for (int i = 0; i < BitmapImage.Width; i++)
            {
                for (int j = 0; j < BitmapImage.Height; j++)
                {
                    BrightnessArray[getPixelBrightness(BitmapImage.GetPixel(i, j))]++;
                }
            }

            //Очистка предыдущих подсчетов математического ожидания
            Mean = 0;
            //Обновление гистограммы
            for (int i = 1; i < 255; i++)
            {
                //Добавляем столбец в гистограмму
                chart.Series["Яркость"].Points.Add(BrightnessArray[i]);
                //Здесь будет удобно параллельно считать математичекое ожидание summ(p(i) * x(i)).
                //Вероятность (p) в данном случае считается как отношение количества пикселей текущей яркости
                //к общему числу пикселей
                Mean += i * BrightnessArray[i] / (BitmapImage.Width * BitmapImage.Height);
            }
        }

        public Bitmap gray(Bitmap BitmapImage)
        {
            setProgressOptions(BitmapImage.Width + 1);

            for (int i = 0; i < BitmapImage.Width; i++)
            {
                for (int j = 0; j < BitmapImage.Height; j++)
                {
                    //Считываем яркость пикселя и образуем новый
                    int PixelBrightness = getPixelBrightness(BitmapImage.GetPixel(i, j));
                    BitmapImage.SetPixel(i, j, Color.FromArgb(PixelBrightness, PixelBrightness, PixelBrightness));
                }
                toolStripProgressBar.PerformStep();
            }
            return BitmapImage;
        }

    }


}

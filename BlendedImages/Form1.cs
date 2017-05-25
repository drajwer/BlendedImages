using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace BlendedImages
{
    public partial class Form1 : Form
    {
        private readonly BackgroundWorker backgroundWorker1;
        private readonly BackgroundWorker backgroundWorker2;
        private readonly Image noImage;
        private int counter;
        private readonly int libraryImageWidth = 150;
        private readonly int libraryImageHeight = 150;
        private PictureBox selectedPictureBox;
        private ImageLibrary library;
        public Form1()
        {
            InitializeComponent();
            MaximumSize = new Size(int.MaxValue, 350);
            MinimumSize = new Size(400, 350);
            Text = "BlendedImages";
            noImage = pictureBox1.Image;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Click += pictureBox_Click;
            pictureBox1.Tag = false;
            pictureBox2.Tag = false;
            pictureBox2.Click += pictureBox_Click;
            KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            backgroundWorker1 = CreateBgrWorker();
            backgroundWorker2 = CreateBgrWorker();
            flowLayoutPanel1.DragEnter += FlowLayoutPanelOnDragEnter;

            // Library class
            LoadLibrary();
            LoadImagesFromLibrary();
        }

        private BackgroundWorker CreateBgrWorker()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += BackgroundWorker_DoWork;
            worker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            worker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            worker.WorkerReportsProgress = true;
            return worker;
        }
        private void LoadImagesFromLibrary()
        {
            foreach (var path in library.ImagePaths)
            {
                if (File.Exists(path))
                {
                    PictureBox p = CreateLibraryImage(path, true);
                    if (p != null)
                        flowLayoutPanel1.Controls.Add(p);
                }
            }
        }

        private void LoadLibrary()
        {
            string libraryPath = "imgLibrary.xml";
            XmlDocument xmlDocument = new XmlDocument();
            if (File.Exists(libraryPath))
            {
                TextReader reader;
                try
                {
                    reader = new StreamReader(libraryPath);
                    xmlDocument.Load(reader);
                    library = new ImageLibrary(xmlDocument);
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                reader.Close();
            }
            else
            {
                library = new ImageLibrary();
            }
        }

        private void FlowLayoutPanelOnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FlowLayoutPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null)
            {
                Console.WriteLine("FlowLayoutPanel_DragDrop: panel == null!");
                return;
            }
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                AddImageToLibrary(file, false);
            }
            panel.Update();
        }

        public void AddImageToLibrary(string file, bool flag)
        {
            PictureBox p = CreateLibraryImage(file, flag);
            if (p != null)
                flowLayoutPanel1.Controls.Add(p);
        }

        public PictureBox CreateLibraryImage(string file, bool loadedFromLibrary)
        {
            //Library
            if (!library.Add(file) && !loadedFromLibrary)
                return null;

            int thickness = 5;
            PictureBox p = new PictureBox
            {
                Image = Image.FromFile(file),
                ClientSize = new Size(libraryImageWidth + 2 * thickness, libraryImageHeight + 2 * thickness),
                BackColor = Color.White,
                Padding = new Padding(thickness),
                ImageLocation = file,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            p.MouseClick += P_MouseClick;
            return p;
        }

        private void P_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox == null)
            {
                Console.WriteLine("P_MouseClick: pictureBox == null");
                return;
            }
            if (selectedPictureBox != null)
            {
                selectedPictureBox.BackColor = Color.White;
            }
            selectedPictureBox = selectedPictureBox == pictureBox ? null : pictureBox;
            if (selectedPictureBox != null)
            {
                selectedPictureBox.BackColor = Color.Orange;
            }
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            try
            {
                ProgressBar bar = worker == backgroundWorker1 ? progressBar1 : progressBar2;
                bar.Value = progressChangedEventArgs.ProgressPercentage;
            }
            catch (NullReferenceException e)
            {
                MessageBox.Show(this, e.Message, "Error");
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker == null)
                return;
            BackgroundWorker secondWorker = worker == backgroundWorker1 ? backgroundWorker2 : backgroundWorker1;
            ProgressBar bar = worker == backgroundWorker1 ? progressBar1 : progressBar2;
            bar.Value = bar.Minimum;
            SetProgressControls(bar, label2, secondWorker, false);
            Form form = e.Result as Form;
            if (form == null)
                return;
            form.Show();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BgrWorkerArgs args = e.Argument as BgrWorkerArgs;
            e.Result = null;
            if (args != null)
                e.Result = PerformBlending(args);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F12:
                    TakeScreenshot();
                    break;
                case Keys.Delete:
                    RemoveFromLibrary(selectedPictureBox);
                    break;
            }

        }

        private void RemoveFromLibrary(PictureBox pictureBox)
        {
            if (pictureBox == null)
                return;
            library.Delete(pictureBox.ImageLocation);
            flowLayoutPanel1.Controls.Remove(pictureBox);
        }

        private void TakeScreenshot()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics gr = Graphics.FromImage(bitmap))
            {

                gr.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            if (!(bool)pictureBox1.Tag)
            {
                pictureBox1.Image = bitmap;
                pictureBox1.Tag = true;
            }
            else
            {
                pictureBox2.Image = bitmap;
                pictureBox2.Tag = true;
            }
            UpdateButtonVisibilty();
        }

        private class BgrWorkerArgs
        {
            public double alfa;
            public BackgroundWorker worker;
            public int number;
        }


        private void SetProgressControls(ProgressBar bar, Label l, BackgroundWorker secondWorker, bool enabled)
        {
            bar.Visible = enabled;
            if (enabled)
                l.Visible = enabled;
            else if (!secondWorker.IsBusy)
                l.Visible = enabled;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            BgrWorkerArgs args = new BgrWorkerArgs()
            {
                alfa = (double)trackBar1.Value / (double)trackBar1.Maximum,
                number = counter++
            };
            if (!backgroundWorker1.IsBusy)
            {
                StartWork(backgroundWorker1, backgroundWorker2, args);
            }
            else if (!backgroundWorker2.IsBusy)
            {
                StartWork(backgroundWorker2, backgroundWorker1, args);
            }
        }

        private void StartWork(BackgroundWorker worker, BackgroundWorker secondWorker, BgrWorkerArgs args)
        {
            args.worker = worker;
            ProgressBar bar = worker == backgroundWorker1 ? progressBar1 : progressBar2;
            SetProgressControls(bar, label2, secondWorker, true);
            worker.RunWorkerAsync(args);
        }

        private Form PerformBlending(BgrWorkerArgs args)
        {
            Bitmap bitmap1 = new Bitmap(pictureBox1.Image);
            Bitmap bitmap2 = new Bitmap(pictureBox2.Image);
            int width = bitmap1.Width > bitmap2.Width ? bitmap2.Width : bitmap1.Width;
            int height = bitmap1.Height > bitmap2.Height ? bitmap2.Height : bitmap1.Height;
            int lastReportedPercentage = 0;
            double alfa = args.alfa;
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    ProceedPixel(bitmap1, bitmap2, alfa, resultBitmap, x, y);
                    int percentage = (int)((double)(x * height + y) * 100 / (double)(width * height));
                    if (lastReportedPercentage + 5 < percentage)
                    {
                        args.worker.ReportProgress(percentage);
                        lastReportedPercentage = percentage;
                    }
                }
            }
            Form form = new ImageWindow(resultBitmap, args.number, this);
            return form;
        }

        private static void ProceedPixel(Bitmap bitmap1, Bitmap bitmap2, double alfa, Bitmap resultBitmap, int x, int y)
        {
            Color color1 = bitmap1.GetPixel(x, y);
            Color color2 = bitmap2.GetPixel(x, y);
            int R = (int)(alfa * color1.R + (1 - alfa) * color2.R);
            int G = (int)(alfa * color1.G + (1 - alfa) * color2.G);
            int B = (int)(alfa * color1.B + (1 - alfa) * color2.B);

            Color resColor = Color.FromArgb(R, G, B);
            resultBitmap.SetPixel(x, y, resColor);
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            PictureBox p = sender as PictureBox;
            if (p == null)
                return;
            if (selectedPictureBox != null)
            {
                p.Image = selectedPictureBox.Image;
                p.Tag = true;
            }
            else
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "image files (*.BMP;*.JPG;*.GIF)|*.bmp;*.jpg;*.gif|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (!Path.GetExtension(dlg.FileName).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) &&
                        !Path.GetExtension(dlg.FileName).Equals(".bmp", StringComparison.InvariantCultureIgnoreCase) &&
                        !Path.GetExtension(dlg.FileName).Equals(".gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MessageBox.Show("Invalid file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        p.Tag = false;
                        p.Image = noImage;
                        return;
                    }
                    // Create a new Bitmap object from the picture file on disk,
                    // and assign that to the PictureBox.Image property
                    p.Image = new Bitmap(dlg.FileName);
                    p.Tag = true;
                }
            }
            UpdateButtonVisibilty();

        }

        private void UpdateButtonVisibilty()
        {
            if ((bool)pictureBox1.Tag && (bool)pictureBox2.Tag)
                button1.Enabled = true;
        }
    }
}

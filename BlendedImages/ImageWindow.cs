using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BlendedImages
{
    public partial class ImageWindow : Form
    {
        private readonly int imageNr;
        private Form1 parentForm;
        private readonly string addedPath = "newImage0.bmp";
        private string lastSaved = "";
        public ImageWindow()
        {
            InitializeComponent();
        }
        public ImageWindow(Bitmap bitmap, int number, Form1 form)
        {
            InitializeComponent();
            parentForm = form;
            imageNr = number;
            ClientSize = new Size(bitmap.Width, bitmap.Height);
            BackgroundImage = bitmap;
            BackgroundImageLayout = ImageLayout.Stretch;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "image files (*.BMP;*.JPG;*.GIF)|*.bmp;*.jpg;*.gif|All files (*.*)|*.*";
            dialog.RestoreDirectory = true;
            dialog.FileName = $"image{imageNr}";
            ImageFormat format = ImageFormat.Bmp;
            if(dialog.ShowDialog() != DialogResult.OK)
                return;
            string ext = System.IO.Path.GetExtension(dialog.FileName);
            switch (ext)
            {
                case ".jpg":
                    format = ImageFormat.Jpeg;
                    break;
                case ".bmp":
                    format = ImageFormat.Bmp;
                    break;
                case ".gif":
                    format = ImageFormat.Gif;
                    break;
                default:
                    MessageBox.Show(this, "Invalid filename!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }
            BackgroundImage.Save(dialog.FileName, format);
            lastSaved = dialog.FileName;
        }

        private void addToLibraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(addedPath))
            {
                if(lastSaved == "")
                    MessageBox.Show(this, "Please save file on your disk first.");
                else
                {
                    parentForm.AddImageToLibrary(lastSaved, false);
                }
            }
            else
            {
                BackgroundImage.Save(addedPath);
                parentForm.AddImageToLibrary(addedPath, false);
            }
                
            
        }
    }
}

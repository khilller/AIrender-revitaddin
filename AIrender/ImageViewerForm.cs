using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RevitAIRenderer
{
    public partial class ImageViewerForm : Form
    {
        private string _imagePath;
        private Image _loadedImage;

        public ImageViewerForm(string imagePath, string prompt)
        {
            InitializeComponent();
            _imagePath = imagePath;

            try
            {
                _loadedImage = Image.FromFile(imagePath);

                // Set form size based on image dimensions, but with reasonable limits
                int width = Math.Min(_loadedImage.Width + 40, 1200);
                int height = Math.Min(_loadedImage.Height + 140, 900);
                this.Size = new Size(width, height);

                // Set title with the prompt used
                this.Text = $"AI Rendered View - {prompt.Substring(0, Math.Min(prompt.Length, 50))}";

                // Set the label to show image details
                lblImageInfo.Text = $"Image Size: {_loadedImage.Width} x {_loadedImage.Height} pixels | File: {Path.GetFileName(imagePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _loadedImage = null;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Basic form settings
            this.Text = "AI Rendered View";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(640, 480);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Icon = SystemIcons.Application;

            // Create the picture box for displaying the image
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _loadedImage,
                BackColor = Color.LightGray
            };

            // Create an information label
            lblImageInfo = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(5, 0, 0, 0),
                Text = "Image Information"
            };

            // Create a panel for the buttons
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.WhiteSmoke
            };

            // Save button
            btnSave = new Button
            {
                Text = "Save Image",
                Width = 120,
                Height = 30,
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(240, 240, 240),
                UseVisualStyleBackColor = true
            };
            btnSave.Click += btnSave_Click;

            // Compare button (toggle between original and rendered)
            btnCompare = new Button
            {
                Text = "Show Original",
                Width = 120,
                Height = 30,
                Location = new Point(140, 10),
                BackColor = Color.FromArgb(240, 240, 240),
                UseVisualStyleBackColor = true
            };
            btnCompare.Click += btnCompare_Click;

            // Set as current view button
            btnSetAsCurrent = new Button
            {
                Text = "Insert Into View",
                Width = 120,
                Height = 30,
                Location = new Point(270, 10),
                BackColor = Color.FromArgb(240, 240, 240),
                UseVisualStyleBackColor = true
            };
            btnSetAsCurrent.Click += btnSetAsCurrent_Click;

            // Close button
            btnClose = new Button
            {
                Text = "Close",
                Width = 120,
                Height = 30,
                Location = new Point(this.ClientSize.Width - 130, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(240, 240, 240),
                UseVisualStyleBackColor = true
            };
            btnClose.Click += btnClose_Click;

            // Add buttons to panel
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCompare);
            buttonPanel.Controls.Add(btnSetAsCurrent);
            buttonPanel.Controls.Add(btnClose);

            // Setup trackbar for zoom control
            trackBarZoom = new TrackBar
            {
                Minimum = 10,
                Maximum = 200,
                Value = 100,
                TickFrequency = 10,
                LargeChange = 10,
                SmallChange = 5,
                Width = 150,
                Location = new Point(400, 10),
                Anchor = AnchorStyles.Top
            };
            trackBarZoom.ValueChanged += trackBarZoom_ValueChanged;

            // Add label for zoom percentage
            lblZoom = new Label
            {
                Text = "Zoom: 100%",
                Width = 80,
                Location = new Point(560, 15),
                Anchor = AnchorStyles.Top
            };

            buttonPanel.Controls.Add(trackBarZoom);
            buttonPanel.Controls.Add(lblZoom);

            // Add controls to form
            this.Controls.Add(pictureBox);
            this.Controls.Add(lblImageInfo);
            this.Controls.Add(buttonPanel);

            this.ResumeLayout(false);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Show a save file dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    Title = "Save Rendered Image",
                    FileName = $"RevitRender_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                // Determine default filter based on file extension
                string extension = Path.GetExtension(_imagePath).ToLower();
                if (extension == ".jpg" || extension == ".jpeg")
                    saveDialog.FilterIndex = 2;
                else
                    saveDialog.FilterIndex = 1;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Copy the file to the selected location
                    File.Copy(_imagePath, saveDialog.FileName, true);
                    MessageBox.Show($"Image saved successfully to:\n{saveDialog.FileName}",
                        "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool _showingOriginal = false;
        private string _originalImagePath = null;

        private void btnCompare_Click(object sender, EventArgs e)
        {
            // Need to store original image path for comparison
            if (_originalImagePath == null)
            {
                // Try to get the original image path by parsing the rendered image path
                // Assuming rendered image path is something like "Results/result_20250329_123456.jpg" and
                // original is in "Temp/view_12345_20250329_123456.png"
                string resultsDir = Path.GetDirectoryName(_imagePath);
                string baseDir = Directory.GetParent(resultsDir)?.FullName;
                string tempDir = Path.Combine(baseDir, "Temp");

                if (Directory.Exists(tempDir))
                {
                    // Try to find the original image based on timestamp
                    string renderedFilename = Path.GetFileName(_imagePath);
                    string timestampPart = renderedFilename.Contains("_")
                        ? renderedFilename.Substring(renderedFilename.IndexOf("_"))
                        : "";

                    var files = Directory.GetFiles(tempDir, $"*{timestampPart}*");
                    if (files.Length > 0)
                    {
                        _originalImagePath = files[0]; // Take the first matching file
                    }
                }

                // If we still don't have the original, show an error
                if (_originalImagePath == null || !File.Exists(_originalImagePath))
                {
                    MessageBox.Show("Original image not found. The comparison feature requires the original screenshot to be available.",
                        "Original Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                if (_showingOriginal)
                {
                    // Switch back to rendered image
                    pictureBox.Image = Image.FromFile(_imagePath);
                    btnCompare.Text = "Show Original";
                    _showingOriginal = false;
                }
                else
                {
                    // Switch to original image
                    pictureBox.Image = Image.FromFile(_originalImagePath);
                    btnCompare.Text = "Show Rendered";
                    _showingOriginal = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching images: {ex.Message}", "Image Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSetAsCurrent_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This feature will import the rendered image as a drafting view or as an image in the current view. " +
                            "Implementation requires access to the Revit API context, which will be added in the next update.",
                            "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void trackBarZoom_ValueChanged(object sender, EventArgs e)
        {
            int zoomValue = trackBarZoom.Value;
            lblZoom.Text = $"Zoom: {zoomValue}%";

            if (zoomValue == 100)
            {
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else
            {
                pictureBox.SizeMode = PictureBoxSizeMode.Normal;
                if (_loadedImage != null)
                {
                    float scale = zoomValue / 100.0f;
                    int newWidth = (int)(_loadedImage.Width * scale);
                    int newHeight = (int)(_loadedImage.Height * scale);

                    if (pictureBox.Image != _loadedImage)
                    {
                        // If we're showing the original image for comparison
                        float originalScale = zoomValue / 100.0f;
                        newWidth = (int)(pictureBox.Image.Width * originalScale);
                        newHeight = (int)(pictureBox.Image.Height * originalScale);
                    }

                    // Create a new bitmap with the resized dimensions
                    Bitmap bmp = new Bitmap(newWidth, newHeight);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(pictureBox.Image, new Rectangle(0, 0, newWidth, newHeight));
                    }

                    // Store the current image to dispose it later
                    Image oldImage = pictureBox.Image;

                    // Set the new image
                    pictureBox.Image = bmp;

                    // Dispose the old image if it's not the original loaded image
                    if (oldImage != _loadedImage && oldImage != null)
                    {
                        oldImage.Dispose();
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Clean up resources
            if (pictureBox.Image != null && pictureBox.Image != _loadedImage)
            {
                pictureBox.Image.Dispose();
            }

            if (_loadedImage != null)
            {
                _loadedImage.Dispose();
            }
        }

        private PictureBox pictureBox;
        private Button btnSave;
        private Button btnCompare;
        private Button btnSetAsCurrent;
        private Button btnClose;
        private Label lblImageInfo;
        private TrackBar trackBarZoom;
        private Label lblZoom;
    }
}
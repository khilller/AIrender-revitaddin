using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RevitAIRenderer
{
    public class ReferenceImageViewer : Form
    {
        private List<string> _imagePaths;
        private int _currentIndex = 0;
        private PictureBox _pictureBox;
        private Label _counterLabel;
        private Button _btnPrevious;
        private Button _btnNext;
        private Label _infoLabel;

        public ReferenceImageViewer(List<string> imagePaths, int startIndex = 0)
        {
            _imagePaths = imagePaths;
            _currentIndex = Math.Min(Math.Max(0, startIndex), imagePaths.Count - 1);
            InitializeComponent();
            LoadCurrentImage();
            UpdateNavigationButtons();
        }

        private void InitializeComponent()
        {
            this.Text = "Reference Image Viewer";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Main PictureBox
            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray
            };

            // Navigation panel
            Panel navPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // Previous button
            _btnPrevious = new Button
            {
                Text = "< Previous",
                Size = new Size(100, 30),
                Location = new Point(20, 10),
                UseVisualStyleBackColor = true
            };
            _btnPrevious.Click += BtnPrevious_Click;

            // Next button
            _btnNext = new Button
            {
                Text = "Next >",
                Size = new Size(100, 30),
                Location = new Point(navPanel.Width - 120, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            _btnNext.Click += BtnNext_Click;

            // Counter label
            _counterLabel = new Label
            {
                Size = new Size(100, 20),
                Location = new Point(navPanel.Width / 2 - 50, 15),
                Anchor = AnchorStyles.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Image 1 of 1"
            };

            // Image info label
            _infoLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 0, 0, 0)
            };

            // Add controls to navigation panel
            navPanel.Controls.Add(_btnPrevious);
            navPanel.Controls.Add(_counterLabel);
            navPanel.Controls.Add(_btnNext);

            // Add controls to form
            this.Controls.Add(_pictureBox);
            this.Controls.Add(_infoLabel);
            this.Controls.Add(navPanel);

            // Close on Escape key
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape)
                    this.Close();
            };
        }

        private void LoadCurrentImage()
        {
            if (_imagePaths != null && _imagePaths.Count > 0 && _currentIndex >= 0 && _currentIndex < _imagePaths.Count)
            {
                string imagePath = _imagePaths[_currentIndex];

                try
                {
                    // Dispose of previous image if exists
                    if (_pictureBox.Image != null)
                    {
                        _pictureBox.Image.Dispose();
                        _pictureBox.Image = null;
                    }

                    // Load new image
                    _pictureBox.Image = Image.FromFile(imagePath);

                    // Update info label
                    FileInfo fileInfo = new FileInfo(imagePath);

                    Image img = _pictureBox.Image;
                    string dimensions = $"{img.Width} × {img.Height} pixels";
                    string fileSize = $"{fileInfo.Length / 1024:N0} KB";

                    _infoLabel.Text = $"Filename: {Path.GetFileName(imagePath)} | {dimensions} | {fileSize}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Image Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateNavigationButtons()
        {
            _btnPrevious.Enabled = _currentIndex > 0;
            _btnNext.Enabled = _currentIndex < _imagePaths.Count - 1;
            _counterLabel.Text = $"Image {_currentIndex + 1} of {_imagePaths.Count}";
        }

        private void BtnPrevious_Click(object sender, EventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                LoadCurrentImage();
                UpdateNavigationButtons();
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_currentIndex < _imagePaths.Count - 1)
            {
                _currentIndex++;
                LoadCurrentImage();
                UpdateNavigationButtons();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up resources
            if (_pictureBox.Image != null)
            {
                _pictureBox.Image.Dispose();
                _pictureBox.Image = null;
            }

            base.OnFormClosing(e);
        }
    }
}
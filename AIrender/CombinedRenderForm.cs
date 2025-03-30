using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rectangle = System.Drawing.Rectangle;

namespace RevitAIRenderer
{
    public class CombinedRenderForm : System.Windows.Forms.Form, IDisposable
    {
        private AiRendererSettings _settings;
        private string _originalImagePath;
        private string _renderedImagePath;
        private Image _originalImage;
        private Image _renderedImage;
        private bool _isRendering = false;
        private Document _document;
        private Autodesk.Revit.DB.View _view;
        private bool _highQualityRender = true;  // Added flag for high-quality rendering

        // Form controls
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.TextBox txtPrompt;
        private System.Windows.Forms.TextBox txtNegativePrompt;
        private System.Windows.Forms.NumericUpDown numControlStrength;
        private System.Windows.Forms.ComboBox cboOutputFormat;
        private System.Windows.Forms.ComboBox cboStylePreset;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.PictureBox pictureBoxOriginal;
        private System.Windows.Forms.PictureBox pictureBoxRendered;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.CheckBox chkHighQuality;  // New checkbox for high quality

        // Constructor
        public CombinedRenderForm(Document doc, Autodesk.Revit.DB.View view, AiRendererSettings settings)
        {
            _document = doc;
            _view = view;
            _settings = settings;
            InitializeComponent();

            // Load the settings into UI controls
            LoadSettings();

            // Capture the current view as soon as the form loads
            CaptureCurrentView();
        }

        private void InitializeComponent()
        {
            this.Text = "Revit AI Renderer";
            this.Size = new System.Drawing.Size(1200, 800);  // Increased form size
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(900, 600);

            // Create a split container to divide the form
            splitContainer = new System.Windows.Forms.SplitContainer();
            splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer.Orientation = System.Windows.Forms.Orientation.Vertical;

            // First add the control to the form, before setting min sizes
            this.Controls.Add(splitContainer);

            // Set the splitter properties after adding to form
            splitContainer.Panel1MinSize = 250;
            splitContainer.Panel2MinSize = 600;  // Larger right panel for images

            // Set the splitter distance in the Load event to avoid initialization errors
            this.Load += (sender, e) => {
                try
                {
                    // Recalculate splitter position based on actual form size
                    int splitterPos = Math.Min(300, this.ClientSize.Width - 600);
                    if (splitterPos > 0)
                    {
                        splitContainer.SplitterDistance = splitterPos;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting splitter position: {ex.Message}");
                }
            };

            // === Left Panel (Settings) ===
            System.Windows.Forms.Panel settingsPanel = new System.Windows.Forms.Panel();
            settingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            settingsPanel.AutoScroll = true;
            settingsPanel.Padding = new System.Windows.Forms.Padding(10);
            splitContainer.Panel1.Controls.Add(settingsPanel);

            // API Settings section
            System.Windows.Forms.Label lblApiSection = new System.Windows.Forms.Label();
            lblApiSection.Text = "API Configuration";
            lblApiSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblApiSection.Size = new System.Drawing.Size(200, 20);
            lblApiSection.Location = new System.Drawing.Point(10, 10);

            System.Windows.Forms.Label lblApiKey = new System.Windows.Forms.Label();
            lblApiKey.Text = "Stability AI API Key:";
            lblApiKey.Size = new System.Drawing.Size(120, 20);
            lblApiKey.Location = new System.Drawing.Point(10, 40);

            txtApiKey = new System.Windows.Forms.TextBox();
            txtApiKey.Size = new System.Drawing.Size(250, 20);
            txtApiKey.Location = new System.Drawing.Point(10, 60);
            txtApiKey.PasswordChar = '*';

            // Rendering settings section
            System.Windows.Forms.Label lblRenderSection = new System.Windows.Forms.Label();
            lblRenderSection.Text = "Rendering Configuration";
            lblRenderSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblRenderSection.Size = new System.Drawing.Size(200, 20);
            lblRenderSection.Location = new System.Drawing.Point(10, 100);

            System.Windows.Forms.Label lblPrompt = new System.Windows.Forms.Label();
            lblPrompt.Text = "Prompt:";
            lblPrompt.Size = new System.Drawing.Size(100, 20);
            lblPrompt.Location = new System.Drawing.Point(10, 130);

            txtPrompt = new System.Windows.Forms.TextBox();
            txtPrompt.Size = new System.Drawing.Size(250, 80);
            txtPrompt.Location = new System.Drawing.Point(10, 150);
            txtPrompt.Multiline = true;

            System.Windows.Forms.Label lblNegativePrompt = new System.Windows.Forms.Label();
            lblNegativePrompt.Text = "Negative Prompt:";
            lblNegativePrompt.Size = new System.Drawing.Size(100, 20);
            lblNegativePrompt.Location = new System.Drawing.Point(10, 240);

            txtNegativePrompt = new System.Windows.Forms.TextBox();
            txtNegativePrompt.Size = new System.Drawing.Size(250, 60);
            txtNegativePrompt.Location = new System.Drawing.Point(10, 260);
            txtNegativePrompt.Multiline = true;

            // Parameters
            System.Windows.Forms.Label lblControlStrength = new System.Windows.Forms.Label();
            lblControlStrength.Text = "Control Strength:";
            lblControlStrength.Size = new System.Drawing.Size(100, 20);
            lblControlStrength.Location = new System.Drawing.Point(10, 330);

            numControlStrength = new System.Windows.Forms.NumericUpDown();
            numControlStrength.Size = new System.Drawing.Size(80, 20);
            numControlStrength.Location = new System.Drawing.Point(130, 330);
            numControlStrength.DecimalPlaces = 2;
            numControlStrength.Increment = 0.1m;
            numControlStrength.Minimum = 0.1m;
            numControlStrength.Maximum = 1.0m;
            numControlStrength.Value = 0.7m;

            System.Windows.Forms.Label lblOutputFormat = new System.Windows.Forms.Label();
            lblOutputFormat.Text = "Output Format:";
            lblOutputFormat.Size = new System.Drawing.Size(100, 20);
            lblOutputFormat.Location = new System.Drawing.Point(10, 360);

            cboOutputFormat = new System.Windows.Forms.ComboBox();
            cboOutputFormat.Size = new System.Drawing.Size(80, 20);
            cboOutputFormat.Location = new System.Drawing.Point(130, 360);
            cboOutputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboOutputFormat.Items.AddRange(new object[] { "jpeg", "png", "webp" });
            cboOutputFormat.SelectedIndex = 0;

            System.Windows.Forms.Label lblStylePreset = new System.Windows.Forms.Label();
            lblStylePreset.Text = "Style Preset:";
            lblStylePreset.Size = new System.Drawing.Size(100, 20);
            lblStylePreset.Location = new System.Drawing.Point(10, 390);

            cboStylePreset = new System.Windows.Forms.ComboBox();
            cboStylePreset.Size = new System.Drawing.Size(170, 20);
            cboStylePreset.Location = new System.Drawing.Point(130, 390);
            cboStylePreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboStylePreset.Items.AddRange(new object[] {
                "",
                "3d-model",
                "analog-film",
                "anime",
                "cinematic",
                "comic-book",
                "digital-art",
                "enhance",
                "fantasy-art",
                "isometric",
                "line-art",
                "low-poly",
                "modeling-compound",
                "neon-punk",
                "origami",
                "photographic",
                "pixel-art",
                "tile-texture"
            });
            cboStylePreset.SelectedIndex = 0;

            // Add high-quality option
            chkHighQuality = new System.Windows.Forms.CheckBox();
            chkHighQuality.Text = "High-Quality Image Capture";
            chkHighQuality.Size = new System.Drawing.Size(200, 20);
            chkHighQuality.Location = new System.Drawing.Point(10, 420);
            chkHighQuality.Checked = _highQualityRender;
            chkHighQuality.CheckedChanged += (s, e) => {
                _highQualityRender = chkHighQuality.Checked;
                CaptureCurrentView(); // Recapture with new quality settings
            };

            // Action buttons
            btnRender = new System.Windows.Forms.Button();
            btnRender.Text = "Render View";
            btnRender.Size = new System.Drawing.Size(120, 30);
            btnRender.Location = new System.Drawing.Point(10, 450);
            btnRender.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            btnRender.ForeColor = System.Drawing.Color.White;
            btnRender.Click += btnRender_Click;

            btnSaveSettings = new System.Windows.Forms.Button();
            btnSaveSettings.Text = "Save Settings";
            btnSaveSettings.Size = new System.Drawing.Size(120, 30);
            btnSaveSettings.Location = new System.Drawing.Point(140, 450);
            btnSaveSettings.Click += btnSaveSettings_Click;

            // Add a Sync View button
            Button btnSyncView = new Button();
            btnSyncView.Text = "Sync View";
            btnSyncView.Size = new System.Drawing.Size(120, 30);
            btnSyncView.Location = new System.Drawing.Point(10, 490); // Position below Render button
            btnSyncView.BackColor = System.Drawing.Color.FromArgb(0, 160, 160); // Distinct color
            btnSyncView.ForeColor = System.Drawing.Color.White;
            btnSyncView.Click += btnSyncView_Click;

            // Add a Save Image button
            Button btnSaveImage = new Button();
            btnSaveImage.Text = "Save Image";
            btnSaveImage.Size = new System.Drawing.Size(120, 30);
            btnSaveImage.Location = new System.Drawing.Point(140, 490);
            btnSaveImage.BackColor = System.Drawing.Color.FromArgb(220, 220, 220);
            btnSaveImage.ForeColor = System.Drawing.Color.Black;
            btnSaveImage.Click += btnSaveImage_Click;

            // Add all controls to the settings panel
            settingsPanel.Controls.Add(lblApiSection);
            settingsPanel.Controls.Add(lblApiKey);
            settingsPanel.Controls.Add(txtApiKey);
            settingsPanel.Controls.Add(lblRenderSection);
            settingsPanel.Controls.Add(lblPrompt);
            settingsPanel.Controls.Add(txtPrompt);
            settingsPanel.Controls.Add(lblNegativePrompt);
            settingsPanel.Controls.Add(txtNegativePrompt);
            settingsPanel.Controls.Add(lblControlStrength);
            settingsPanel.Controls.Add(numControlStrength);
            settingsPanel.Controls.Add(lblOutputFormat);
            settingsPanel.Controls.Add(cboOutputFormat);
            settingsPanel.Controls.Add(lblStylePreset);
            settingsPanel.Controls.Add(cboStylePreset);
            settingsPanel.Controls.Add(chkHighQuality);
            settingsPanel.Controls.Add(btnRender);
            settingsPanel.Controls.Add(btnSaveSettings);
            settingsPanel.Controls.Add(btnSyncView);
            settingsPanel.Controls.Add(btnSaveImage);

            // === Right Panel (Image Viewer) ===
            System.Windows.Forms.Panel imageViewerPanel = new System.Windows.Forms.Panel();
            imageViewerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            imageViewerPanel.AutoScroll = true;
            splitContainer.Panel2.Controls.Add(imageViewerPanel);

            // Image control tabs
            tabControl = new System.Windows.Forms.TabControl();
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            imageViewerPanel.Controls.Add(tabControl);

            // Original image tab
            System.Windows.Forms.TabPage tabOriginal = new System.Windows.Forms.TabPage("Original View");
            pictureBoxOriginal = new System.Windows.Forms.PictureBox();
            pictureBoxOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBoxOriginal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBoxOriginal.BackColor = System.Drawing.Color.LightGray;
            tabOriginal.Controls.Add(pictureBoxOriginal);

            // Rendered image tab
            System.Windows.Forms.TabPage tabRendered = new System.Windows.Forms.TabPage("AI Rendered");
            pictureBoxRendered = new System.Windows.Forms.PictureBox();
            pictureBoxRendered.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBoxRendered.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBoxRendered.BackColor = System.Drawing.Color.LightGray;
            tabRendered.Controls.Add(pictureBoxRendered);

            tabControl.TabPages.Add(tabOriginal);
            tabControl.TabPages.Add(tabRendered);

            // Status bar for information
            statusStrip = new System.Windows.Forms.StatusStrip();
            statusStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            lblStatus.Text = "Ready";
            lblStatus.Spring = true;
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(lblStatus);
            this.Controls.Add(statusStrip);

            // Progress bar for rendering
            progressBar = new System.Windows.Forms.ProgressBar();
            progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Height = 20;
            progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            progressBar.Visible = false;
            this.Controls.Add(progressBar);
        }

        private void LoadSettings()
        {
            txtApiKey.Text = _settings.ApiKey;
            txtPrompt.Text = _settings.DefaultPrompt;

            // Set control strength from settings or default
            numControlStrength.Value = Convert.ToDecimal(_settings.ControlStrength > 0 ?
                _settings.ControlStrength : 0.7f);

            // Select output format if it exists in the combobox
            string format = _settings.OutputFormat ?? "webp";
            for (int i = 0; i < cboOutputFormat.Items.Count; i++)
            {
                if (cboOutputFormat.Items[i].ToString() == format)
                {
                    cboOutputFormat.SelectedIndex = i;
                    break;
                }
            }

            // Set the style preset if available
            if (!string.IsNullOrEmpty(_settings.StylePreset))
            {
                for (int i = 0; i < cboStylePreset.Items.Count; i++)
                {
                    if (cboStylePreset.Items[i].ToString() == _settings.StylePreset)
                    {
                        cboStylePreset.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            // Check if we have a rendered image
            if (string.IsNullOrEmpty(_renderedImagePath) || !File.Exists(_renderedImagePath))
            {
                MessageBox.Show("No rendered image available to save. Please render a view first.",
                    "No Image Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                string extension = Path.GetExtension(_renderedImagePath).ToLower();
                if (extension == ".jpg" || extension == ".jpeg")
                    saveDialog.FilterIndex = 2;
                else
                    saveDialog.FilterIndex = 1;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Copy the file to the selected location
                    File.Copy(_renderedImagePath, saveDialog.FileName, true);
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

        private void btnSyncView_Click(object sender, EventArgs e)
        {
            try
            {
                // Show progress indicator
                lblStatus.Text = "Synchronizing with Revit view...";
                progressBar.Visible = true;
                this.Cursor = Cursors.WaitCursor;

                // Recapture the view with current Revit settings
                CaptureCurrentView();

                // Switch to the original view tab
                tabControl.SelectedIndex = 0;

                // Success message
                lblStatus.Text = "View synchronized successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error synchronizing view: {ex.Message}", "Sync Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "View synchronization failed";
            }
            finally
            {
                // Reset UI state
                progressBar.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Update settings object
                _settings.ApiKey = txtApiKey.Text;
                _settings.DefaultPrompt = txtPrompt.Text;
                _settings.OutputFormat = cboOutputFormat.SelectedItem.ToString();
                _settings.ControlStrength = (float)numControlStrength.Value;

                // Save style preset (using empty string if "None" is selected)
                string stylePreset = cboStylePreset.SelectedItem.ToString();
                _settings.StylePreset = stylePreset;

                // Save settings
                _settings.Save();

                // Show confirmation
                lblStatus.Text = "Settings saved successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Settings Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRender_Click(object sender, EventArgs e)
        {
            if (_isRendering)
            {
                MessageBox.Show("Rendering is already in progress.", "Please Wait",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            // Check if API key is provided
            if (string.IsNullOrEmpty(txtApiKey.Text))
            {
                MessageBox.Show("Please enter your Stability AI API key before rendering.",
                    "API Key Required", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            _isRendering = true;
            btnRender.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "Rendering in progress...";

            try
            {
                // Create the API client with verbose logging enabled
                var client = new StabilityApiClient(txtApiKey.Text, true); // Enable verbose logging for debugging

                // Get the style preset (empty string if nothing selected)
                string stylePreset = cboStylePreset.SelectedItem.ToString();

                // Wait for the rendering task
                string resultPath = await client.ProcessRevitViewAsync(
                    _originalImagePath,
                    txtPrompt.Text,
                    (float)numControlStrength.Value,
                    txtNegativePrompt.Text,
                    cboOutputFormat.SelectedItem.ToString(),
                    stylePreset);

                // Update the UI with rendered image
                LoadRenderedImage(resultPath);

                // Switch to the rendered tab
                tabControl.SelectedIndex = 1;

                lblStatus.Text = "Rendering completed successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during rendering: {ex.Message}", "Rendering Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                lblStatus.Text = "Rendering failed";
            }
            finally
            {
                _isRendering = false;
                btnRender.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private void CaptureCurrentView()
        {
            lblStatus.Text = "Capturing current view...";

            try
            {
                // Create temporary directory
                string tempDir = GetSafeTempDirectory("Temp");

                // Generate file path
                _originalImagePath = Path.Combine(tempDir,
                    $"view_{_view.Id.IntegerValue}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Calculate maximum dimensions for API (square root of max pixels)
                // Max pixels is 9,437,184 according to error message
                int maxDimension = (int)Math.Sqrt(9437184); // ~3072

                // Calculate target dimensions based on quality setting but respect max size
                int scaleFactor = _highQualityRender ? 2 : 1;
                int baseWidth = 1600;
                int baseHeight = 900;

                // First, ensure aspect ratio is within allowed limits for Stability AI (1:2.5 to 2.5:1)
                double aspectRatio = (double)baseWidth / baseHeight;
                if (aspectRatio > 2.5)
                {
                    baseWidth = (int)(baseHeight * 2.5);
                    lblStatus.Text = "Adjusted aspect ratio (was too wide)";
                }
                else if (aspectRatio < 0.4) // 1/2.5 = 0.4
                {
                    baseHeight = (int)(baseWidth / 0.4);
                    lblStatus.Text = "Adjusted aspect ratio (was too tall)";
                }

                // Apply quality factor but ensure we don't exceed max dimensions
                int targetWidth = Math.Min(baseWidth * scaleFactor, 3072);
                int targetHeight = Math.Min(baseHeight * scaleFactor, 3072);

                // Ensure total pixels stay under limit
                long totalPixels = (long)targetWidth * targetHeight;
                if (totalPixels > 9437184)
                {
                    // Scale down proportionally to fit within pixel limit
                    double scalingFactor = Math.Sqrt(9437184.0 / totalPixels);
                    targetWidth = (int)(targetWidth * scalingFactor);
                    targetHeight = (int)(targetHeight * scalingFactor);
                    lblStatus.Text = $"Scaled dimensions to fit API limits: {targetWidth}x{targetHeight}";
                }

                // Check if we need to use the exact viewport
                bool useExactViewport = AppStartup.CurrentViewCorners != null &&
                                       AppStartup.CurrentViewId == _view.Id;

                if (useExactViewport)
                {
                    lblStatus.Text = "Capturing exact viewport as seen...";

                    // Get the UIView for the current view
                    UIDocument uidoc = new UIDocument(_document);
                    IList<UIView> uiviews = uidoc.GetOpenUIViews();
                    UIView activeUIView = null;

                    foreach (UIView uv in uiviews)
                    {
                        if (uv.ViewId == _view.Id)
                        {
                            activeUIView = uv;
                            break;
                        }
                    }

                    if (activeUIView != null)
                    {
                        // Export with the exact viewport zoom
                        ImageExportOptions imgOptions = new ImageExportOptions
                        {
                            ExportRange = ExportRange.VisibleRegionOfCurrentView,
                            ZoomType = ZoomFitType.Zoom,
                            ViewName = _view.Name,
                            ImageResolution = _highQualityRender ? ImageResolution.DPI_300 : ImageResolution.DPI_150,
                            HLRandWFViewsFileType = ImageFileType.PNG,
                            FilePath = _originalImagePath
                        };

                        // Set pixel size
                        imgOptions.PixelSize = targetWidth;

                        // Export the image
                        _document.ExportImage(imgOptions);
                        lblStatus.Text = "Captured exact viewport successfully";
                    }
                    else
                    {
                        // Fall back to the standard export if we can't get the UIView
                        FallbackExportImage(targetWidth, targetHeight);
                    }
                }
                else
                {
                    // Use the standard export if we don't have view corner information
                    FallbackExportImage(targetWidth, targetHeight);
                }

                // Load the image into the picture box
                LoadOriginalImage(_originalImagePath);

                // Additional check to resize if image is still too large
                CheckAndResizeIfTooLarge();

                // Now check if the exported image has valid aspect ratio for API
                CheckAndAdjustAspectRatio();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to capture view: {ex.Message}", "Capture Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                lblStatus.Text = "Failed to capture view";
            }
        }

        // Add this new method to check and resize large images
        private void CheckAndResizeIfTooLarge()
        {
            if (_originalImage != null)
            {
                long totalPixels = (long)_originalImage.Width * _originalImage.Height;

                // If over the 9,437,184 pixel limit, resize
                if (totalPixels > 9437184)
                {
                    lblStatus.Text = $"Image too large ({totalPixels} pixels). Resizing...";

                    // Calculate scaling factor
                    double scalingFactor = Math.Sqrt(9437184.0 / totalPixels);
                    int newWidth = (int)(_originalImage.Width * scalingFactor);
                    int newHeight = (int)(_originalImage.Height * scalingFactor);

                    // Create resized image
                    System.Drawing.Bitmap resizedImage = new System.Drawing.Bitmap(newWidth, newHeight);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(resizedImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(_originalImage, 0, 0, newWidth, newHeight);
                    }

                    // Dispose of original
                    _originalImage.Dispose();

                    // Save the resized image
                    resizedImage.Save(_originalImagePath);

                    // Update references
                    _originalImage = resizedImage;
                    pictureBoxOriginal.Image = _originalImage;

                    lblStatus.Text = $"Resized image to {newWidth}x{newHeight} ({newWidth * newHeight} pixels)";
                }
            }
        }

        private void LoadOriginalImage(string imagePath)
        {
            if (File.Exists(imagePath))
            {
                // Dispose of previous image if it exists
                if (_originalImage != null)
                {
                    _originalImage.Dispose();
                    _originalImage = null;
                }

                try
                {
                    // Load the new image
                    _originalImage = Image.FromFile(imagePath);
                    pictureBoxOriginal.Image = _originalImage;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Image Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadRenderedImage(string imagePath)
        {
            if (File.Exists(imagePath))
            {
                _renderedImagePath = imagePath;

                // Dispose of previous image if it exists
                if (_renderedImage != null)
                {
                    _renderedImage.Dispose();
                    _renderedImage = null;
                }

                try
                {
                    // Load the new image
                    _renderedImage = Image.FromFile(imagePath);
                    pictureBoxRendered.Image = _renderedImage;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading rendered image: {ex.Message}", "Image Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void FallbackExportImage(int targetWidth, int targetHeight)
        {
            lblStatus.Text = "Using standard view export (no exact viewport data)...";

            // Export view with appropriate settings
            ImageExportOptions imgOptions = new ImageExportOptions
            {
                ExportRange = ExportRange.CurrentView,
                ZoomType = ZoomFitType.FitToPage, // Use FitToPage for standard export
                ViewName = _view.Name,
                ImageResolution = _highQualityRender ? ImageResolution.DPI_300 : ImageResolution.DPI_150,
                HLRandWFViewsFileType = ImageFileType.PNG,
                FilePath = _originalImagePath
            };

            // Set pixel size only
            imgOptions.PixelSize = targetWidth;

            // Export the image
            _document.ExportImage(imgOptions);
            lblStatus.Text = "View captured with standard export";
        }

        private void CheckAndAdjustAspectRatio()
        {
            // This contains your existing aspect ratio adjustment code
            if (_originalImage != null)
            {
                double exportedAspectRatio = (double)_originalImage.Width / _originalImage.Height;
                lblStatus.Text = $"View captured: {_originalImage.Width} x {_originalImage.Height} pixels, aspect ratio: {exportedAspectRatio:F2}";

                // If the exported image has an invalid aspect ratio, crop it
                if (exportedAspectRatio > 2.5 || exportedAspectRatio < 0.4)
                {
                    // Create a new bitmap with corrected aspect ratio
                    int newWidth = _originalImage.Width;
                    int newHeight = _originalImage.Height;

                    if (exportedAspectRatio > 2.5)
                    {
                        // Too wide - crop width
                        newWidth = (int)(_originalImage.Height * 2.5);
                    }
                    else
                    {
                        // Too tall - crop height
                        newHeight = (int)(_originalImage.Width / 0.4);
                    }

                    // Create a new bitmap with the corrected dimensions
                    System.Drawing.Bitmap croppedImage = new System.Drawing.Bitmap(newWidth, newHeight);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(croppedImage))
                    {
                        // Center the crop
                        int x = (_originalImage.Width - newWidth) / 2;
                        int y = (_originalImage.Height - newHeight) / 2;

                        // Draw the cropped portion
                        g.DrawImage(_originalImage,
                                    new System.Drawing.Rectangle(0, 0, newWidth, newHeight),
                                    new System.Drawing.Rectangle(x, y, newWidth, newHeight),
                                    System.Drawing.GraphicsUnit.Pixel);
                    }

                    // Dispose of the original image
                    _originalImage.Dispose();

                    // Save the cropped image
                    croppedImage.Save(_originalImagePath);

                    // Load the cropped image
                    _originalImage = croppedImage;
                    pictureBoxOriginal.Image = _originalImage;

                    lblStatus.Text = $"Corrected image: {_originalImage.Width} x {_originalImage.Height} pixels, aspect ratio: {(double)_originalImage.Width / _originalImage.Height:F2}";
                }
            }
        }

        private string GetSafeTempDirectory(string subFolder)
        {
            try
            {
                string tempDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RevitAIRenderer", subFolder);

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                // Test write access
                string testFile = Path.Combine(tempDir, "test.txt");
                File.WriteAllText(testFile, "Test");
                File.Delete(testFile);

                return tempDir;
            }
            catch
            {
                // Fall back to system temp directory
                string tempDir = Path.Combine(Path.GetTempPath(), "RevitAIRenderer", subFolder);

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                return tempDir;
            }
        }

        // Implement IDisposable
        new public void Dispose()
        {
            // Clean up resources
            if (_originalImage != null)
            {
                _originalImage.Dispose();
                _originalImage = null;
            }

            if (_renderedImage != null)
            {
                _renderedImage.Dispose();
                _renderedImage = null;
            }

            // Call the base implementation
            base.Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Clean up resources
            if (_originalImage != null)
            {
                _originalImage.Dispose();
                _originalImage = null;
            }

            if (_renderedImage != null)
            {
                _renderedImage.Dispose();
                _renderedImage = null;
            }
        }
    }
}
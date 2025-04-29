using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rectangle = System.Drawing.Rectangle;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

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
        private bool _highQualityRender = true;
        private List<string> _referenceImagePaths = new List<string>();

        // Form controls
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel settingsPanel;
        private System.Windows.Forms.ComboBox cboProvider;

        // Stability AI controls
        private System.Windows.Forms.Panel pnlStabilitySettings;
        private System.Windows.Forms.TextBox txtStabilityApiKey;
        private System.Windows.Forms.TextBox txtPrompt;
        private System.Windows.Forms.TextBox txtNegativePrompt;
        private System.Windows.Forms.NumericUpDown numControlStrength;
        private System.Windows.Forms.ComboBox cboOutputFormat;
        private System.Windows.Forms.ComboBox cboStylePreset;

        // OpenAI controls
        private System.Windows.Forms.Panel pnlOpenAiSettings;
        private System.Windows.Forms.TextBox txtOpenAiApiKey;
        private System.Windows.Forms.TextBox txtOpenAiPrompt;
        private System.Windows.Forms.ComboBox cboOpenAiModel;
        private System.Windows.Forms.FlowLayoutPanel _referencesPanel;
        private System.Windows.Forms.Button _addReferenceButton;

        // Common controls
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.PictureBox pictureBoxOriginal;
        private System.Windows.Forms.PictureBox pictureBoxRendered;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.CheckBox chkHighQuality;

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
            this.Size = new System.Drawing.Size(1200, 800);
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
            splitContainer.Panel2MinSize = 600;

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
            settingsPanel = new System.Windows.Forms.Panel();
            settingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            settingsPanel.AutoScroll = true;
            settingsPanel.Padding = new System.Windows.Forms.Padding(10);
            splitContainer.Panel1.Controls.Add(settingsPanel);

            // Add Provider Selection Section
            System.Windows.Forms.Label lblProviderSection = new System.Windows.Forms.Label();
            lblProviderSection.Text = "AI Provider";
            lblProviderSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblProviderSection.Size = new System.Drawing.Size(200, 20);
            lblProviderSection.Location = new System.Drawing.Point(10, 10);

            cboProvider = new System.Windows.Forms.ComboBox();
            cboProvider.Size = new System.Drawing.Size(260, 25);
            cboProvider.Location = new System.Drawing.Point(10, 35);
            cboProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboProvider.Items.AddRange(new object[] { "StabilityAI", "OpenAI" });
            cboProvider.SelectedIndex = 0; // Default to Stability AI
            cboProvider.SelectedIndexChanged += CboProvider_SelectedIndexChanged;

            // === Stability AI Panel ===
            pnlStabilitySettings = new System.Windows.Forms.Panel();
            pnlStabilitySettings.Size = new System.Drawing.Size(280, 520);
            pnlStabilitySettings.Location = new System.Drawing.Point(10, 70);
            pnlStabilitySettings.Visible = true;

            System.Windows.Forms.Label lblStabilityApiSection = new System.Windows.Forms.Label();
            lblStabilityApiSection.Text = "API Configuration";
            lblStabilityApiSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblStabilityApiSection.Size = new System.Drawing.Size(200, 20);
            lblStabilityApiSection.Location = new System.Drawing.Point(0, 0);

            System.Windows.Forms.Label lblStabilityApiKey = new System.Windows.Forms.Label();
            lblStabilityApiKey.Text = "Stability AI API Key:";
            lblStabilityApiKey.Size = new System.Drawing.Size(120, 20);
            lblStabilityApiKey.Location = new System.Drawing.Point(0, 25);

            txtStabilityApiKey = new System.Windows.Forms.TextBox();
            txtStabilityApiKey.Size = new System.Drawing.Size(260, 20);
            txtStabilityApiKey.Location = new System.Drawing.Point(0, 45);
            txtStabilityApiKey.PasswordChar = '*';

            // Rendering settings section
            System.Windows.Forms.Label lblRenderSection = new System.Windows.Forms.Label();
            lblRenderSection.Text = "Rendering Configuration";
            lblRenderSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblRenderSection.Size = new System.Drawing.Size(200, 20);
            lblRenderSection.Location = new System.Drawing.Point(0, 75);

            System.Windows.Forms.Label lblPrompt = new System.Windows.Forms.Label();
            lblPrompt.Text = "Prompt:";
            lblPrompt.Size = new System.Drawing.Size(100, 20);
            lblPrompt.Location = new System.Drawing.Point(0, 100);

            txtPrompt = new System.Windows.Forms.TextBox();
            txtPrompt.Size = new System.Drawing.Size(260, 80);
            txtPrompt.Location = new System.Drawing.Point(0, 120);
            txtPrompt.Multiline = true;

            System.Windows.Forms.Label lblNegativePrompt = new System.Windows.Forms.Label();
            lblNegativePrompt.Text = "Negative Prompt:";
            lblNegativePrompt.Size = new System.Drawing.Size(100, 20);
            lblNegativePrompt.Location = new System.Drawing.Point(0, 210);

            txtNegativePrompt = new System.Windows.Forms.TextBox();
            txtNegativePrompt.Size = new System.Drawing.Size(260, 60);
            txtNegativePrompt.Location = new System.Drawing.Point(0, 230);
            txtNegativePrompt.Multiline = true;

            // Parameters
            System.Windows.Forms.Label lblControlStrength = new System.Windows.Forms.Label();
            lblControlStrength.Text = "Control Strength:";
            lblControlStrength.Size = new System.Drawing.Size(100, 20);
            lblControlStrength.Location = new System.Drawing.Point(0, 300);

            numControlStrength = new System.Windows.Forms.NumericUpDown();
            numControlStrength.Size = new System.Drawing.Size(80, 20);
            numControlStrength.Location = new System.Drawing.Point(130, 300);
            numControlStrength.DecimalPlaces = 2;
            numControlStrength.Increment = 0.1m;
            numControlStrength.Minimum = 0.1m;
            numControlStrength.Maximum = 1.0m;
            numControlStrength.Value = 0.7m;

            System.Windows.Forms.Label lblOutputFormat = new System.Windows.Forms.Label();
            lblOutputFormat.Text = "Output Format:";
            lblOutputFormat.Size = new System.Drawing.Size(100, 20);
            lblOutputFormat.Location = new System.Drawing.Point(0, 330);

            cboOutputFormat = new System.Windows.Forms.ComboBox();
            cboOutputFormat.Size = new System.Drawing.Size(80, 20);
            cboOutputFormat.Location = new System.Drawing.Point(130, 330);
            cboOutputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboOutputFormat.Items.AddRange(new object[] { "jpeg", "png", "webp" });
            cboOutputFormat.SelectedIndex = 0;

            System.Windows.Forms.Label lblStylePreset = new System.Windows.Forms.Label();
            lblStylePreset.Text = "Style Preset:";
            lblStylePreset.Size = new System.Drawing.Size(100, 20);
            lblStylePreset.Location = new System.Drawing.Point(0, 360);

            cboStylePreset = new System.Windows.Forms.ComboBox();
            cboStylePreset.Size = new System.Drawing.Size(170, 20);
            cboStylePreset.Location = new System.Drawing.Point(130, 360);
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
            chkHighQuality.Location = new System.Drawing.Point(0, 390);
            chkHighQuality.Checked = _highQualityRender;
            chkHighQuality.CheckedChanged += (s, e) => {
                _highQualityRender = chkHighQuality.Checked;
                CaptureCurrentView(); // Recapture with new quality settings
            };

            // Add Stability AI controls to panel
            pnlStabilitySettings.Controls.Add(lblStabilityApiSection);
            pnlStabilitySettings.Controls.Add(lblStabilityApiKey);
            pnlStabilitySettings.Controls.Add(txtStabilityApiKey);
            pnlStabilitySettings.Controls.Add(lblRenderSection);
            pnlStabilitySettings.Controls.Add(lblPrompt);
            pnlStabilitySettings.Controls.Add(txtPrompt);
            pnlStabilitySettings.Controls.Add(lblNegativePrompt);
            pnlStabilitySettings.Controls.Add(txtNegativePrompt);
            pnlStabilitySettings.Controls.Add(lblControlStrength);
            pnlStabilitySettings.Controls.Add(numControlStrength);
            pnlStabilitySettings.Controls.Add(lblOutputFormat);
            pnlStabilitySettings.Controls.Add(cboOutputFormat);
            pnlStabilitySettings.Controls.Add(lblStylePreset);
            pnlStabilitySettings.Controls.Add(cboStylePreset);
            pnlStabilitySettings.Controls.Add(chkHighQuality);

            // === OpenAI Panel ===
            pnlOpenAiSettings = new System.Windows.Forms.Panel();
            pnlOpenAiSettings.Size = new System.Drawing.Size(280, 520);
            pnlOpenAiSettings.Location = new System.Drawing.Point(10, 70);
            pnlOpenAiSettings.Visible = false; // Initially hidden

            System.Windows.Forms.Label lblOpenAiApiSection = new System.Windows.Forms.Label();
            lblOpenAiApiSection.Text = "API Configuration";
            lblOpenAiApiSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblOpenAiApiSection.Size = new System.Drawing.Size(200, 20);
            lblOpenAiApiSection.Location = new System.Drawing.Point(0, 0);

            System.Windows.Forms.Label lblOpenAiApiKey = new System.Windows.Forms.Label();
            lblOpenAiApiKey.Text = "OpenAI API Key:";
            lblOpenAiApiKey.Size = new System.Drawing.Size(120, 20);
            lblOpenAiApiKey.Location = new System.Drawing.Point(0, 25);

            txtOpenAiApiKey = new System.Windows.Forms.TextBox();
            txtOpenAiApiKey.Size = new System.Drawing.Size(260, 20);
            txtOpenAiApiKey.Location = new System.Drawing.Point(0, 45);
            txtOpenAiApiKey.PasswordChar = '*';

            // OpenAI specific controls
            System.Windows.Forms.Label lblOpenAiRenderSection = new System.Windows.Forms.Label();
            lblOpenAiRenderSection.Text = "Rendering Configuration";
            lblOpenAiRenderSection.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblOpenAiRenderSection.Size = new System.Drawing.Size(200, 20);
            lblOpenAiRenderSection.Location = new System.Drawing.Point(0, 75);

            System.Windows.Forms.Label lblOpenAiPrompt = new System.Windows.Forms.Label();
            lblOpenAiPrompt.Text = "Prompt:";
            lblOpenAiPrompt.Size = new System.Drawing.Size(100, 20);
            lblOpenAiPrompt.Location = new System.Drawing.Point(0, 100);

            txtOpenAiPrompt = new System.Windows.Forms.TextBox();
            txtOpenAiPrompt.Size = new System.Drawing.Size(260, 80);
            txtOpenAiPrompt.Location = new System.Drawing.Point(0, 120);
            txtOpenAiPrompt.Multiline = true;

            System.Windows.Forms.Label lblOpenAiModel = new System.Windows.Forms.Label();
            lblOpenAiModel.Text = "Model:";
            lblOpenAiModel.Size = new System.Drawing.Size(100, 20);
            lblOpenAiModel.Location = new System.Drawing.Point(0, 210);

            cboOpenAiModel = new System.Windows.Forms.ComboBox();
            cboOpenAiModel.Size = new System.Drawing.Size(200, 20);
            cboOpenAiModel.Location = new System.Drawing.Point(60, 210);
            cboOpenAiModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboOpenAiModel.Items.AddRange(new object[] { "gpt-image-1" }); // Add more as they become available
            cboOpenAiModel.SelectedIndex = 0;

            // Reference Images Section
            System.Windows.Forms.Label lblReferenceImages = new System.Windows.Forms.Label();
            lblReferenceImages.Text = "Reference Images";
            lblReferenceImages.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            lblReferenceImages.Size = new System.Drawing.Size(200, 20);
            lblReferenceImages.Location = new System.Drawing.Point(0, 240);

            // Description label
            System.Windows.Forms.Label lblReferenceDesc = new System.Windows.Forms.Label();
            lblReferenceDesc.Text = "Add images to influence the final rendering";
            lblReferenceDesc.Size = new System.Drawing.Size(260, 20);
            lblReferenceDesc.Location = new System.Drawing.Point(0, 265);
            lblReferenceDesc.ForeColor = System.Drawing.Color.DimGray;
            lblReferenceDesc.Font = new System.Drawing.Font(this.Font.FontFamily, 8);

            // Panel to hold reference thumbnails
            _referencesPanel = new System.Windows.Forms.FlowLayoutPanel();
            _referencesPanel.Size = new System.Drawing.Size(260, 80);
            _referencesPanel.Location = new System.Drawing.Point(0, 290);
            _referencesPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            _referencesPanel.AutoScroll = true;
            _referencesPanel.FlowDirection = FlowDirection.LeftToRight;
            _referencesPanel.WrapContents = true;

            // Add Reference button
            _addReferenceButton = new System.Windows.Forms.Button();
            _addReferenceButton.Text = "+";
            _addReferenceButton.Size = new System.Drawing.Size(30, 30);
            _addReferenceButton.Location = new System.Drawing.Point(230, 290);
            _addReferenceButton.Font = new System.Drawing.Font(this.Font.FontFamily, 14, System.Drawing.FontStyle.Bold);
            _addReferenceButton.UseVisualStyleBackColor = true;
            _addReferenceButton.Click += AddReferenceButton_Click;

            // OpenAI High Quality checkbox
            System.Windows.Forms.CheckBox chkOpenAiHighQuality = new System.Windows.Forms.CheckBox();
            chkOpenAiHighQuality.Text = "High-Quality Image Capture";
            chkOpenAiHighQuality.Size = new System.Drawing.Size(200, 20);
            chkOpenAiHighQuality.Location = new System.Drawing.Point(0, 380);
            chkOpenAiHighQuality.Checked = _highQualityRender;
            chkOpenAiHighQuality.CheckedChanged += (s, e) => {
                _highQualityRender = chkOpenAiHighQuality.Checked;
                CaptureCurrentView(); // Recapture with new quality settings
            };

            // Add OpenAI controls to its panel
            pnlOpenAiSettings.Controls.Add(lblOpenAiApiSection);
            pnlOpenAiSettings.Controls.Add(lblOpenAiApiKey);
            pnlOpenAiSettings.Controls.Add(txtOpenAiApiKey);
            pnlOpenAiSettings.Controls.Add(lblOpenAiRenderSection);
            pnlOpenAiSettings.Controls.Add(lblOpenAiPrompt);
            pnlOpenAiSettings.Controls.Add(txtOpenAiPrompt);
            pnlOpenAiSettings.Controls.Add(lblOpenAiModel);
            pnlOpenAiSettings.Controls.Add(cboOpenAiModel);
            pnlOpenAiSettings.Controls.Add(lblReferenceImages);
            pnlOpenAiSettings.Controls.Add(lblReferenceDesc);
            pnlOpenAiSettings.Controls.Add(_referencesPanel);
            pnlOpenAiSettings.Controls.Add(_addReferenceButton);
            pnlOpenAiSettings.Controls.Add(chkOpenAiHighQuality);

            // Action buttons (should be visible regardless of which panel is active)
            btnRender = new System.Windows.Forms.Button();
            btnRender.Text = "Render View";
            btnRender.Size = new System.Drawing.Size(120, 30);
            btnRender.Location = new System.Drawing.Point(10, 500);
            btnRender.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            btnRender.ForeColor = System.Drawing.Color.White;
            btnRender.Click += btnRender_Click;

            btnSaveSettings = new System.Windows.Forms.Button();
            btnSaveSettings.Text = "Save Settings";
            btnSaveSettings.Size = new System.Drawing.Size(120, 30);
            btnSaveSettings.Location = new System.Drawing.Point(140, 500);
            btnSaveSettings.Click += btnSaveSettings_Click;

            // Add a Sync View button
            Button btnSyncView = new Button();
            btnSyncView.Text = "Sync View";
            btnSyncView.Size = new System.Drawing.Size(120, 30);
            btnSyncView.Location = new System.Drawing.Point(10, 540); // Position below Render button
            btnSyncView.BackColor = System.Drawing.Color.FromArgb(0, 160, 160); // Distinct color
            btnSyncView.ForeColor = System.Drawing.Color.White;
            btnSyncView.Click += btnSyncView_Click;

            // Add a Save Image button
            Button btnSaveImage = new Button();
            btnSaveImage.Text = "Save Image";
            btnSaveImage.Size = new System.Drawing.Size(120, 30);
            btnSaveImage.Location = new System.Drawing.Point(140, 540);
            btnSaveImage.BackColor = System.Drawing.Color.FromArgb(220, 220, 220);
            btnSaveImage.ForeColor = System.Drawing.Color.Black;
            btnSaveImage.Click += btnSaveImage_Click;

            // Add all controls to the settings panel
            settingsPanel.Controls.Add(lblProviderSection);
            settingsPanel.Controls.Add(cboProvider);
            settingsPanel.Controls.Add(pnlStabilitySettings);
            settingsPanel.Controls.Add(pnlOpenAiSettings);
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

        // Provider selection changed event handler
        private void CboProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedProvider = cboProvider.SelectedItem.ToString();

            // Show/hide appropriate panels
            if (selectedProvider == "StabilityAI")
            {
                pnlStabilitySettings.Visible = true;
                pnlOpenAiSettings.Visible = false;
                lblStatus.Text = "Switched to Stability AI provider";
            }
            else if (selectedProvider == "OpenAI")
            {
                pnlStabilitySettings.Visible = false;
                pnlOpenAiSettings.Visible = true;
                lblStatus.Text = "Switched to OpenAI provider";
            }
        }

        private void LoadSettings()
        {
            // Set provider dropdown
            for (int i = 0; i < cboProvider.Items.Count; i++)
            {
                if (cboProvider.Items[i].ToString() == _settings.SelectedProvider)
                {
                    cboProvider.SelectedIndex = i;
                    break;
                }
            }

            // Load Stability AI settings
            txtStabilityApiKey.Text = _settings.StabilityApiKey;
            txtPrompt.Text = _settings.DefaultPrompt;
            txtNegativePrompt.Text = "";
            numControlStrength.Value = Convert.ToDecimal(_settings.ControlStrength);

            // Set output format
            for (int i = 0; i < cboOutputFormat.Items.Count; i++)
            {
                if (cboOutputFormat.Items[i].ToString() == _settings.OutputFormat)
                {
                    cboOutputFormat.SelectedIndex = i;
                    break;
                }
            }

            // Set style preset
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

            // Set high quality flag
            chkHighQuality.Checked = _highQualityRender;

            // Load OpenAI settings
            txtOpenAiApiKey.Text = _settings.OpenAiApiKey;
            txtOpenAiPrompt.Text = _settings.DefaultPrompt; // Initially use the same prompt

            // Select OpenAI model if it exists in the combobox
            for (int i = 0; i < cboOpenAiModel.Items.Count; i++)
            {
                if (cboOpenAiModel.Items[i].ToString() == _settings.OpenAiModel)
                {
                    cboOpenAiModel.SelectedIndex = i;
                    break;
                }
            }

            // Manually trigger the provider change event to show the right panel
            CboProvider_SelectedIndexChanged(cboProvider, EventArgs.Empty);
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Save selected provider
                _settings.SelectedProvider = cboProvider.SelectedItem.ToString();

                // Save Stability AI settings
                _settings.StabilityApiKey = txtStabilityApiKey.Text;
                _settings.ControlStrength = (float)numControlStrength.Value;
                _settings.OutputFormat = cboOutputFormat.SelectedItem.ToString();
                _settings.StylePreset = cboStylePreset.SelectedItem.ToString();

                // Save OpenAI settings
                _settings.OpenAiApiKey = txtOpenAiApiKey.Text;
                _settings.OpenAiModel = cboOpenAiModel.SelectedItem.ToString();
                _settings.EnableReferenceImages = _referenceImagePaths.Count > 0;

                // Check if prompts are different
                string stabilityPrompt = txtPrompt.Text;
                string openAiPrompt = txtOpenAiPrompt.Text;

                // If prompts are the same, save one value, otherwise keep them separate
                if (stabilityPrompt == openAiPrompt)
                {
                    _settings.DefaultPrompt = stabilityPrompt;
                }
                else
                {
                    // For now, just save the active provider's prompt
                    if (_settings.SelectedProvider == "StabilityAI")
                        _settings.DefaultPrompt = stabilityPrompt;
                    else
                        _settings.DefaultPrompt = openAiPrompt;
                }

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

        // Reference image management methods
        private void AddReferenceButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    dialog.Title = "Select Reference Image";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string imagePath = dialog.FileName;
                        
                        // Copy image to a temporary location
                        string refDir = GetSafeTempDirectory("References");
                        string destPath = Path.Combine(refDir, 
                            $"ref_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(imagePath)}");
                        
                        File.Copy(imagePath, destPath, true);
                        
                        // Add to our collection
                        _referenceImagePaths.Add(destPath);
                        
                        // Create and add the thumbnail
                        AddThumbnailToUI(destPath);
                        
                        // Update status
                        lblStatus.Text = $"Added reference image: {Path.GetFileName(imagePath)}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding reference image: {ex.Message}", "Reference Image Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddThumbnailToUI(string imagePath)
        {
            try
            {
                // Create a thumbnail container panel
                WinForms.Panel thumbnailPanel = new WinForms.Panel
                {
                    Size = new Size(80, 65),
                    Margin = new Padding(3),
                    BorderStyle = BorderStyle.FixedSingle
                };

                // Create PictureBox for the image
                PictureBox thumbnail = new PictureBox
                {
                    Size = new Size(70, 45),
                    Location = new Drawing.Point(5, 5),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Tag = imagePath // Store the path for reference
                };

                // Load the image
                using (Image fullImage = Image.FromFile(imagePath))
                {
                    // Create a thumbnail sized copy
                    thumbnail.Image = new Bitmap(fullImage, thumbnail.Size);
                }

                // Add click handler to show full image
                thumbnail.Click += Thumbnail_Click;

                // Create delete button
                Button deleteButton = new Button
                {
                    Text = "×",
                    Size = new Size(20, 20),
                    Location = new Drawing.Point(thumbnailPanel.Width - 25, 5),
                    Font = new Font(this.Font.FontFamily, 8, FontStyle.Bold),
                    UseVisualStyleBackColor = true,
                    FlatStyle = FlatStyle.Flat,
                    Tag = imagePath // Store the path for reference
                };
                deleteButton.Click += DeleteReferenceButton_Click;

                // Add controls to the panel
                thumbnailPanel.Controls.Add(thumbnail);
                thumbnailPanel.Controls.Add(deleteButton);

                // Add to flow layout panel
                _referencesPanel.Controls.Add(thumbnailPanel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating thumbnail: {ex.Message}", "Thumbnail Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Thumbnail_Click(object sender, EventArgs e)
        {
            PictureBox thumbnail = sender as PictureBox;
            if (thumbnail != null && thumbnail.Tag is string imagePath)
            {
                int index = _referenceImagePaths.IndexOf(imagePath);
                if (index >= 0)
                {
                    // Show the reference image viewer dialog
                    using (ReferenceImageViewer viewer = new ReferenceImageViewer(_referenceImagePaths, index))
                    {
                        viewer.ShowDialog();
                    }
                }
            }
        }

        private void DeleteReferenceButton_Click(object sender, EventArgs e)
        {
            Button deleteButton = sender as Button;
            if (deleteButton != null && deleteButton.Tag is string imagePath)
            {
                // Remove from our collection
                _referenceImagePaths.Remove(imagePath);
                
                // Find and remove the parent panel from the flow layout
                WinForms.Control parentPanel = deleteButton.Parent;
                if (parentPanel != null)
                {
                    _referencesPanel.Controls.Remove(parentPanel);
                    parentPanel.Dispose();
                }
                
                lblStatus.Text = $"Removed reference image: {Path.GetFileName(imagePath)}";
            }
        }

        private async void btnRender_Click(object sender, EventArgs e)
        {
            if (_isRendering)
            {
                MessageBox.Show("Rendering is already in progress.", "Please Wait",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get the selected provider
            string selectedProvider = cboProvider.SelectedItem.ToString();
            string apiKey = string.Empty;
            string prompt = string.Empty;
            
            if (selectedProvider == "StabilityAI")
            {
                apiKey = txtStabilityApiKey.Text;
                prompt = txtPrompt.Text;  // Use Stability AI prompt field
            }
            else if (selectedProvider == "OpenAI")
            {
                apiKey = txtOpenAiApiKey.Text;
                prompt = txtOpenAiPrompt.Text;  // Use OpenAI prompt field
            }

            // Check if API key is provided
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show($"Please enter your {selectedProvider} API key before rendering.",
                    "API Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isRendering = true;
            btnRender.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "Rendering in progress...";

            try
            {
                string resultPath = string.Empty;
                
                if (selectedProvider == "StabilityAI")
                {
                    // Use existing Stability AI path
                    var client = new StabilityApiClient(apiKey, _settings.VerboseLogging);
                    
                    // Get the style preset (empty string if nothing selected)
                    string stylePreset = cboStylePreset.SelectedItem.ToString();

                    // Render using Stability AI
                    resultPath = await client.ProcessRevitViewAsync(
                        _originalImagePath,
                        prompt,
                        (float)numControlStrength.Value,
                        txtNegativePrompt.Text,
                        cboOutputFormat.SelectedItem.ToString(),
                        stylePreset);
                }
                else if (selectedProvider == "OpenAI")
                {
                    // Use new OpenAI path with reference images
                    var client = new OpenAiApiClient(apiKey, _settings.VerboseLogging);
                    
                    // Get selected model
                    string model = cboOpenAiModel.SelectedItem.ToString();
                    
                    // Render using OpenAI
                    resultPath = await client.ProcessRevitViewWithReferencesAsync(
                        _originalImagePath,
                        _referenceImagePaths,  // Pass the reference images
                        prompt,
                        model);
                }

                // Update the UI with rendered image
                if (!string.IsNullOrEmpty(resultPath))
                {
                    LoadRenderedImage(resultPath);
                    
                    // Switch to the rendered tab
                    tabControl.SelectedIndex = 1;
                    
                    lblStatus.Text = "Rendering completed successfully";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during rendering: {ex.Message}", "Rendering Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Clean up any reference image thumbnails
            foreach (WinForms.Control control in _referencesPanel.Controls)
            {
                if (control is WinForms.Panel panel)
                {
                    foreach (WinForms.Control c in panel.Controls)
                    {
                        if (c is PictureBox pb && pb.Image != null)
                        {
                            pb.Image.Dispose();
                            pb.Image = null;
                        }
                    }
                }
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

            // Clean up any reference image thumbnails
            foreach (WinForms.Control control in _referencesPanel.Controls)
            {
                if (control is WinForms.Panel panel)
                {
                    foreach (WinForms.Control c in panel.Controls)
                    {
                        if (c is PictureBox pb && pb.Image != null)
                        {
                            pb.Image.Dispose();
                            pb.Image = null;
                        }
                    }
                }
            }
        }
    }
}
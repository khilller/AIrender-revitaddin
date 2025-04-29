using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace RevitAIRenderer
{
    public class CombinedRenderForm : WinForms.Form, IDisposable
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
        private WinForms.SplitContainer splitContainer;
        private WinForms.Panel settingsPanel;
        private WinForms.ComboBox cboProvider;

        // Stability AI controls
        private WinForms.Panel pnlStabilitySettings;
        private WinForms.TextBox txtStabilityApiKey;
        private WinForms.TextBox txtPrompt;
        private WinForms.TextBox txtNegativePrompt;
        private WinForms.NumericUpDown numControlStrength;
        private WinForms.ComboBox cboOutputFormat;
        private WinForms.ComboBox cboStylePreset;

        // OpenAI controls
        private WinForms.Panel pnlOpenAiSettings;
        private WinForms.TextBox txtOpenAiApiKey;
        private WinForms.TextBox txtOpenAiPrompt;
        private WinForms.ComboBox cboOpenAiModel;
        private WinForms.FlowLayoutPanel _referencesPanel;
        private WinForms.Button _addReferenceButton;

        // Common controls
        private WinForms.Button btnRender;
        private WinForms.Button btnSaveSettings;
        private WinForms.TabControl tabControl;
        private WinForms.PictureBox pictureBoxOriginal;
        private WinForms.PictureBox pictureBoxRendered;
        private WinForms.StatusStrip statusStrip;
        private WinForms.ToolStripStatusLabel lblStatus;
        private WinForms.ProgressBar progressBar;
        private WinForms.CheckBox chkHighQuality;

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
            this.Size = new Drawing.Size(1200, 800);
            this.StartPosition = WinForms.FormStartPosition.CenterScreen;
            this.MinimumSize = new Drawing.Size(900, 600);
            this.BackColor = Drawing.Color.WhiteSmoke;

            // Create a split container to divide the form
            splitContainer = new WinForms.SplitContainer();
            splitContainer.Dock = WinForms.DockStyle.Fill;
            splitContainer.Orientation = WinForms.Orientation.Horizontal;
            splitContainer.BackColor = Drawing.Color.White;
            splitContainer.BorderStyle = WinForms.BorderStyle.FixedSingle;

            // First add the control to the form
            this.Controls.Add(splitContainer);

            // Now set minimum sizes and splitter distance AFTER the control is added to the form
            // This gives the control a chance to size itself according to the form
            this.Shown += (s, e) => {
                // Set these values after the control is sized
                int totalHeight = splitContainer.Height;
                splitContainer.Panel1MinSize = Math.Min(250, totalHeight / 3);
                splitContainer.Panel2MinSize = Math.Min(400, totalHeight / 3);
                splitContainer.SplitterDistance = Math.Min(300, totalHeight / 2);
            };

            // === Left Panel (Settings) ===
            settingsPanel = new WinForms.Panel();
            settingsPanel.Dock = WinForms.DockStyle.Fill;
            settingsPanel.AutoScroll = true;
            settingsPanel.Padding = new WinForms.Padding(20);
            settingsPanel.BackColor = Drawing.Color.White;
            splitContainer.Panel1.Controls.Add(settingsPanel);

            // Add Provider Selection Section with rounded border
            WinForms.GroupBox providerGroup = new WinForms.GroupBox();
            providerGroup.Text = "AI Provider";
            providerGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            providerGroup.Size = new Drawing.Size(450, 80);
            providerGroup.Location = new Drawing.Point(20, 20);
            providerGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            providerGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            cboProvider = new WinForms.ComboBox();
            cboProvider.Size = new Drawing.Size(400, 30);
            cboProvider.Location = new Drawing.Point(20, 30);
            cboProvider.DropDownStyle = WinForms.ComboBoxStyle.DropDownList;
            cboProvider.Font = new Drawing.Font(this.Font.FontFamily, 10);
            cboProvider.Items.AddRange(new object[] { "StabilityAI", "OpenAI" });
            cboProvider.SelectedIndex = 0; // Default to Stability AI
            cboProvider.SelectedIndexChanged += CboProvider_SelectedIndexChanged;
            providerGroup.Controls.Add(cboProvider);

            // === Stability AI Panel ===
            pnlStabilitySettings = new WinForms.Panel();
            pnlStabilitySettings.Size = new Drawing.Size(450, 520);
            pnlStabilitySettings.Location = new Drawing.Point(20, 110);
            pnlStabilitySettings.Visible = true;

            WinForms.GroupBox stabilityApiGroup = new WinForms.GroupBox();
            stabilityApiGroup.Text = "API Configuration";
            stabilityApiGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            stabilityApiGroup.Size = new Drawing.Size(450, 100);
            stabilityApiGroup.Location = new Drawing.Point(0, 0);
            stabilityApiGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityApiGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            WinForms.Label lblStabilityApiKey = new WinForms.Label();
            lblStabilityApiKey.Text = "Stability AI API Key:";
            lblStabilityApiKey.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblStabilityApiKey.Size = new Drawing.Size(120, 20);
            lblStabilityApiKey.Location = new Drawing.Point(20, 30);
            lblStabilityApiKey.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityApiGroup.Controls.Add(lblStabilityApiKey);

            txtStabilityApiKey = new WinForms.TextBox();
            txtStabilityApiKey.Size = new Drawing.Size(400, 25);
            txtStabilityApiKey.Location = new Drawing.Point(20, 55);
            txtStabilityApiKey.PasswordChar = '*';
            txtStabilityApiKey.Font = new Drawing.Font(this.Font.FontFamily, 10);
            txtStabilityApiKey.BorderStyle = WinForms.BorderStyle.FixedSingle;
            stabilityApiGroup.Controls.Add(txtStabilityApiKey);

            // Rendering settings section
            WinForms.GroupBox stabilityRenderGroup = new WinForms.GroupBox();
            stabilityRenderGroup.Text = "Rendering Configuration";
            stabilityRenderGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            stabilityRenderGroup.Size = new Drawing.Size(450, 300);
            stabilityRenderGroup.Location = new Drawing.Point(0, 110);
            stabilityRenderGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            WinForms.Label lblPrompt = new WinForms.Label();
            lblPrompt.Text = "Prompt:";
            lblPrompt.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblPrompt.Size = new Drawing.Size(100, 20);
            lblPrompt.Location = new Drawing.Point(20, 30);
            lblPrompt.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.Controls.Add(lblPrompt);

            txtPrompt = new WinForms.TextBox();
            txtPrompt.Size = new Drawing.Size(400, 80);
            txtPrompt.Location = new Drawing.Point(20, 55);
            txtPrompt.Multiline = true;
            txtPrompt.Font = new Drawing.Font(this.Font.FontFamily, 10);
            txtPrompt.BorderStyle = WinForms.BorderStyle.FixedSingle;
            stabilityRenderGroup.Controls.Add(txtPrompt);

            WinForms.Label lblNegativePrompt = new WinForms.Label();
            lblNegativePrompt.Text = "Negative Prompt:";
            lblNegativePrompt.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblNegativePrompt.Size = new Drawing.Size(120, 20);
            lblNegativePrompt.Location = new Drawing.Point(20, 145);
            lblNegativePrompt.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.Controls.Add(lblNegativePrompt);

            txtNegativePrompt = new WinForms.TextBox();
            txtNegativePrompt.Size = new Drawing.Size(400, 60);
            txtNegativePrompt.Location = new Drawing.Point(20, 170);
            txtNegativePrompt.Multiline = true;
            txtNegativePrompt.Font = new Drawing.Font(this.Font.FontFamily, 10);
            txtNegativePrompt.BorderStyle = WinForms.BorderStyle.FixedSingle;
            stabilityRenderGroup.Controls.Add(txtNegativePrompt);

            // Parameters
            WinForms.Label lblControlStrength = new WinForms.Label();
            lblControlStrength.Text = "Control Strength:";
            lblControlStrength.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblControlStrength.Size = new Drawing.Size(100, 20);
            lblControlStrength.Location = new Drawing.Point(20, 240);
            lblControlStrength.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.Controls.Add(lblControlStrength);

            numControlStrength = new WinForms.NumericUpDown();
            numControlStrength.Size = new Drawing.Size(80, 20);
            numControlStrength.Location = new Drawing.Point(130, 240);
            numControlStrength.DecimalPlaces = 2;
            numControlStrength.Increment = 0.1m;
            numControlStrength.Minimum = 0.1m;
            numControlStrength.Maximum = 1.0m;
            numControlStrength.Value = 0.7m;
            numControlStrength.Font = new Drawing.Font(this.Font.FontFamily, 10);
            stabilityRenderGroup.Controls.Add(numControlStrength);

            WinForms.Label lblOutputFormat = new WinForms.Label();
            lblOutputFormat.Text = "Output Format:";
            lblOutputFormat.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblOutputFormat.Size = new Drawing.Size(100, 20);
            lblOutputFormat.Location = new Drawing.Point(230, 240);
            lblOutputFormat.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.Controls.Add(lblOutputFormat);

            cboOutputFormat = new WinForms.ComboBox();
            cboOutputFormat.Size = new Drawing.Size(80, 20);
            cboOutputFormat.Location = new Drawing.Point(340, 240);
            cboOutputFormat.DropDownStyle = WinForms.ComboBoxStyle.DropDownList;
            cboOutputFormat.Font = new Drawing.Font(this.Font.FontFamily, 10);
            cboOutputFormat.Items.AddRange(new object[] { "jpeg", "png", "webp" });
            cboOutputFormat.SelectedIndex = 0;
            stabilityRenderGroup.Controls.Add(cboOutputFormat);

            WinForms.Label lblStylePreset = new WinForms.Label();
            lblStylePreset.Text = "Style Preset:";
            lblStylePreset.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblStylePreset.Size = new Drawing.Size(100, 20);
            lblStylePreset.Location = new Drawing.Point(20, 270);
            lblStylePreset.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            stabilityRenderGroup.Controls.Add(lblStylePreset);

            cboStylePreset = new WinForms.ComboBox();
            cboStylePreset.Size = new Drawing.Size(170, 20);
            cboStylePreset.Location = new Drawing.Point(130, 270);
            cboStylePreset.DropDownStyle = WinForms.ComboBoxStyle.DropDownList;
            cboStylePreset.Font = new Drawing.Font(this.Font.FontFamily, 10);
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
            stabilityRenderGroup.Controls.Add(cboStylePreset);

            // Add high-quality option
            WinForms.CheckBox chkStabilityHighQuality = new WinForms.CheckBox();
            chkStabilityHighQuality.Text = "High-Quality Image Capture";
            chkStabilityHighQuality.Size = new Drawing.Size(200, 20);
            chkStabilityHighQuality.Location = new Drawing.Point(20, 420);
            chkStabilityHighQuality.Checked = _highQualityRender;
            chkStabilityHighQuality.Font = new Drawing.Font(this.Font.FontFamily, 10);
            chkStabilityHighQuality.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            chkStabilityHighQuality.CheckedChanged += (s, e) => {
                _highQualityRender = chkStabilityHighQuality.Checked;
                CaptureCurrentView(); // Recapture with new quality settings
            };

            // Add Stability AI controls to panel
            pnlStabilitySettings.Controls.Add(stabilityApiGroup);
            pnlStabilitySettings.Controls.Add(stabilityRenderGroup);
            pnlStabilitySettings.Controls.Add(chkStabilityHighQuality);

            // === OpenAI Panel ===
            pnlOpenAiSettings = new WinForms.Panel();
            pnlOpenAiSettings.Size = new Drawing.Size(450, 520);
            pnlOpenAiSettings.Location = new Drawing.Point(20, 110);
            pnlOpenAiSettings.Visible = false; // Initially hidden

            // API Configuration section with rounded border
            WinForms.GroupBox apiConfigGroup = new WinForms.GroupBox();
            apiConfigGroup.Text = "API Configuration";
            apiConfigGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            apiConfigGroup.Size = new Drawing.Size(450, 100);
            apiConfigGroup.Location = new Drawing.Point(0, 0);
            apiConfigGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            apiConfigGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            // OpenAI API Key label and textbox
            WinForms.Label lblOpenAiApiKey = new WinForms.Label();
            lblOpenAiApiKey.Text = "OpenAI API Key:";
            lblOpenAiApiKey.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblOpenAiApiKey.Size = new Drawing.Size(120, 25);
            lblOpenAiApiKey.Location = new Drawing.Point(20, 30);
            lblOpenAiApiKey.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            apiConfigGroup.Controls.Add(lblOpenAiApiKey);

            txtOpenAiApiKey = new WinForms.TextBox();
            txtOpenAiApiKey.Size = new Drawing.Size(400, 25);
            txtOpenAiApiKey.Location = new Drawing.Point(20, 55);
            txtOpenAiApiKey.PasswordChar = '*';
            txtOpenAiApiKey.Font = new Drawing.Font(this.Font.FontFamily, 10);
            txtOpenAiApiKey.BorderStyle = WinForms.BorderStyle.FixedSingle;
            apiConfigGroup.Controls.Add(txtOpenAiApiKey);

            // Rendering Configuration section
            WinForms.GroupBox renderConfigGroup = new WinForms.GroupBox();
            renderConfigGroup.Text = "Rendering Configuration";
            renderConfigGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            renderConfigGroup.Size = new Drawing.Size(450, 210);
            renderConfigGroup.Location = new Drawing.Point(0, 110);
            renderConfigGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            renderConfigGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            // Model dropdown
            WinForms.Label lblOpenAiModel = new WinForms.Label();
            lblOpenAiModel.Text = "Model:";
            lblOpenAiModel.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblOpenAiModel.Size = new Drawing.Size(60, 25);
            lblOpenAiModel.Location = new Drawing.Point(20, 30);
            lblOpenAiModel.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            renderConfigGroup.Controls.Add(lblOpenAiModel);

            cboOpenAiModel = new WinForms.ComboBox();
            cboOpenAiModel.Size = new Drawing.Size(300, 25);
            cboOpenAiModel.Location = new Drawing.Point(90, 30);
            cboOpenAiModel.DropDownStyle = WinForms.ComboBoxStyle.DropDownList;
            cboOpenAiModel.Font = new Drawing.Font(this.Font.FontFamily, 10);
            cboOpenAiModel.Items.AddRange(new object[] { "gpt-image-1" });
            cboOpenAiModel.SelectedIndex = 0;
            renderConfigGroup.Controls.Add(cboOpenAiModel);

            // Prompt field
            WinForms.Label lblOpenAiPrompt = new WinForms.Label();
            lblOpenAiPrompt.Text = "Prompt:";
            lblOpenAiPrompt.Font = new Drawing.Font(this.Font.FontFamily, 9);
            lblOpenAiPrompt.Size = new Drawing.Size(100, 25);
            lblOpenAiPrompt.Location = new Drawing.Point(20, 65);
            lblOpenAiPrompt.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            renderConfigGroup.Controls.Add(lblOpenAiPrompt);

            txtOpenAiPrompt = new WinForms.TextBox();
            txtOpenAiPrompt.Size = new Drawing.Size(400, 80);
            txtOpenAiPrompt.Location = new Drawing.Point(20, 90);
            txtOpenAiPrompt.Multiline = true;
            txtOpenAiPrompt.Font = new Drawing.Font(this.Font.FontFamily, 10);
            txtOpenAiPrompt.BorderStyle = WinForms.BorderStyle.FixedSingle;
            renderConfigGroup.Controls.Add(txtOpenAiPrompt);

            // Reference Images Section
            WinForms.GroupBox refImagesGroup = new WinForms.GroupBox();
            refImagesGroup.Text = "Reference Images";
            refImagesGroup.Font = new Drawing.Font(this.Font.FontFamily, 11, Drawing.FontStyle.Bold);
            refImagesGroup.Size = new Drawing.Size(450, 150);
            refImagesGroup.Location = new Drawing.Point(0, 330);
            refImagesGroup.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            refImagesGroup.BackColor = Drawing.Color.FromArgb(245, 245, 245);

            // Description label
            WinForms.Label lblReferenceDesc = new WinForms.Label();
            lblReferenceDesc.Text = "Add images to influence the final rendering";
            lblReferenceDesc.Size = new Drawing.Size(400, 20);
            lblReferenceDesc.Location = new Drawing.Point(20, 30);
            lblReferenceDesc.ForeColor = Drawing.Color.DimGray;
            lblReferenceDesc.Font = new Drawing.Font(this.Font.FontFamily, 8);
            refImagesGroup.Controls.Add(lblReferenceDesc);

            // Panel to hold reference thumbnails
            _referencesPanel = new WinForms.FlowLayoutPanel();
            _referencesPanel.Size = new Drawing.Size(400, 80);
            _referencesPanel.Location = new Drawing.Point(20, 50);
            _referencesPanel.BorderStyle = WinForms.BorderStyle.None;
            _referencesPanel.AutoScroll = true;
            _referencesPanel.FlowDirection = WinForms.FlowDirection.LeftToRight;
            _referencesPanel.WrapContents = true;
            _referencesPanel.BackColor = Drawing.Color.White;
            refImagesGroup.Controls.Add(_referencesPanel);

            // Add Reference button
            _addReferenceButton = new WinForms.Button();
            _addReferenceButton.Text = "+";
            _addReferenceButton.Size = new Drawing.Size(40, 40);
            _addReferenceButton.Location = new Drawing.Point(380, 70);
            _addReferenceButton.Font = new Drawing.Font(this.Font.FontFamily, 14, Drawing.FontStyle.Bold);
            _addReferenceButton.UseVisualStyleBackColor = true;
            _addReferenceButton.FlatStyle = WinForms.FlatStyle.Flat;
            _addReferenceButton.FlatAppearance.BorderSize = 1;
            _addReferenceButton.FlatAppearance.BorderColor = Drawing.Color.LightGray;
            _addReferenceButton.Click += AddReferenceButton_Click;
            refImagesGroup.Controls.Add(_addReferenceButton);

            // OpenAI High Quality checkbox
            chkHighQuality = new WinForms.CheckBox();
            chkHighQuality.Text = "High-Quality Image Capture";
            chkHighQuality.Size = new Drawing.Size(300, 25);
            chkHighQuality.Location = new Drawing.Point(20, 490);
            chkHighQuality.Checked = _highQualityRender;
            chkHighQuality.Font = new Drawing.Font(this.Font.FontFamily, 10);
            chkHighQuality.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            chkHighQuality.CheckedChanged += (s, e) => {
                _highQualityRender = chkHighQuality.Checked;
                CaptureCurrentView(); // Recapture with new quality settings
            };

            // Add OpenAI controls to its panel
            pnlOpenAiSettings.Controls.Add(apiConfigGroup);
            pnlOpenAiSettings.Controls.Add(renderConfigGroup);
            pnlOpenAiSettings.Controls.Add(refImagesGroup);
            pnlOpenAiSettings.Controls.Add(chkHighQuality);

            // Action buttons (styled to match your screenshot)
            btnRender = new WinForms.Button();
            btnRender.Text = "Render View";
            btnRender.Size = new Drawing.Size(200, 40);
            btnRender.Location = new Drawing.Point(20, 640);
            btnRender.BackColor = Drawing.Color.FromArgb(0, 122, 204);
            btnRender.ForeColor = Drawing.Color.White;
            btnRender.FlatStyle = WinForms.FlatStyle.Flat;
            btnRender.FlatAppearance.BorderSize = 0;
            btnRender.Font = new Drawing.Font(this.Font.FontFamily, 10, Drawing.FontStyle.Bold);
            btnRender.Click += btnRender_Click;

            btnSaveSettings = new WinForms.Button();
            btnSaveSettings.Text = "Save Settings";
            btnSaveSettings.Size = new Drawing.Size(200, 40);
            btnSaveSettings.Location = new Drawing.Point(230, 640);
            btnSaveSettings.BackColor = Drawing.Color.FromArgb(230, 230, 230);
            btnSaveSettings.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            btnSaveSettings.FlatStyle = WinForms.FlatStyle.Flat;
            btnSaveSettings.FlatAppearance.BorderSize = 0;
            btnSaveSettings.Font = new Drawing.Font(this.Font.FontFamily, 10);
            btnSaveSettings.Click += btnSaveSettings_Click;

            // Sync View button
            WinForms.Button btnSyncView = new WinForms.Button();
            btnSyncView.Text = "Sync View";
            btnSyncView.Size = new Drawing.Size(200, 40);
            btnSyncView.Location = new Drawing.Point(20, 690);
            btnSyncView.BackColor = Drawing.Color.FromArgb(0, 160, 160);
            btnSyncView.ForeColor = Drawing.Color.White;
            btnSyncView.FlatStyle = WinForms.FlatStyle.Flat;
            btnSyncView.FlatAppearance.BorderSize = 0;
            btnSyncView.Font = new Drawing.Font(this.Font.FontFamily, 10);
            btnSyncView.Click += btnSyncView_Click;

            // Save Image button
            WinForms.Button btnSaveImage = new WinForms.Button();
            btnSaveImage.Text = "Save Image";
            btnSaveImage.Size = new Drawing.Size(200, 40);
            btnSaveImage.Location = new Drawing.Point(230, 690);
            btnSaveImage.BackColor = Drawing.Color.FromArgb(230, 230, 230);
            btnSaveImage.ForeColor = Drawing.Color.FromArgb(60, 60, 60);
            btnSaveImage.FlatStyle = WinForms.FlatStyle.Flat;
            btnSaveImage.FlatAppearance.BorderSize = 0;
            btnSaveImage.Font = new Drawing.Font(this.Font.FontFamily, 10);
            btnSaveImage.Click += btnSaveImage_Click;

            // Add all controls to the settings panel
            settingsPanel.Controls.Add(providerGroup);
            settingsPanel.Controls.Add(pnlStabilitySettings);
            settingsPanel.Controls.Add(pnlOpenAiSettings);
            settingsPanel.Controls.Add(btnRender);
            settingsPanel.Controls.Add(btnSaveSettings);
            settingsPanel.Controls.Add(btnSyncView);
            settingsPanel.Controls.Add(btnSaveImage);

            // === Right Panel (Image Viewer) ===
            WinForms.Panel imageViewerPanel = new WinForms.Panel();
            imageViewerPanel.Dock = WinForms.DockStyle.Fill;
            imageViewerPanel.BackColor = Drawing.Color.White;
            imageViewerPanel.Padding = new WinForms.Padding(10);
            splitContainer.Panel2.Controls.Add(imageViewerPanel);

            // Tabbed interface for image views
            tabControl = new WinForms.TabControl();
            tabControl.Dock = WinForms.DockStyle.Fill;
            tabControl.Font = new Drawing.Font(this.Font.FontFamily, 10);
            imageViewerPanel.Controls.Add(tabControl);

            // Original image tab
            WinForms.TabPage tabOriginal = new WinForms.TabPage("Original View");
            tabOriginal.BackColor = Drawing.Color.White;
            pictureBoxOriginal = new WinForms.PictureBox();
            pictureBoxOriginal.Dock = WinForms.DockStyle.Fill;
            pictureBoxOriginal.SizeMode = WinForms.PictureBoxSizeMode.Zoom;
            pictureBoxOriginal.BackColor = Drawing.Color.FromArgb(240, 240, 240);
            tabOriginal.Controls.Add(pictureBoxOriginal);

            // Rendered image tab
            WinForms.TabPage tabRendered = new WinForms.TabPage("AI Rendered");
            tabRendered.BackColor = Drawing.Color.White;
            pictureBoxRendered = new WinForms.PictureBox();
            pictureBoxRendered.Dock = WinForms.DockStyle.Fill;
            pictureBoxRendered.SizeMode = WinForms.PictureBoxSizeMode.Zoom;
            pictureBoxRendered.BackColor = Drawing.Color.FromArgb(240, 240, 240);
            tabRendered.Controls.Add(pictureBoxRendered);

            tabControl.TabPages.Add(tabOriginal);
            tabControl.TabPages.Add(tabRendered);

            // Status bar for information
            statusStrip = new WinForms.StatusStrip();
            statusStrip.BackColor = Drawing.Color.WhiteSmoke;
            lblStatus = new WinForms.ToolStripStatusLabel();
            lblStatus.Text = "Ready";
            lblStatus.Spring = true;
            lblStatus.TextAlign = Drawing.ContentAlignment.MiddleLeft;
            lblStatus.Font = new Drawing.Font(this.Font.FontFamily, 9);
            statusStrip.Items.Add(lblStatus);
            this.Controls.Add(statusStrip);

            // Progress bar for rendering
            progressBar = new WinForms.ProgressBar();
            progressBar.Style = WinForms.ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Height = 6;
            progressBar.Dock = WinForms.DockStyle.Bottom;
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
                WinForms.MessageBox.Show($"Error saving settings: {ex.Message}", "Settings Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            // Check if we have a rendered image
            if (string.IsNullOrEmpty(_renderedImagePath) || !File.Exists(_renderedImagePath))
            {
                WinForms.MessageBox.Show("No rendered image available to save. Please render a view first.",
                    "No Image Available", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Show a save file dialog
                WinForms.SaveFileDialog saveDialog = new WinForms.SaveFileDialog
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

                if (saveDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    // Copy the file to the selected location
                    File.Copy(_renderedImagePath, saveDialog.FileName, true);
                    WinForms.MessageBox.Show($"Image saved successfully to:\n{saveDialog.FileName}",
                        "Save Complete", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show($"Error saving image: {ex.Message}", "Save Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
        }

        private void btnSyncView_Click(object sender, EventArgs e)
        {
            try
            {
                // Show progress indicator
                lblStatus.Text = "Synchronizing with Revit view...";
                progressBar.Visible = true;
                this.Cursor = WinForms.Cursors.WaitCursor;

                // Recapture the view with current Revit settings
                CaptureCurrentView();

                // Switch to the original view tab
                tabControl.SelectedIndex = 0;

                // Success message
                lblStatus.Text = "View synchronized successfully";
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show($"Error synchronizing view: {ex.Message}", "Sync Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                lblStatus.Text = "View synchronization failed";
            }
            finally
            {
                // Reset UI state
                progressBar.Visible = false;
                this.Cursor = WinForms.Cursors.Default;
            }
        }

        // Reference image management methods
        private void AddReferenceButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (WinForms.OpenFileDialog dialog = new WinForms.OpenFileDialog())
                {
                    dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    dialog.Title = "Select Reference Image";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
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
                WinForms.MessageBox.Show($"Error adding reference image: {ex.Message}", "Reference Image Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
        }

        private void AddThumbnailToUI(string imagePath)
        {
            try
            {
                // Create a thumbnail container panel
                WinForms.Panel thumbnailPanel = new WinForms.Panel
                {
                    Size = new Drawing.Size(80, 65),
                    Margin = new WinForms.Padding(3),
                    BorderStyle = WinForms.BorderStyle.FixedSingle
                };

                // Create PictureBox for the image
                WinForms.PictureBox thumbnail = new WinForms.PictureBox
                {
                    Size = new Drawing.Size(70, 45),
                    Location = new Drawing.Point(5, 5),
                    SizeMode = WinForms.PictureBoxSizeMode.Zoom,
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
                WinForms.Button deleteButton = new WinForms.Button
                {
                    Text = "×",
                    Size = new Drawing.Size(20, 20),
                    Location = new Drawing.Point(thumbnailPanel.Width - 25, 5),
                    Font = new Drawing.Font(this.Font.FontFamily, 8, Drawing.FontStyle.Bold),
                    UseVisualStyleBackColor = true,
                    FlatStyle = WinForms.FlatStyle.Flat,
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
                WinForms.MessageBox.Show($"Error creating thumbnail: {ex.Message}", "Thumbnail Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
        }

        private void Thumbnail_Click(object sender, EventArgs e)
        {
            WinForms.PictureBox thumbnail = sender as WinForms.PictureBox;
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
            WinForms.Button deleteButton = sender as WinForms.Button;
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
                WinForms.MessageBox.Show("Rendering is already in progress.", "Please Wait",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
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
                WinForms.MessageBox.Show($"Please enter your {selectedProvider} API key before rendering.",
                    "API Key Required", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
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
                WinForms.MessageBox.Show($"Error during rendering: {ex.Message}", "Rendering Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
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
                WinForms.MessageBox.Show($"Failed to capture view: {ex.Message}", "Capture Error",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
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
                    Drawing.Bitmap resizedImage = new Drawing.Bitmap(newWidth, newHeight);
                    using (Drawing.Graphics g = Drawing.Graphics.FromImage(resizedImage))
                    {
                        g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
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
                    WinForms.MessageBox.Show($"Error loading image: {ex.Message}", "Image Error",
                        WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
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
                    WinForms.MessageBox.Show($"Error loading rendered image: {ex.Message}", "Image Error",
                        WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
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
                    Drawing.Bitmap croppedImage = new Drawing.Bitmap(newWidth, newHeight);
                    using (Drawing.Graphics g = Drawing.Graphics.FromImage(croppedImage))
                    {
                        // Center the crop
                        int x = (_originalImage.Width - newWidth) / 2;
                        int y = (_originalImage.Height - newHeight) / 2;

                        // Draw the cropped portion
                        g.DrawImage(_originalImage,
                                    new Drawing.Rectangle(0, 0, newWidth, newHeight),
                                    new Drawing.Rectangle(x, y, newWidth, newHeight),
                                    Drawing.GraphicsUnit.Pixel);
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
                        if (c is WinForms.PictureBox pb && pb.Image != null)
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

        protected override void OnFormClosing(WinForms.FormClosingEventArgs e)
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
                        if (c is WinForms.PictureBox pb && pb.Image != null)
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
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;

namespace RevitAIRenderer
{
    public partial class RenderSettingsForm : Form
    {
        public AiRendererSettings Settings { get; private set; }

        public RenderSettingsForm(AiRendererSettings settings)
        {
            Settings = settings;
            InitializeComponent();

            // Initialize form values AFTER the component initialization
            txtApiKey.Text = settings.ApiKey;
            txtPrompt.Text = settings.DefaultPrompt;
            numStrength.Value = (decimal)settings.Strength;
            numSteps.Value = settings.Steps;
            numGuidance.Value = (decimal)settings.GuidanceScale;
            chkOfflineMode.Checked = settings.OfflineMode;

            // New model settings
            if (string.IsNullOrEmpty(settings.OutputFormat))
            {
                cboOutputFormat.SelectedIndex = 0; // Default to first item (jpeg)
            }
            else
            {
                // Select the appropriate item
                for (int i = 0; i < cboOutputFormat.Items.Count; i++)
                {
                    if (cboOutputFormat.Items[i].ToString().Equals(settings.OutputFormat, StringComparison.OrdinalIgnoreCase))
                    {
                        cboOutputFormat.SelectedIndex = i;
                        break;
                    }
                }

                // If not found, default to first item
                if (cboOutputFormat.SelectedIndex == -1)
                {
                    cboOutputFormat.SelectedIndex = 0;
                }
            }

            numControlLoraStrength.Value = (decimal)settings.ControlLoraStrength;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Update settings from form values
            Settings.ApiKey = txtApiKey.Text;
            Settings.DefaultPrompt = txtPrompt.Text;
            Settings.Strength = (float)numStrength.Value;
            Settings.Steps = (int)numSteps.Value;
            Settings.GuidanceScale = (float)numGuidance.Value;
            Settings.OfflineMode = chkOfflineMode.Checked;

            // New model settings
            Settings.OutputFormat = cboOutputFormat.SelectedItem.ToString();
            Settings.ControlLoraStrength = (float)numControlLoraStrength.Value;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void InitializeComponent()
        {
            // Form designer code
            this.Text = "AI Render Settings";
            this.Size = new System.Drawing.Size(450, 520); // Increased height for new controls
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create a main panel with scrolling capability
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            this.Controls.Add(mainPanel);

            // API Key section
            Label lblApiSection = new Label
            {
                Text = "API Configuration",
                Location = new Point(20, 20),
                Size = new Size(200, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            Label lblApiKey = new Label
            {
                Text = "FAL.ai API Key:",
                Location = new Point(20, 50),
                Size = new Size(100, 20)
            };

            txtApiKey = new TextBox
            {
                Location = new Point(130, 50),
                Size = new Size(280, 20),
                PasswordChar = '*'
            };

            // Prompt section
            Label lblPromptSection = new Label
            {
                Text = "Rendering Configuration",
                Location = new Point(20, 90),
                Size = new Size(200, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            Label lblPrompt = new Label
            {
                Text = "Prompt:",
                Location = new Point(20, 120),
                Size = new Size(100, 20)
            };

            txtPrompt = new TextBox
            {
                Location = new Point(130, 120),
                Size = new Size(280, 60),
                Multiline = true
            };

            // Parameters
            Label lblStrength = new Label
            {
                Text = "Strength:",
                Location = new Point(20, 200),
                Size = new Size(100, 20)
            };

            numStrength = new NumericUpDown
            {
                Location = new Point(130, 200),
                Size = new Size(80, 20),
                DecimalPlaces = 2,
                Increment = 0.05m,
                Minimum = 0.1m,
                Maximum = 1.0m,
                Value = 0.85m  // Updated default
            };

            Label lblSteps = new Label
            {
                Text = "Steps:",
                Location = new Point(20, 230),
                Size = new Size(100, 20)
            };

            numSteps = new NumericUpDown
            {
                Location = new Point(130, 230),
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 100,
                Value = 28  // Updated default
            };

            Label lblGuidance = new Label
            {
                Text = "Guidance Scale:",
                Location = new Point(20, 260),
                Size = new Size(100, 20)
            };

            numGuidance = new NumericUpDown
            {
                Location = new Point(130, 260),
                Size = new Size(80, 20),
                DecimalPlaces = 1,
                Increment = 0.1m,
                Minimum = 1.0m,
                Maximum = 20.0m,
                Value = 3.5m
            };

            // New model settings
            Label lblOutputFormat = new Label
            {
                Text = "Output Format:",
                Location = new Point(20, 290),
                Size = new Size(100, 20)
            };

            cboOutputFormat = new ComboBox
            {
                Location = new Point(130, 290),
                Size = new Size(80, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Add output format options
            cboOutputFormat.Items.AddRange(new object[] { "jpeg", "png" });
            cboOutputFormat.SelectedIndex = 0; // Default to jpeg

            Label lblControlLoraStrength = new Label
            {
                Text = "LoRA Strength:",
                Location = new Point(20, 320),
                Size = new Size(100, 20)
            };

            numControlLoraStrength = new NumericUpDown
            {
                Location = new Point(130, 320),
                Size = new Size(80, 20),
                DecimalPlaces = 2,
                Increment = 0.1m,
                Minimum = 0.1m,
                Maximum = 2.0m,
                Value = 1.0m // Default value
            };

            // Add a tooltip for the new parameter
            ToolTip loraTooltip = new ToolTip();
            loraTooltip.SetToolTip(numControlLoraStrength, "Controls the influence of the LoRA model on the final result");

            // Network settings section
            Label lblNetworkHeader = new Label
            {
                Text = "Network Settings",
                Location = new Point(20, 360),
                Size = new Size(200, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            chkOfflineMode = new CheckBox
            {
                Text = "Offline Mode (for testing)",
                Location = new Point(20, 390),
                Size = new Size(200, 20)
            };

            // Help button for network troubleshooting
            Button btnNetworkHelp = new Button
            {
                Text = "Network Troubleshooting",
                Location = new Point(20, 430),
                Size = new Size(170, 30)
            };
            btnNetworkHelp.Click += (s, e) => {
                MessageBox.Show(
                    "Network Troubleshooting Tips:\n\n" +
                    "1. Ensure your internet connection is active\n" +
                    "2. Check if your firewall allows outbound connections\n" +
                    "3. If using a corporate network, ask IT about API access\n" +
                    "4. Verify your API key is valid and not expired\n" +
                    "5. Try enabling Offline Mode for testing without API access",
                    "Network Troubleshooting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            // Information button for new model
            Button btnModelInfo = new Button
            {
                Text = "About the New Model",
                Location = new Point(240, 320),
                Size = new Size(170, 30)
            };
            btnModelInfo.Click += (s, e) => {
                MessageBox.Show(
                    "New Model Information:\n\n" +
                    "This version uses an updated AI model with LoRA (Low-Rank Adaptation) control. " +
                    "The LoRA control allows for better preservation of architectural details while " +
                    "applying the style from the prompt.\n\n" +
                    "Recommended settings:\n" +
                    "- Strength: 0.85 (lower preserves more of original image)\n" +
                    "- Steps: 28-40 (higher gives better quality but takes longer)\n" +
                    "- LoRA Strength: 1.0 (adjust for more/less influence)",
                    "New Model Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            // Buttons
            Button btnOK = new Button
            {
                Text = "OK",
                Location = new Point(240, 430),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += btnOK_Click;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(330, 430),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += btnCancel_Click;

            // Add controls to the panel
            mainPanel.Controls.AddRange(new Control[]
            {
                lblApiSection, lblApiKey, txtApiKey,
                lblPromptSection, lblPrompt, txtPrompt,
                lblStrength, numStrength,
                lblSteps, numSteps,
                lblGuidance, numGuidance,
                lblOutputFormat, cboOutputFormat,
                lblControlLoraStrength, numControlLoraStrength, btnModelInfo,
                lblNetworkHeader, chkOfflineMode, btnNetworkHelp,
                btnOK, btnCancel
            });
        }

        private TextBox txtApiKey;
        private TextBox txtPrompt;
        private NumericUpDown numStrength;
        private NumericUpDown numSteps;
        private NumericUpDown numGuidance;
        private CheckBox chkOfflineMode;
        private ComboBox cboOutputFormat;
        private NumericUpDown numControlLoraStrength;
    }
}
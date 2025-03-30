using System;
using System.Windows.Forms;

namespace RevitAIRenderer
{
    public partial class ProgressForm : Form
    {
        public ProgressForm(string title, string message)
        {
            InitializeComponent();
            this.Text = title;
            lblMessage.Text = message;
        }

        public void UpdateMessage(string message)
        {
            // Check if we need to invoke to avoid cross-thread issues
            if (lblMessage.InvokeRequired)
            {
                lblMessage.Invoke(new Action<string>(UpdateMessage), message);
                return;
            }

            lblMessage.Text = message;
            Application.DoEvents();
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblMessage = new Label
            {
                Text = "Processing...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(360, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(360, 30),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            this.Controls.AddRange(new Control[] { lblMessage, progressBar });
        }

        private Label lblMessage;
        private ProgressBar progressBar;
    }
}
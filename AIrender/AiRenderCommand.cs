using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Windows.Forms;

namespace RevitAIRenderer
{
    [Transaction(TransactionMode.ReadOnly)]
    public class AiRenderCommand : IExternalCommand
    {
        // Static field to maintain a reference to the form
        private static CombinedRenderForm _form = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Load settings
                var settings = AiRendererSettings.Load();

                // Check if form already exists
                if (_form != null && !_form.IsDisposed)
                {
                    // Bring existing form to front
                    _form.BringToFront();
                    _form.Focus();
                    _form.WindowState = FormWindowState.Normal;

                    // You could update the view here if needed
                    // _form.UpdateCurrentView(doc.ActiveView);

                    return Result.Succeeded;
                }

                // Create and show the form in non-modal mode
                _form = new CombinedRenderForm(doc, doc.ActiveView, settings);
                _form.Show(); // Use Show() instead of ShowDialog() for non-modal display
                _form.FormClosed += (s, e) => { _form = null; }; // Clear reference when form is closed

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                LogError($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
                TaskDialog.Show("AI Render Error", $"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private void LogError(string message)
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "RevitAIRenderer_Logs");

            if (!Directory.Exists(logDir))
            {
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch
                {
                    // Fall back to temp directory if desktop is not writable
                    logDir = Path.Combine(Path.GetTempPath(), "RevitAIRenderer_Logs");
                    Directory.CreateDirectory(logDir);
                }
            }

            string logPath = Path.Combine(logDir, "RevitAIRenderer_ErrorLog.txt");

            try
            {
                using (StreamWriter writer = File.AppendText(logPath))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}");
                }
            }
            catch
            {
                // If even logging fails, we can't do anything more
            }
        }
    }
}
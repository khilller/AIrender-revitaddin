using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;

namespace RevitAIRenderer
{
    [Transaction(TransactionMode.ReadOnly)]
    public class AiRenderCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Load settings
                var settings = AiRendererSettings.Load();

                // Create and show the combined form
                using (var form = new CombinedRenderForm(doc, doc.ActiveView, settings))
                {
                    form.ShowDialog();
                }

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
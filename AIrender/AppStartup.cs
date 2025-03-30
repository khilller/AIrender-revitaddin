using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitAIRenderer
{
    public class AppStartup : IExternalApplication
    {
        // Use a unique ID for logging
        private static readonly string LogFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "RevitAIRenderer_Logs");

        private static readonly string LogFilePath = Path.Combine(
            LogFolderPath,
            "RevitAIRenderer_Log.txt");

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Start by logging
                LogToFile("OnStartup called - Beginning add-in initialization");
                LogToFile($"Assembly location: {Assembly.GetExecutingAssembly().Location}");
                LogToFile($"Current directory: {Environment.CurrentDirectory}");

                // Get the assembly location for resources
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                LogToFile($"Using assembly path: {assemblyPath}");

                try
                {
                    // Create a ribbon panel
                    LogToFile("Creating ribbon panel");
                    RibbonPanel panel = application.CreateRibbonPanel("AI Renderer");

                    // Create a push button for the renderer
                    LogToFile("Creating ribbon button");
                    PushButtonData buttonData = new PushButtonData(
                        "AIRender",
                        "AI Render",
                        assemblyPath,
                        "RevitAIRenderer.AiRenderCommand");

                    buttonData.ToolTip = "Render the current view with AI";

                    try
                    {
                        // Try to load image resource
                        LogToFile("Attempting to load button image");
                        BitmapSource image = LoadImage("RevitAIRenderer.Resources.render_icon.png");
                        if (image != null)
                        {
                            buttonData.LargeImage = image;
                            LogToFile("Image loaded successfully");
                        }
                        else
                        {
                            LogToFile("WARNING: Image resource not found, using default button appearance");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Continue even if image loading fails
                        LogToFile($"WARNING: Failed to load image resource: {ex.Message}");
                        LogToFile("Continuing with default button appearance");
                    }

                    // Add the button to the panel
                    LogToFile("Adding button to panel");
                    PushButton button = panel.AddItem(buttonData) as PushButton;

                    LogToFile("Add-in initialization completed successfully");
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    LogToFile($"ERROR: Failed to create ribbon UI: {ex.Message}");
                    LogToFile($"Stack trace: {ex.StackTrace}");

                    // Try to show an error message to the user
                    try
                    {
                        TaskDialog.Show("AI Renderer Error",
                            $"The AI Renderer add-in failed to initialize: {ex.Message}\n\n" +
                            $"Please check the log at {LogFilePath} for details.");
                    }
                    catch
                    {
                        // If even showing dialog fails, just log it
                        LogToFile("ERROR: Could not show error dialog to user");
                    }

                    return Result.Failed;
                }
            }
            catch (Exception ex)
            {
                // Catch any errors in the main try block
                try
                {
                    LogToFile($"CRITICAL ERROR: Unhandled exception in OnStartup: {ex.Message}");
                    LogToFile($"Stack trace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        LogToFile($"Inner exception: {ex.InnerException.Message}");
                        LogToFile($"Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                }
                catch
                {
                    // If even logging fails, we can't do much more
                }

                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                LogToFile("OnShutdown called - Add-in shutting down");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Just log the error, but return success anyway to avoid affecting Revit shutdown
                try
                {
                    LogToFile($"ERROR in OnShutdown: {ex.Message}");
                }
                catch
                {
                    // If logging fails, we can't do anything else
                }

                return Result.Succeeded;
            }
        }

        private BitmapSource LoadImage(string resourcePath)
        {
            try
            {
                LogToFile($"Attempting to load resource: {resourcePath}");

                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(resourcePath);

                if (stream != null)
                {
                    LogToFile("Resource stream found");
                    return BitmapFrame.Create(stream);
                }
                else
                {
                    LogToFile($"WARNING: Resource stream not found: {resourcePath}");

                    // List available resources to help debugging
                    LogToFile("Available resources:");
                    foreach (string name in assembly.GetManifestResourceNames())
                    {
                        LogToFile($" - {name}");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"ERROR in LoadImage: {ex.Message}");
                return null;
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                // Make sure log directory exists
                if (!Directory.Exists(LogFolderPath))
                {
                    Directory.CreateDirectory(LogFolderPath);
                }

                // Append to log file with timestamp
                using (StreamWriter writer = File.AppendText(LogFilePath))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}");
                }
            }
            catch
            {
                // If logging fails, we can't do anything about it
            }
        }
    }
}
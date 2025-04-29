using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitAIRenderer
{
    public class AiRendererSettings
    {
        // Common settings
        public string SelectedProvider { get; set; } = "StabilityAI"; // Default provider

        // Provider-specific API keys
        public string StabilityApiKey { get; set; } = "";
        public string OpenAiApiKey { get; set; } = "";

        // Common settings for both providers
        public string DefaultPrompt { get; set; } = "photorealistic rendering, high quality, architectural visualization";
        public bool OfflineMode { get; set; } = false;

        // Stability AI specific settings
        public float Strength { get; set; } = 0.85f;
        public int Steps { get; set; } = 28;
        public float GuidanceScale { get; set; } = 3.5f;
        public string OutputFormat { get; set; } = "jpeg";
        public float ControlLoraStrength { get; set; } = 1.0f;
        public string StylePreset { get; set; } = "";
        public float ControlStrength { get; set; } = 0.7f;

        // OpenAI specific settings
        public string OpenAiModel { get; set; } = "gpt-image-1";

        // Network configuration properties
        public bool UseSystemProxy { get; set; } = true;
        public string ProxyAddress { get; set; } = "";
        public int ProxyPort { get; set; } = 8080;
        public bool ProxyRequiresAuthentication { get; set; } = false;
        public string ProxyUsername { get; set; } = "";
        public string ProxyPassword { get; set; } = "";

        // Timeout settings
        public int ApiTimeoutSeconds { get; set; } = 300; // 5 minutes

        // Diagnostic settings
        public bool VerboseLogging { get; set; } = false;
        public int MaxLogFileSizeMB { get; set; } = 10;

        // We can't directly serialize reference image paths as they're temporary
        // Instead, we'll just remember if the user has the feature enabled
        public bool EnableReferenceImages { get; set; } = false;

        public static AiRendererSettings Load()
        {
            string settingsPath = GetSettingsFilePath();
            LogSettings($"Attempting to load settings from: {settingsPath}");

            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    LogSettings($"Settings file found, content length: {json.Length}");
                    var settings = JsonConvert.DeserializeObject<AiRendererSettings>(json);
                    LogSettings($"Settings loaded successfully. Current provider: {settings.SelectedProvider}");
                    return settings;
                }
                catch (Exception ex)
                {
                    // Log the error but continue with default settings
                    LogError($"Failed to load settings: {ex.Message}");
                    LogSettings($"Settings load failed: {ex.Message}");

                    // Try alternative location
                    try
                    {
                        string altPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "RevitAIRenderer", "settings.json");

                        if (File.Exists(altPath))
                        {
                            LogSettings($"Trying alternative path: {altPath}");
                            string json = File.ReadAllText(altPath);
                            var settings = JsonConvert.DeserializeObject<AiRendererSettings>(json);
                            LogSettings($"Alternative settings loaded successfully");
                            return settings;
                        }
                    }
                    catch (Exception altEx)
                    {
                        LogSettings($"Alternative settings load failed: {altEx.Message}");
                    }

                    return new AiRendererSettings();
                }
            }
            else
            {
                LogSettings($"Settings file not found at: {settingsPath}");

                // Try alternative location
                try
                {
                    string altPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RevitAIRenderer", "settings.json");

                    if (File.Exists(altPath))
                    {
                        LogSettings($"Trying alternative path: {altPath}");
                        string json = File.ReadAllText(altPath);
                        var settings = JsonConvert.DeserializeObject<AiRendererSettings>(json);
                        LogSettings($"Alternative settings loaded successfully");
                        return settings;
                    }
                }
                catch (Exception altEx)
                {
                    LogSettings($"Alternative settings load failed: {altEx.Message}");
                }
            }

            LogSettings("Returning default settings");
            return new AiRendererSettings();
        }

        // Add a dedicated settings log method
        private static void LogSettings(string message)
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

            string logPath = Path.Combine(logDir, "RevitAIRenderer_SettingsLog.txt");

            try
            {
                using (System.IO.StreamWriter writer = File.AppendText(logPath))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}");
                }
            }
            catch
            {
                // If even logging fails, we can't do anything more
            }
        }

        public void Save()
        {
            string settingsPath = GetSettingsFilePath();
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            LogSettings($"Saving settings to: {settingsPath}");
            LogSettings($"API Key length: {(string.IsNullOrEmpty(StabilityApiKey) ? 0 : StabilityApiKey.Length)}");

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    LogSettings($"Created directory: {directory}");
                }

                // Write the settings file
                File.WriteAllText(settingsPath, json);
                LogSettings($"Settings saved successfully to: {settingsPath}");

                // Verify the file was written correctly
                if (File.Exists(settingsPath))
                {
                    string verifyJson = File.ReadAllText(settingsPath);
                    var verifySettings = JsonConvert.DeserializeObject<AiRendererSettings>(verifyJson);
                    LogSettings($"Verified settings. API Key length: {(string.IsNullOrEmpty(verifySettings.ApiKey) ? 0 : verifySettings.ApiKey.Length)}");
                }
            }
            catch (Exception ex)
            {
                // Log the error
                LogError($"Failed to save settings: {ex.Message}");
                LogSettings($"Failed to save settings: {ex.Message}");

                // Try alternative location
                try
                {
                    string altPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RevitAIRenderer", "settings.json");

                    // Ensure directory exists
                    string altDirectory = Path.GetDirectoryName(altPath);
                    if (!Directory.Exists(altDirectory))
                    {
                        Directory.CreateDirectory(altDirectory);
                        LogSettings($"Created alternative directory: {altDirectory}");
                    }

                    File.WriteAllText(altPath, json);
                    LogSettings($"Settings saved to alternative location: {altPath}");

                    // Let the user know we saved to an alternative location
                    System.Windows.Forms.MessageBox.Show(
                        $"Settings saved to alternative location: {altPath}",
                        "Settings Saved",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                catch (Exception altEx)
                {
                    LogSettings($"Failed to save to alternative location: {altEx.Message}");

                    // If even the alternative fails, show an error
                    System.Windows.Forms.MessageBox.Show(
                        "Failed to save settings. Your changes will not persist after closing Revit.",
                        "Settings Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitAIRenderer", "settings.json");
        }

        private static void LogError(string message)
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
                using (System.IO.StreamWriter writer = File.AppendText(logPath))
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
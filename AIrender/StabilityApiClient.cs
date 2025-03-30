using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;

namespace RevitAIRenderer
{
    public class StabilityApiClient
    {
        private readonly string _apiKey;
        private readonly bool _verboseLogging;

        public StabilityApiClient(string apiKey, bool verboseLogging = false)
        {
            _apiKey = apiKey;
            _verboseLogging = verboseLogging;

            if (_verboseLogging)
            {
                LogMessage($"Created StabilityApiClient with API key: {apiKey.Substring(0, Math.Min(4, apiKey.Length))}****");
            }
        }

        public async Task<string> ProcessRevitViewAsync(
            string imagePath,
            string prompt,
            float controlStrength = 0.7f,
            string negativePrompt = "",
            string outputFormat = "webp",
            string stylePreset = "")
        {
            LogMessage($"Processing image with Stability AI: {imagePath}");

            try
            {
                // Peprocess  the image to ensure acceptable aspect ratio for the API
                string processedImagePath = PreprocessImage(imagePath);
                // Create a unique boundary string
                string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
                // For subsequent parts
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                // For the initial part (without leading newline)
                byte[] initialBoundaryBytes = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
                byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

                // Create request with specific URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.stability.ai/v2beta/stable-image/control/structure");
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.Headers.Add("Authorization", "Bearer " + _apiKey);
                request.Accept = "image/*";
                request.Timeout = 300000; // 5 minutes

                // Log request details
                if (_verboseLogging)
                {
                    LogMessage("Request parameters:");
                    LogMessage($"  image = [binary data]");
                    LogMessage($"  prompt = {prompt}");
                    LogMessage($"  control_strength = {controlStrength}");
                    if (!string.IsNullOrEmpty(negativePrompt))
                        LogMessage($"  negative_prompt = {negativePrompt}");
                    LogMessage($"  seed = 0");
                    LogMessage($"  output_format = {outputFormat}");
                    if (!string.IsNullOrEmpty(stylePreset))
                        LogMessage($"  style_preset = {stylePreset}");

                    LogMessage($"Request URL: https://api.stability.ai/v2beta/stable-image/control/structure");
                    LogMessage($"Request Headers:");
                    LogMessage($"  Authorization: Bearer {_apiKey.Substring(0, 4)}****");
                    LogMessage($"  Accept: image/*");
                    LogMessage($"  Content-Type: {request.ContentType}");
                }

                

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    // Write the initial boundary without the preceding newline.
                    await requestStream.WriteAsync(initialBoundaryBytes, 0, initialBoundaryBytes.Length);

                    // Add prompt parameter
                    string promptHeader = "Content-Disposition: form-data; name=\"prompt\"\r\n\r\n";
                    byte[] promptHeaderBytes = Encoding.UTF8.GetBytes(promptHeader + prompt);
                    await requestStream.WriteAsync(promptHeaderBytes, 0, promptHeaderBytes.Length);

                    // Now write the boundary before the next parameter.
                    await requestStream.WriteAsync(boundarybytes, 0, boundarybytes.Length);

                    // Add control_strength parameter
                    string strengthHeader = "Content-Disposition: form-data; name=\"control_strength\"\r\n\r\n";
                    byte[] strengthHeaderBytes = Encoding.UTF8.GetBytes(strengthHeader + controlStrength.ToString());
                    await requestStream.WriteAsync(strengthHeaderBytes, 0, strengthHeaderBytes.Length);

                    // ... Continue with the rest of your parameters in the same way.

                    // When adding the image file:
                    await requestStream.WriteAsync(boundarybytes, 0, boundarybytes.Length);
                    string fileHeader = "Content-Disposition: form-data; name=\"image\"; filename=\"" +
                                        Path.GetFileName(imagePath) + "\"\r\nContent-Type: image/png\r\n\r\n";
                    byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
                    await requestStream.WriteAsync(fileHeaderBytes, 0, fileHeaderBytes.Length);

                    // Write the image data
                    using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await requestStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                    // Write the end boundary
                    await requestStream.WriteAsync(endBoundaryBytes, 0, endBoundaryBytes.Length);
                }


                LogMessage("Request sent, awaiting response...");

                try
                {
                    // Get the response asynchronously
                    WebResponse webResponse = await request.GetResponseAsync();
                    using (HttpWebResponse response = (HttpWebResponse)webResponse)
                    {
                        LogMessage($"Received response with status: {response.StatusCode}");

                        // Process successful response
                        string seed = response.Headers["seed"] ?? string.Empty;
                        string finishReason = response.Headers["finish-reason"] ?? string.Empty;
                        LogMessage($"Generation successful. Seed: {seed}, Finish Reason: {finishReason}");

                        // Save the response to a file
                        string resultDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "RevitAIRenderer", "Results");

                        if (!Directory.Exists(resultDir))
                            Directory.CreateDirectory(resultDir);

                        string resultPath = Path.Combine(resultDir,
                            $"result_{DateTime.Now:yyyyMMdd_HHmmss}.{outputFormat}");

                        LogMessage($"Saving image to: {resultPath}");

                        using (Stream responseStream = response.GetResponseStream())
                        using (FileStream fileStream = File.Create(resultPath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }

                        LogMessage($"Image saved successfully");
                        return resultPath;
                    }
                }
                catch (WebException webEx)
                {
                    if (webEx.Response != null)
                    {
                        // Read the error response
                        using (Stream responseStream = webEx.Response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            string errorContent = await reader.ReadToEndAsync();
                            LogMessage($"Error response content: {errorContent}");

                            // Add additional handling for specific error types
                            if (errorContent.Contains("aspect ratio"))
                            {
                                string detailedMessage = $"Image aspect ratio error: {errorContent}\n" +
                                    $"Original image dimensions: {GetImageDimensions(imagePath)}\n" +
                                    "The API requires aspect ratios between 1:2.5 and 2.5:1.\n" +
                                    "Try adjusting your Revit view or enabling automatic aspect ratio correction.";

                                throw new Exception(detailedMessage);
                            }

                            // Throw a generic exception for other errors
                            throw new Exception($"API Error: {errorContent}");
                        }
                    }
                    else
                    {
                        LogMessage($"Network error: {webEx.Message}");
                        throw new Exception($"Network error: {webEx.Message}", webEx);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}");
                if (ex.InnerException != null)
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                throw;
            }
        }

        public async Task<bool> TestApiConnectivityAsync()
        {
            try
            {
                // First test basic internet connectivity
                HttpWebRequest basicRequest = (HttpWebRequest)WebRequest.Create("https://www.google.com");
                basicRequest.Timeout = 5000; // 5 seconds
                using (WebResponse basicWebResponse = await basicRequest.GetResponseAsync())
                using (HttpWebResponse basicResponse = (HttpWebResponse)basicWebResponse)
                {
                    if (basicResponse.StatusCode != HttpStatusCode.OK)
                    {
                        LogMessage("Basic internet connectivity test failed");
                        return false;
                    }
                }

                // Then test Stability AI API access
                HttpWebRequest apiRequest = (HttpWebRequest)WebRequest.Create("https://api.stability.ai/v1/user/account");
                apiRequest.Headers.Add("Authorization", "Bearer " + _apiKey);
                apiRequest.Timeout = 5000; // 5 seconds

                try
                {
                    using (WebResponse apiWebResponse = await apiRequest.GetResponseAsync())
                    using (HttpWebResponse apiResponse = (HttpWebResponse)apiWebResponse)
                    {
                        bool isSuccess = apiResponse.StatusCode == HttpStatusCode.OK;
                        LogMessage($"API connectivity test result: {isSuccess}");
                        return isSuccess;
                    }
                }
                catch (WebException)
                {
                    LogMessage("API connectivity test failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"API connectivity test failed with error: {ex.Message}");
                return false;
            }
        }

        // Helper method to get image dimensions
        private string GetImageDimensions(string imagePath)
        {
            try
            {
                using (Image img = Image.FromFile(imagePath))
                {
                    return $"{img.Width}x{img.Height} (aspect ratio: {(double)img.Width / img.Height:F2})";
                }
            }
            catch
            {
                return "Unknown dimensions";
            }
        }

        public string PreprocessImage(string imagePath)
        {
            try
            {
                // Load the image
                using (Image originalImage = Image.FromFile(imagePath))
                {
                    // Check aspect ratio
                    double aspectRatio = (double)originalImage.Width / originalImage.Height;
                    if (aspectRatio >= 0.4 && aspectRatio <= 2.5)
                    {
                        // Image is already within the acceptable range
                        return imagePath;
                    }

                    // Calculate new dimensions
                    int newWidth = originalImage.Width;
                    int newHeight = originalImage.Height;

                    if (aspectRatio > 2.5)
                    {
                        // Too wide
                        newWidth = (int)(originalImage.Height * 2.5);
                    }
                    else if (aspectRatio < 0.4)
                    {
                        // Too tall
                        newHeight = (int)(originalImage.Width / 0.4);
                    }

                    // Create a new bitmap with the corrected dimensions
                    string outputPath = Path.Combine(
                        Path.GetDirectoryName(imagePath),
                        Path.GetFileNameWithoutExtension(imagePath) + "_corrected" + Path.GetExtension(imagePath));

                    using (Bitmap croppedImage = new Bitmap(newWidth, newHeight))
                    {
                        using (Graphics g = Graphics.FromImage(croppedImage))
                        {
                            // Center the crop
                            int x = (originalImage.Width - newWidth) / 2;
                            int y = (originalImage.Height - newHeight) / 2;

                            // Draw the cropped portion
                            g.DrawImage(originalImage,
                                        new Rectangle(0, 0, newWidth, newHeight),
                                        new Rectangle(x, y, newWidth, newHeight),
                                        GraphicsUnit.Pixel);
                        }

                        // Save the cropped image
                        croppedImage.Save(outputPath);
                    }

                    LogMessage($"Preprocessed image from {originalImage.Width}x{originalImage.Height} to {newWidth}x{newHeight}");
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error preprocessing image: {ex.Message}");
                return imagePath; // Return original on error
            }
        }

        private void LogMessage(string message)
        {
            Debug.WriteLine($"StabilityApiClient: {message}");

            if (_verboseLogging)
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

                string logPath = Path.Combine(logDir, "RevitAIRenderer_APILog.txt");

                try
                {
                    using (StreamWriter writer = File.AppendText(logPath))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}");
                    }
                }
                catch
                {
                    // If logging fails, continue without logging
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json;

namespace RevitAIRenderer
{
    public class OpenAiApiClient
    {
        private readonly string _apiKey;
        private readonly bool _verboseLogging;

        public OpenAiApiClient(string apiKey, bool verboseLogging = false)
        {
            _apiKey = apiKey;
            _verboseLogging = verboseLogging;

            if (_verboseLogging)
            {
                LogMessage($"Created OpenAiApiClient with API key: {apiKey.Substring(0, Math.Min(4, apiKey.Length))}****");
            }
        }

        public async Task<string> ProcessRevitViewWithReferencesAsync(
            string mainImagePath,
            List<string> referenceImagePaths,
            string prompt,
            string model = "gpt-image-1")
        {
            LogMessage($"Processing image with OpenAI: {mainImagePath} with {referenceImagePaths.Count} reference images");

            try
            {
                // Create a unique boundary string
                string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                byte[] initialBoundaryBytes = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
                byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

                // Create request to OpenAI API
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.openai.com/v1/images/edits");
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.Headers.Add("Authorization", "Bearer " + _apiKey);
                request.Accept = "application/json";
                request.Timeout = 300000; // 5 minutes

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    // Write initial boundary
                    await requestStream.WriteAsync(initialBoundaryBytes, 0, initialBoundaryBytes.Length);

                    // Add model parameter
                    string modelHeader = "Content-Disposition: form-data; name=\"model\"\r\n\r\n";
                    byte[] modelHeaderBytes = Encoding.UTF8.GetBytes(modelHeader + model);
                    await requestStream.WriteAsync(modelHeaderBytes, 0, modelHeaderBytes.Length);

                    // Write boundary for next parameter
                    await requestStream.WriteAsync(boundarybytes, 0, boundarybytes.Length);

                    // Add prompt parameter
                    string promptHeader = "Content-Disposition: form-data; name=\"prompt\"\r\n\r\n";
                    byte[] promptHeaderBytes = Encoding.UTF8.GetBytes(promptHeader + prompt);
                    await requestStream.WriteAsync(promptHeaderBytes, 0, promptHeaderBytes.Length);

                    // Add the main image
                    await AddImageToRequest(requestStream, boundarybytes, mainImagePath, "image[]");

                    // Add each reference image
                    foreach (string refImagePath in referenceImagePaths)
                    {
                        await AddImageToRequest(requestStream, boundarybytes, refImagePath, "image[]");
                    }

                    // Write the end boundary
                    await requestStream.WriteAsync(endBoundaryBytes, 0, endBoundaryBytes.Length);
                }

                LogMessage("Request sent to OpenAI, awaiting response...");

                try
                {
                    // Get the response
                    using (WebResponse response = await request.GetResponseAsync())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        // Read the response content
                        string responseJson = await reader.ReadToEndAsync();
                        LogMessage("Response received");

                        // Parse the JSON response to get the image data
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseJson);
                        string base64Image = jsonResponse.data[0].b64_json;

                        // Create directory for saving the result
                        string resultDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "RevitAIRenderer", "Results");

                        if (!Directory.Exists(resultDir))
                            Directory.CreateDirectory(resultDir);

                        // Save the base64 image to a file
                        string resultPath = Path.Combine(resultDir,
                            $"openai_result_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                        byte[] imageBytes = Convert.FromBase64String(base64Image);
                        File.WriteAllBytes(resultPath, imageBytes);

                        LogMessage($"Image saved to: {resultPath}");
                        return resultPath;
                    }
                }
                catch (WebException webEx)
                {
                    if (webEx.Response != null)
                    {
                        using (Stream responseStream = webEx.Response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            string errorContent = await reader.ReadToEndAsync();
                            LogMessage($"Error response content: {errorContent}");
                            throw new Exception($"OpenAI API Error: {errorContent}");
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

        private async Task AddImageToRequest(Stream requestStream, byte[] boundarybytes, string imagePath, string paramName)
        {
            await requestStream.WriteAsync(boundarybytes, 0, boundarybytes.Length);

            string fileHeader = "Content-Disposition: form-data; name=\"" + paramName + "\"; filename=\"" +
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

                // Then test OpenAI API access
                HttpWebRequest apiRequest = (HttpWebRequest)WebRequest.Create("https://api.openai.com/v1/models");
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

        private void LogMessage(string message)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAiApiClient: {message}");

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

                string logPath = Path.Combine(logDir, "RevitAIRenderer_OpenAI_APILog.txt");

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
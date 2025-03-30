using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitAIRenderer
{
    public class FalApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly bool _verboseLogging;
        private readonly int _timeoutSeconds;

        public FalApiClient(string apiKey, bool verboseLogging = false, int timeoutSeconds = 300)
        {
            _apiKey = apiKey;
            _verboseLogging = verboseLogging;
            _timeoutSeconds = timeoutSeconds;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        }

        private void LogMessage(string message)
        {
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

        // Download file using WebClient
        private bool DownloadFileWithWebClient(string url, string destinationPath, int retryCount = 3)
        {
            LogMessage($"DownloadFileWithWebClient: {url} to {destinationPath}");

            int attempts = 0;
            int delayMs = 1000;

            while (attempts < retryCount)
            {
                attempts++;
                LogMessage($"Download attempt {attempts}/{retryCount}");

                try
                {
                    // Create directory if it doesn't exist
                    string directory = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (WebClient client = new WebClient())
                    {
                        // Set UserAgent to avoid any blocking
                        client.Headers.Add("User-Agent", "RevitAIRenderer/1.0");

                        // Download the file synchronously
                        client.DownloadFile(url, destinationPath);

                        // Check if file exists and has content
                        if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0)
                        {
                            LogMessage($"File downloaded successfully ({new FileInfo(destinationPath).Length} bytes)");
                            return true;
                        }
                        else
                        {
                            LogMessage("File download failed: File is empty or doesn't exist");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Download attempt {attempts} failed: {ex.Message}");

                    if (attempts < retryCount)
                    {
                        LogMessage($"Waiting {delayMs}ms before retry");
                        System.Threading.Thread.Sleep(delayMs);
                        delayMs *= 2; // Exponential backoff
                    }
                }
            }

            LogMessage($"All {retryCount} download attempts failed");
            return false;
        }

        // Download using HttpWebRequest
        private bool DownloadFileWithWebRequest(string url, string destinationPath)
        {
            LogMessage($"DownloadFileWithWebRequest: {url} to {destinationPath}");

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "RevitAIRenderer/1.0";
                request.Timeout = 30000; // 30 seconds

                // Get response
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fileStream = File.Create(destinationPath))
                {
                    // Copy stream
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }

                // Verify file
                if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0)
                {
                    LogMessage($"File downloaded successfully with WebRequest ({new FileInfo(destinationPath).Length} bytes)");
                    return true;
                }
                else
                {
                    LogMessage("File download failed with WebRequest: File is empty or doesn't exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"WebRequest download failed: {ex.Message}");
                return false;
            }
        }

        // Convert image file to Base64 data URI
        private string ConvertImageToBase64DataUri(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64String = Convert.ToBase64String(imageBytes);
            // Determine the MIME type based on file extension
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            string mimeType = "image/jpeg";
            if (extension == ".png")
            {
                mimeType = "image/png";
            }
            // Return data URI format
            return $"data:{mimeType};base64,{base64String}";
        }

        // Process a Revit view with updated API model using Base64 image input
        public async Task<string> ProcessRevitView(string imagePath, string prompt,
            float strength = 0.85f, int numSteps = 28, float guidanceScale = 3.5f,
            string outputFormat = "jpeg", float controlLoraStrength = 1.0f)
        {
            try
            {
                // Log the operation with detailed parameters
                LogMessage($"Starting ProcessRevitView with:");
                LogMessage($"- Image path: {imagePath}");
                LogMessage($"- Prompt: {prompt}");
                LogMessage($"- Strength: {strength}");
                LogMessage($"- Steps: {numSteps}");
                LogMessage($"- Guidance: {guidanceScale}");
                LogMessage($"- Output format: {outputFormat}");
                LogMessage($"- LoRA strength: {controlLoraStrength}");

                // Convert the image file to a Base64 data URI
                string base64Image = ConvertImageToBase64DataUri(imagePath);
                LogMessage("Converted image to Base64 data URI");

                // Use Base64 image data for both image_url and control_lora_image_url
                // Set sync_mode to false to use the async approach with polling
                bool syncMode = false;

                // Create request body following API schema
                var requestBody = new
                {
                    prompt = prompt,
                    image_url = base64Image,
                    control_lora_image_url = base64Image,
                    strength = strength,
                    num_inference_steps = numSteps,
                    guidance_scale = guidanceScale,
                    sync_mode = syncMode,
                    num_images = 1,
                    output_format = outputFormat,
                    control_lora_strength = controlLoraStrength,
                    enable_safety_checker = true
                };

                // Convert to JSON
                string jsonBody = JsonConvert.SerializeObject(requestBody);
                LogMessage("Request JSON prepared (not logging full content due to size)");

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                LogMessage($"Request body prepared with Content-Type: {content.Headers.ContentType}");

                // Use the endpoint specified by the API requirement
                string apiEndpoint = "https://queue.fal.run/fal-ai/flux-control-lora-canny/image-to-image";
                LogMessage($"Calling API endpoint: {apiEndpoint}");

                // Send request
                var response = await _httpClient.PostAsync(apiEndpoint, content);
                LogMessage($"Received response with status: {response.StatusCode}");

                // Get the response content even if it's an error
                string responseBody = await response.Content.ReadAsStringAsync();

                // Log a truncated version of the response
                string truncatedResponse = responseBody.Length > 500
                    ? responseBody.Substring(0, 500) + "..."
                    : responseBody;
                LogMessage($"Response body (truncated): {truncatedResponse}");

                // Check for success
                response.EnsureSuccessStatusCode();

                // Parse the response
                var result = JsonConvert.DeserializeObject<JObject>(responseBody);
                LogMessage("Parsed JSON response");

                string resultUrl = null;
                string requestId = null;

                // Check if we have a result with images (sync_mode = true)
                if (result["images"] != null && result["images"].HasValues)
                {
                    LogMessage("Found images in response");
                    resultUrl = result["images"][0]["url"].ToString();
                    LogMessage($"Image URL: {resultUrl}");
                }
                // If in async mode, we need to get the request ID and poll for the result
                else if (result["request_id"] != null)
                {
                    LogMessage("Found request_id, polling for result");
                    requestId = result["request_id"].ToString();

                    // Increase polling timeout and attempts
                    resultUrl = await PollForResult(requestId, 20, 30000); // 20 retries, up to 30 seconds per retry
                    LogMessage($"Polling complete, image URL: {resultUrl}");
                }
                else
                {
                    LogMessage("No images or request_id found in response");
                    throw new Exception("Failed to get image URL from response: " + responseBody);
                }

                // Validate URL has correct scheme
                if (string.IsNullOrEmpty(resultUrl) ||
                    (!resultUrl.StartsWith("http://") && !resultUrl.StartsWith("https://")))
                {
                    LogMessage($"Invalid URL scheme: {resultUrl}");
                    throw new Exception($"Invalid URL returned from API: {resultUrl}");
                }

                // Determine file extension from output_format
                string fileExtension = outputFormat.ToLower() == "jpeg" ? ".jpg" : $".{outputFormat.ToLower()}";

                // Save to a temporary file
                string tempDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RevitAIRenderer", "Results");

                string resultPath = Path.Combine(tempDir, $"result_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}");

                // Try to download the file using multiple methods
                LogMessage($"Attempting to download image from {resultUrl} to {resultPath}");

                // Try WebClient first
                if (DownloadFileWithWebClient(resultUrl, resultPath))
                {
                    LogMessage($"WebClient download succeeded: {resultPath}");
                    return resultPath;
                }

                // Try WebRequest as backup
                string altPath = Path.Combine(tempDir, $"result_alt_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}");
                if (DownloadFileWithWebRequest(resultUrl, altPath))
                {
                    LogMessage($"WebRequest download succeeded: {altPath}");
                    return altPath;
                }

                // Try direct URI download as last resort
                string lastResortPath = Path.Combine(tempDir, $"result_last_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}");
                try
                {
                    LogMessage($"Trying direct file copying method to {lastResortPath}");

                    // Create a URI object
                    Uri uri = new Uri(resultUrl);

                    // Copy file directly
                    using (WebClient finalClient = new WebClient())
                    {
                        finalClient.Headers.Add("User-Agent", "Mozilla/5.0");
                        finalClient.DownloadFile(uri, lastResortPath);
                    }

                    if (File.Exists(lastResortPath) && new FileInfo(lastResortPath).Length > 0)
                    {
                        LogMessage($"Direct copy succeeded: {lastResortPath}");
                        return lastResortPath;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Direct copy failed: {ex.Message}");
                }

                throw new Exception("Failed to download result image after trying multiple methods. The image was successfully generated but could not be downloaded.");
            }
            catch (HttpRequestException ex)
            {
                LogMessage($"HTTP Request Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                }

                throw new Exception($"Error communicating with FAL.ai API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                LogMessage($"Request timed out: {ex.Message}");
                throw new Exception($"Request to FAL.ai API timed out after {_timeoutSeconds} seconds. Try reducing image size or complexity, or increasing the timeout in settings.", ex);
            }
            catch (Exception ex)
            {
                LogMessage($"General Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                }

                throw new Exception($"Error processing Revit view: {ex.Message}", ex);
            }
        }

        // Poll for result of async request
        private async Task<string> PollForResult(string requestId, int maxRetries = 10, int maxDelayMs = 10000)
        {
            LogMessage($"PollForResult started for request ID: {requestId} with maxRetries={maxRetries}, maxDelayMs={maxDelayMs}");

            int retryCount = 0;
            int delay = 1000; // Start with 1 second

            while (retryCount < maxRetries)
            {
                LogMessage($"Poll attempt {retryCount + 1}/{maxRetries}, waiting {delay}ms");
                await Task.Delay(delay);

                try
                {
                    string statusUrl = $"https://queue.fal.run/fal-ai/flux-control-lora-canny/requests/{requestId}/status";
                    LogMessage($"Checking status: {statusUrl}");

                    var statusResponse = await _httpClient.GetAsync(statusUrl);
                    LogMessage($"Status response: {statusResponse.StatusCode}");

                    string statusResponseBody = await statusResponse.Content.ReadAsStringAsync();
                    LogMessage($"Status response body: {statusResponseBody}");

                    if (statusResponse.IsSuccessStatusCode)
                    {
                        var statusResult = JsonConvert.DeserializeObject<JObject>(statusResponseBody);
                        string status = statusResult["status"]?.ToString();
                        LogMessage($"Request status: {status}");

                        if (status == "COMPLETED")
                        {
                            string resultUrl = $"https://queue.fal.run/fal-ai/flux-control-lora-canny/requests/{requestId}";
                            LogMessage($"Getting result: {resultUrl}");

                            var resultResponse = await _httpClient.GetAsync(resultUrl);
                            LogMessage($"Result response: {resultResponse.StatusCode}");

                            string resultResponseBody = await resultResponse.Content.ReadAsStringAsync();
                            LogMessage($"Result response body: {resultResponseBody}");

                            if (resultResponse.IsSuccessStatusCode)
                            {
                                var resultObj = JsonConvert.DeserializeObject<JObject>(resultResponseBody);
                                if (resultObj["images"] != null && resultObj["images"].HasValues)
                                {
                                    string imageUrl = resultObj["images"][0]["url"].ToString();
                                    LogMessage($"Found image URL: {imageUrl}");
                                    return imageUrl;
                                }
                                else
                                {
                                    LogMessage("No images found in completed result");
                                }
                            }
                            break;
                        }
                        else if (status == "FAILED")
                        {
                            string errorMessage = statusResult["error"]?.ToString() ?? "Unknown error";
                            LogMessage($"Request failed: {errorMessage}");
                            throw new Exception($"API request failed: {errorMessage}");
                        }
                        else if (status == "PENDING" || status == "IN_PROGRESS")
                        {
                            LogMessage($"Request still {status}, continuing to poll");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error during polling: {ex.Message}");
                }

                delay = Math.Min(delay * 2, maxDelayMs);
                retryCount++;
            }

            LogMessage("Timed out waiting for image processing result");
            throw new Exception("Timed out waiting for image processing result. The FAL.ai service might be overloaded or having issues. Please try again later.");
        }

        // Wrapper for ProcessRevitView to match what's being called in AiRenderCommand
        public async Task<string> ProcessRevitViewAsync(string imagePath, string prompt,
            float strength = 0.85f, int numSteps = 28, float guidanceScale = 3.5f,
            string outputFormat = "jpeg", float controlLoraStrength = 1.0f)
        {
            return await ProcessRevitView(imagePath, prompt, strength, numSteps, guidanceScale, outputFormat, controlLoraStrength);
        }

        // Test API connectivity
        public async Task<bool> TestApiConnectivityAsync()
        {
            try
            {
                using (var basicClient = new HttpClient())
                {
                    basicClient.Timeout = TimeSpan.FromSeconds(5);
                    LogMessage("Testing basic internet connectivity (Google)...");
                    var googleResponse = await basicClient.GetAsync("https://www.google.com");

                    if (!googleResponse.IsSuccessStatusCode)
                    {
                        LogMessage($"Basic internet connectivity test failed with status: {googleResponse.StatusCode}");
                        return false;
                    }
                    LogMessage("Basic internet connectivity test passed");
                }

                using (var domainClient = new HttpClient())
                {
                    domainClient.Timeout = TimeSpan.FromSeconds(5);
                    LogMessage("Testing connection to FAL.ai domain...");
                    var response = await domainClient.GetAsync("https://fal.ai");

                    if (!response.IsSuccessStatusCode)
                    {
                        LogMessage($"Could not reach FAL.ai domain. Status: {response.StatusCode}");
                        return false;
                    }
                    LogMessage("Successfully reached FAL.ai domain");
                }

                if (string.IsNullOrEmpty(_apiKey))
                {
                    LogMessage("API key is not provided. Skipping API authentication test.");
                    return false;
                }

                try
                {
                    LogMessage("Testing API authentication with storage endpoint...");

                    using (var apiClient = new HttpClient())
                    {
                        apiClient.Timeout = TimeSpan.FromSeconds(10);
                        apiClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");

                        var request = new HttpRequestMessage(HttpMethod.Options, "https://api.fal.ai/storage");
                        var response = await apiClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            LogMessage("API authentication test passed");
                            return true;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            LogMessage($"API authentication test failed: {response.StatusCode} - {responseContent}");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error testing API authentication: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"API connectivity test failed: {ex.Message}");
                return false;
            }
        }
    }
}

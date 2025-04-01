# Revit AI Renderer

An advanced add-in for Autodesk Revit that uses AI-powered image generation to transform standard Revit views into photorealistic architectural visualizations.

![Revit AI Renderer](https://raw.githubusercontent.com/username/RevitAIRenderer/main/docs/images/sample-render.png)

## 🚀 Features

- **AI-Powered Rendering**: Transform standard Revit views into photorealistic or stylized renders
- **Real-time Preview**: View original and rendered images side-by-side
- **Custom Prompting**: Fine-tune the AI generation with detailed prompts
- **Style Presets**: Choose from multiple pre-configured rendering styles
- **High-Quality Export**: Save renders in various formats (JPEG, PNG, WebP)
- **Workflow Integration**: Seamlessly integrates into your Revit environment

## 📋 Requirements

- Autodesk Revit 2021 or newer
- .NET Framework 4.8
- Stability AI API key (for AI rendering capabilities)
- Internet connection (for API communication)

## 🔧 Installation

### Method 1: Using the Installer

1. Download the latest installer from the [Releases](https://github.com/username/RevitAIRenderer/releases) page
2. Close all running instances of Revit
3. Run the installer and follow the on-screen instructions
4. Launch Revit - the add-in should appear in a new "AI Renderer" panel in the ribbon

### Method 2: Manual Installation

1. Download the latest release ZIP from the [Releases](https://github.com/username/RevitAIRenderer/releases) page
2. Extract all files to a folder of your choice
3. Copy the `RevitAIRenderer.addin` file to:
   - `C:\ProgramData\Autodesk\Revit\Addins\[Version]` (for all users)
   - or `%APPDATA%\Autodesk\Revit\Addins\[Version]` (for current user only)
4. Copy the remaining files to the location specified in the `.addin` file
5. Start Revit

## 📝 Getting Started

### Setting Up Your API Key

1. Obtain an API key from [Stability AI](https://stability.ai/)
2. In Revit, click on the "AI Render" button in the "AI Renderer" panel
3. Enter your API key in the settings panel
4. Click "Save Settings" to store your key securely

### Creating Your First Render

1. Open or create a 3D view in Revit
2. Adjust the view to your desired composition
3. Click the "AI Render" button in the ribbon
4. Enter a prompt describing the style you want (e.g., "photorealistic rendering, warm sunset lighting, architectural visualization")
5. Customize rendering parameters if needed:
   - Control Strength: Adjusts how closely the AI follows your Revit geometry
   - Output Format: Choose between JPEG, PNG, or WebP
   - Style Preset: Select a predefined rendering style
   - High-Quality Image Capture: Enable for more detailed input/output
6. Click "Render View" and wait for the process to complete
7. View the result in the "AI Rendered" tab

### Saving and Exporting

- Use the "Save Image" button to export your rendered image to your desired location
- Switch between original and rendered views using the tabs
- Use "Sync View" to capture updates to your Revit model

## ⚙️ Advanced Usage

### Effective Prompting

The AI responds to detailed descriptions. Here are some effective prompt components:

- Lighting conditions: "warm sunset lighting", "cool morning light", "dramatic shadows"
- Materials: "exposed concrete walls", "wooden floors", "glass railings"
- Style: "minimalist", "modern", "traditional", "scandinavian"
- Mood: "cozy", "bright and airy", "professional", "dramatic"

Example: "Photorealistic rendering with warm afternoon light, emphasizing natural materials, minimalist style with indoor plants, high-end architectural photography style"

### Negative Prompts

Use negative prompts to specify what you don't want in the image:
- "no people, no text, no distortion, no blur"

### Style Presets

The add-in offers several built-in style presets:
- **3D-Model**: Renders that look like 3D models
- **Analog-Film**: Film-like aesthetic
- **Cinematic**: Movie-quality lighting and composition
- **Photographic**: Realistic photography look
- And more...

### Troubleshooting

If you encounter network or API issues:
1. Check your internet connection
2. Verify your API key is valid and properly entered
3. Try the "Test API Connection" button in settings
4. Check firewall or corporate network restrictions
5. Review logs at `Desktop\RevitAIRenderer_Logs`

## 📊 Technical Details

### Technology Stack

- C# .NET Framework 4.8
- Revit API
- Stability AI API for AI rendering (control/structure endpoint)
- Windows Forms for UI

### Key Parameters

- **Control Strength**: Determines how closely the AI adheres to your Revit geometry (0.1-1.0)
- **Output Format**: File format for the rendered image (JPEG, PNG, WebP)
- **Style Preset**: Predefined styles for different aesthetic results

### Log Files

Log files are stored in:
- `%USERPROFILE%\Desktop\RevitAIRenderer_Logs`
- Or alternatively: `%TEMP%\RevitAIRenderer_Logs`

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 🙏 Acknowledgements

- Stability AI for providing the image generation API
- Autodesk for the Revit API
- All contributors who have helped improve this add-in

---

⭐ If you find this add-in useful, please consider giving it a star on GitHub! ⭐

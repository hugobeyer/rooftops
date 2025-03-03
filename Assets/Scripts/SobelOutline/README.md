# Sobel Outline Effect for Unity URP

This package provides a post-processing outline effect for Unity's Universal Render Pipeline (URP). It uses a Sobel filter to detect edges based on depth and color differences in the scene.

## Features

- Edge detection based on depth and color differences
- Adjustable outline color, thickness, and threshold
- Sensitivity controls for depth and color edge detection
- Debug mode to visualize detected edges
- Works with both lit and unlit scenes
- Compatible with Unity URP

## Setup Instructions

### Quick Setup

1. Go to `Tools > Sobel Outline > Setup Fixed Version` in the Unity menu
2. This will:
   - Create a material with optimized settings
   - Add the Sobel Outline renderer feature to your URP renderer
   - Create a helper GameObject in your scene
   - Check and enable required URP settings (Depth Texture and Opaque Texture)

### Manual Setup

If you prefer to set up manually:

1. Make sure your URP asset has Depth Texture and Opaque Texture enabled
2. Create a material using the "Custom/SobelOutline" shader
3. Add the SobelOutlineFeature to your URP renderer
4. Add a SobelOutlineSetupHelper component to a GameObject in your scene

## Usage

### Adjusting Settings

You can adjust the outline settings in two ways:

1. **In the Inspector**: Select the "Sobel Outline Helper" GameObject and adjust settings in the inspector
2. **In the URP Renderer**: Find the Sobel Outline feature in your URP renderer asset and adjust settings there

### Key Parameters

- **Outline Color**: The color of the outline
- **Outline Thickness**: Controls how thick the outline appears (1-10)
- **Outline Threshold**: Determines the minimum difference required to draw an outline (0-1)
- **Depth Sensitivity**: How much depth differences affect the outline (0-10)
- **Color Sensitivity**: How much color differences affect the outline (0-10)
- **Debug Mode**: When enabled, shows edges as white lines on a black background

### Debug Mode

To toggle debug mode:

1. Go to `Tools > Sobel Outline > Toggle Debug Mode` in the Unity menu
2. This will switch between normal outline mode and debug visualization mode
3. Debug mode is useful for tuning the effect and seeing exactly which edges are being detected

## Troubleshooting

If the outline effect isn't visible:

1. Make sure your URP asset has Depth Texture and Opaque Texture enabled
2. Try enabling debug mode to see if edges are being detected
3. Increase the depth and color sensitivity values
4. Decrease the outline threshold value
5. Make sure your scene has objects at different depths
6. Check the console for any error messages

## Technical Details

The Sobel Outline effect works by:

1. Sampling the depth buffer and color buffer at each pixel and its neighbors
2. Calculating differences in depth and color between neighboring pixels
3. Applying a threshold to determine if an edge exists
4. Rendering the outline by blending the original color with the outline color

The effect is implemented as a URP Renderer Feature that adds a custom render pass to the rendering pipeline.

## Requirements

- Unity 2021.3 or newer
- Universal Render Pipeline (URP) package

## License

This package is provided as-is under the MIT License. 
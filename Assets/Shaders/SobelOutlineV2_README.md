# Sobel Outline V2 Effect for Unity URP

A high-quality edge detection outline effect for Unity's Universal Render Pipeline that uses both depth and color information to create clean, customizable outlines.

## Features

- Edge detection based on both depth and color differences
- Adjustable outline color, thickness, and threshold
- Sensitivity controls for both depth and color edge detection
- Debug mode to visualize detected edges
- Easy setup through Unity Editor menu
- Fully compatible with Unity URP

## Quick Setup

1. Go to `Tools > Sobel Outline V2 > Quick Setup` in the Unity menu
2. The setup will automatically:
   - Create a material with the SobelOutlineV2 shader
   - Add the SobelOutlineRendererFeature to your URP Renderer
   - Enable depth texture in URP settings

## Manual Setup

If you prefer to set things up manually:

1. Go to `Tools > Sobel Outline V2 > Setup` to open the setup window
2. Click "Setup Sobel Outline V2" button
3. Verify that:
   - A material named "SobelOutlineV2Material" has been created in the Materials folder
   - The SobelOutlineRendererFeature has been added to your URP Renderer
   - Depth texture is enabled in your URP settings

## Usage

Once set up, the outline effect will be applied to your scene automatically. You can adjust the effect by modifying the SobelOutlineV2Material properties:

1. Select the SobelOutlineV2Material in your project
2. Adjust the following parameters in the Inspector:
   - Outline Color: The color of the outline
   - Outline Thickness: How thick the outline appears (1-10)
   - Outline Threshold: The minimum difference required to create an outline (0-1)
   - Depth Sensitivity: How sensitive the effect is to depth differences (0-10)
   - Color Sensitivity: How sensitive the effect is to color differences (0-10)
   - Debug Mode: Toggle to visualize edges (white) on a black background

## Debug Mode

If you're having trouble seeing the outline effect or want to fine-tune the edge detection:

1. Go to `Tools > Sobel Outline V2 > Setup` to open the setup window
2. Click "Toggle Debug Mode" button
3. In debug mode, detected edges will appear as white lines on a black background
4. Use this to adjust your threshold and sensitivity settings
5. Click "Toggle Debug Mode" again to return to normal rendering

## Troubleshooting

If you don't see any outlines:

1. Make sure depth texture is enabled in your URP settings
2. Try decreasing the Outline Threshold value
3. Try increasing the Depth Sensitivity and Color Sensitivity values
4. Use Debug Mode to see if any edges are being detected
5. Ensure the SobelOutlineRendererFeature is enabled in your URP Renderer

## Technical Details

The Sobel Outline V2 effect works by:

1. Sampling the depth and color buffers from the camera
2. Applying the Sobel operator to detect edges in both depth and color
3. Combining the results to create a comprehensive edge detection
4. Applying a threshold to determine which edges to display
5. Rendering the outlines by blending the outline color with the original scene

The effect is implemented as a URP Renderer Feature, which means it integrates seamlessly with Unity's rendering pipeline and works with all URP-compatible shaders and effects.

## Requirements

- Unity 2020.3 or newer
- Universal Render Pipeline (URP) package
- Depth texture enabled in URP settings

## License

This Sobel Outline V2 effect is provided under the MIT License. Feel free to use it in your projects, both personal and commercial. 
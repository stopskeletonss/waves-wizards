
using UnityEngine;
using UnityEditor;
using System.IO;

public class StaffIconExporter
{
    [MenuItem("Tools/Staff Icons/Export Selected Camera")]
    private static void ExportIcon()
    {
        // Get the selected camera.
        Camera cam = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Camera>()
            : null;

        if (cam == null || cam.targetTexture == null)
        {
            Debug.LogError("Select a camera with a Render Texture assigned.");
            return;
        }

        RenderTexture renderTexture = cam.targetTexture;

        // Save the current rendering state.
        RenderTexture previousTexture = RenderTexture.active;
        bool previousSRGBWrite = GL.sRGBWrite;

        Texture2D image = null;

        try
        {
            // Match the sRGB setting of the Render Texture.
            GL.sRGBWrite = renderTexture.sRGB;

            // Render the camera.
            cam.Render();

            // Read the Render Texture.
            RenderTexture.active = renderTexture;

            image = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGBA32,
                false,
                false
            );

            image.ReadPixels(
                new Rect(0, 0, renderTexture.width, renderTexture.height),
                0,
                0,
                false
            );

            image.Apply(false, false);

            // Create the export folder if necessary.
            string folder = "Assets/Textures/ScreenUI/StaffIcons/pngs";

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Save the PNG.
            string path = folder + "/StaffIcon.png";

            File.WriteAllBytes(path, image.EncodeToPNG());

            AssetDatabase.Refresh();

            Debug.Log("Staff icon exported to: " + path);
        }
        finally
        {
            // Restore the previous rendering state.
            RenderTexture.active = previousTexture;
            GL.sRGBWrite = previousSRGBWrite;

            if (image != null)
                Object.DestroyImmediate(image);
        }
    }
}

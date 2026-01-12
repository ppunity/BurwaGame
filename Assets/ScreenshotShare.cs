using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

public class ScreenshotShare : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShareImage(string base64Image, string filename);
#endif

    // Call this (e.g. from a UI button) to capture & share
    public void ShareScreenshot()
    {
        StartCoroutine(CaptureAndShare());
    }

    private IEnumerator CaptureAndShare()
    {
        // Wait for the frame to finish rendering
        yield return new WaitForEndOfFrame();

        // Capture the screen as a Texture2D
        // (Unity 2018.1+ has ScreenCapture.CaptureScreenshotAsTexture)
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        // Optional: resize or downscale here if you want smaller images

        // Encode to PNG
        byte[] png = tex.EncodeToPNG();

        // Clean up texture
        Destroy(tex);

        // Convert to base64 for sending to JS
        string base64 = System.Convert.ToBase64String(png);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Call JS function in WebGL build
        ShareImage(base64, "screenshot.png");
#else
        // In the Editor / non-WebGL: just log info
        Debug.Log("Screenshot captured. Base64 length: " + base64.Length);
#endif
    }
}
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TileScanner : MonoBehaviour
{
    public Camera cam;

    public Transform topLeftMarker;
    public Transform bottomRightMarker;

    public int gridWidth = 4;
    public int gridHeight = 4;

    public int tileResolution = 1024;

    public bool topDown = true;

    public string savePath = "Assets/TileScan.png";

    public void ScanNow()
    {
#if UNITY_EDITOR
        if (cam == null || topLeftMarker == null || bottomRightMarker == null)
        {
            Debug.LogError("Assign camera and markers.");
            return;
        }

        float totalW = Mathf.Abs(topLeftMarker.position.x - bottomRightMarker.position.x);
        float totalH = Mathf.Abs(topLeftMarker.position.z - bottomRightMarker.position.z);

        float tileWorldW = totalW / gridWidth;
        float tileWorldH = totalH / gridHeight;

        // Force perfect square camera tile capture
        cam.orthographic = true;
        cam.aspect = 1f;
        cam.orthographicSize = tileWorldH * 0.5f;

        Texture2D finalTex = new Texture2D(
            tileResolution * gridWidth,
            tileResolution * gridHeight,
            TextureFormat.RGBA32,
            false
        );

        RenderTexture rt = new RenderTexture(
            tileResolution,
            tileResolution,
            24,
            RenderTextureFormat.ARGB32
        );

        Vector3 start = new Vector3(
            topLeftMarker.position.x + tileWorldW * 0.5f,
            cam.transform.position.y,
            topLeftMarker.position.z - tileWorldH * 0.5f
        );

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector3 pos = new Vector3(
                    start.x + x * tileWorldW,
                    cam.transform.position.y,
                    start.z - y * tileWorldH
                );

                cam.transform.position = pos;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;

                Texture2D tile = new Texture2D(tileResolution, tileResolution, TextureFormat.RGBA32, false);
                tile.ReadPixels(new Rect(0, 0, tileResolution, tileResolution), 0, 0);
                tile.Apply();

                int px = x * tileResolution;
                int py = (gridHeight - 1 - y) * tileResolution;

                finalTex.SetPixels(px, py, tileResolution, tileResolution, tile.GetPixels());

                Object.DestroyImmediate(tile);
            }
        }

        finalTex.Apply();

        byte[] bytes = finalTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(savePath, bytes);

        AssetDatabase.Refresh();

        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();

        Debug.Log("Tile scan saved to " + savePath);
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileScanner))]
public class TileScannerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TileScanner scanner = (TileScanner)target;

        if (GUILayout.Button("Scan Now"))
        {
            scanner.ScanNow();
        }
    }
}
#endif

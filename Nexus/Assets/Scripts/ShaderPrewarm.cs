using UnityEngine;
using System.Collections;

public class ShaderPrewarm : MonoBehaviour
{
    public Cinematic_1_Controller cinematic;
    public float prewarmDelay = 1f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(prewarmDelay);

        var tempCamObj = new GameObject("PrewarmCamera", typeof(Camera));
        var tempCam = tempCamObj.GetComponent<Camera>();
        tempCam.enabled = false;
        tempCam.clearFlags = CameraClearFlags.SolidColor;
        tempCam.backgroundColor = Color.black;
        tempCam.cullingMask = -1;
        tempCam.nearClipPlane = 0.01f;
        tempCam.farClipPlane = 1000f;
        tempCam.orthographic = true;
        tempCam.orthographicSize = 500f;

        tempCamObj.transform.position = Vector3.zero;
        tempCamObj.transform.rotation = Quaternion.identity;

        RenderTexture rt = new RenderTexture(256, 256, 0);
        tempCam.targetTexture = rt;
        tempCam.Render();
        tempCam.targetTexture = null;
        rt.Release();

        tempCamObj.transform.position = new Vector3(100, 50, 0);
        tempCamObj.transform.LookAt(Vector3.zero);
        rt = new RenderTexture(256, 256, 0);
        tempCam.targetTexture = rt;
        tempCam.Render();
        tempCam.targetTexture = null;
        rt.Release();

        tempCamObj.transform.position = new Vector3(-100, 50, 100);
        tempCamObj.transform.LookAt(Vector3.zero);
        rt = new RenderTexture(256, 256, 0);
        tempCam.targetTexture = rt;
        tempCam.Render();
        tempCam.targetTexture = null;
        rt.Release();

        Destroy(tempCamObj);
        yield return null;
    }
}

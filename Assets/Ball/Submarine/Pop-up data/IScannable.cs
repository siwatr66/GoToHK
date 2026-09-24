using UnityEngine;

public interface IScannable
{
    ScanData GetScanData();
}

[System.Serializable]
public class ScanData
{
    public string title;
    public string subtitle;
    [TextArea(3, 8)]
    public string description;
    public Sprite displayImage;
    public Vector3 hitPosition;
}
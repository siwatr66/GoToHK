using System.Collections.Generic;
using UnityEngine;

public class InteractableTarget : MonoBehaviour, IScannable
{
    [Header("Title / Header")]
    public string targetTitle = "Challenger Deep";
    public string subTitle = "Ocean Trench";

    [Header("Description Content")]
    [TextArea(3, 8)]
    public List<string> descriptionParagraphs = new List<string>();

    [Header("Images")]
    public List<Sprite> displayImages = new List<Sprite>();

    // ปฏิบัติตามสัญญาของ Interface IScannable
    public ScanData GetScanData()
    {
        string fullDesc = descriptionParagraphs.Count > 0 
            ? string.Join("\n\n", descriptionParagraphs) 
            : "";

        Sprite img = displayImages.Count > 0 ? displayImages[0] : null;

        return new ScanData
        {
            title = targetTitle,
            subtitle = subTitle,
            description = fullDesc,
            displayImage = img,
            hitPosition = transform.position
        };
    }
}
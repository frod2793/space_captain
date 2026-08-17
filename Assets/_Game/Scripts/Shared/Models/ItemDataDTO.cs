using System;
using UnityEngine;

[Serializable]
public class ItemDataDTO
{
    public string ItemId;
    public string ItemName;
    public Sprite ItemIcon;
    [TextArea(3, 10)]
    public string Description;
}

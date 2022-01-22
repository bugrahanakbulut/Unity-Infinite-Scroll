using System.Collections.Generic;
using DynamicScrollRect;
using UnityEngine;

public class DemoUI : MonoBehaviour
{
    [SerializeField] private ScrollContent _content = null;

    [SerializeField] private int _itemCount = 50;
    
    private void Awake()
    {
        Application.targetFrameRate = 60;

        List<ScrollItemData> contentDatas = new List<ScrollItemData>();

        for (int i = 0; i < _itemCount; i++)
        {
            contentDatas.Add(new ScrollItemData(i));
        }
        
        _content.InitScrollContent(contentDatas);
    }
}

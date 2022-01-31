[![CodeFactor](https://www.codefactor.io/repository/github/bugrahanakbulut/unity-infinite-scroll/badge/main)](https://www.codefactor.io/repository/github/bugrahanakbulut/unity-infinite-scroll/overview/main)

# Unity-Infinite-Scroll
Unity UI Optimized Scroll Rect to represent large number of entities with less rect transform. Currently only vertical scroll supported.

![Alt Text](https://github.com/bugrahanakbulut/Unity-Infinite-Scroll/blob/main/Assets/Resources/scroll_infinite.gif)
![Alt Text](https://github.com/bugrahanakbulut/Unity-Infinite-Scroll/blob/main/Assets/Resources/scroll_jumpback.gif)


## For Custom Usage

By inheriting from ScrollItemData and ScrollItem<T> you can create your custom scroll entities. You should be aware of CustomScrollItem inherited from monobehaviour so its' file name and class name must be identical to attach component to game object.

```cs

public class CustomScrollItemData : ScrollItemData
{
    // Some arbitrary fields and properties

    public CustomScrollItemData(int index) : base(index)
    {
        
    }
}

public class CustomScrollItem : ScrollItem<CustomScrollItemData> 
{
    protected override void InitItemData(CustomScrollItemData data)
    {
        base.InitItemData(data);
    }

    protected override void ActivatedCustomActions()
    {
        base.ActivatedCustomActions();
    }

    protected override void DeactivatedCustomActions()
    {
        base.DeactivatedCustomActions();
    }
}
```
    
## Focusing Items 
    
Scroll rect can make focus easily when entities outside of the viewport. You can add offset to focus or determine focus duration from focus settings on DynamicScrollRect component.
    
<p align="center">
    <img src="https://github.com/bugrahanakbulut/Unity-Infinite-Scroll/blob/main/Assets/Resources/scroll_focus.gif" alt="animated" />
</p>
   
    
    
## TODO

This project still in under development so there might be some naughty bugs :D. If you met some of them or you need to implement any feature top of it, and if you get stuck please feel free to contact. There are some feature I will be implement in near future :
    
- [x] Focus Item :tada:
- [ ] Horizontal Movement Support [wip]
- [ ] Handling changes in data throughout application life-time
    
    
## Contact & Some Additional Notes
The project is done for educational purpose and may include some files that I do not own. If you own anything and don't want it to be in the project or if you have any questions or comments, please feel free to contact me.



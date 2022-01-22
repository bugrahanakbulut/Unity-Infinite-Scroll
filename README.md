# Unity-Infinite-Scroll
Unity UI Optimized Scroll Rect to represent large number of entities with less rect transform


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


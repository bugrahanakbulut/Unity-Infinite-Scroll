namespace DynamicScrollRect
{
    public class ScrollItemData
    {
        public int Index { get; }

        public ScrollItemData(int index)
        {
            Index = index;
        }
    }

    public class CustomScrollItemData : ScrollItemData
    {
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
}

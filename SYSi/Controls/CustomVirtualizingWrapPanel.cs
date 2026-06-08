namespace SYSi.Controls
{
    public class CustomVirtualizingWrapPanel : VirtualizingWrapPanel
    {
        public static readonly DependencyProperty MaxColumnsProperty =
            DependencyProperty.Register(
                nameof(MaxColumns),
                typeof(int),
                typeof(CustomVirtualizingWrapPanel),
                new FrameworkPropertyMetadata(
                    int.MaxValue,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public int MaxColumns
        {
            get => (int)GetValue(MaxColumnsProperty);
            set => SetValue(MaxColumnsProperty, value);
        }

        public bool EnableCustomBehavior { get; set; } = true;

        protected override Size MeasureOverride(Size availableSize)
        {
            Size result = base.MeasureOverride(availableSize);

            if (MaxColumns > 0)
            {
                ItemsPerRowCount = Math.Min(ItemsPerRowCount, MaxColumns);
                RowCount = (int)Math.Ceiling(
                    (double)Items.Count / ItemsPerRowCount);
            }

            return result;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var offsetX = GetX(Offset);
            var offsetY = GetY(Offset);

            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo)
                offsetY = 0;

            Size childSize = CalculateChildArrangeSize(finalSize);
            CalculateSpacing(finalSize, out double innerSpacing, out double outerSpacing);

            if (Orientation == Orientation.Horizontal)
            {
                childSize = new Size(finalSize.Width, childSize.Height);
            }

            int itemsPerRow = Math.Min(ItemsPerRowCount, MaxColumns);

            for (int childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                UIElement child = InternalChildren[childIndex];
                int itemIndex = GetItemIndexFromChildIndex(childIndex);

                int columnIndex = itemIndex % itemsPerRow;
                int rowIndex = itemIndex / itemsPerRow;

                double x = outerSpacing + columnIndex * (GetWidth(childSize) + innerSpacing);
                double y = rowIndex * GetHeight(childSize);

                if (GetHeight(finalSize) == 0.0)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                }
                else
                {
                    child.Arrange(CreateRect(
                        x - offsetX,
                        y - offsetY,
                        childSize.Width,
                        childSize.Height));
                }
            }

            return finalSize;
        }

        protected override void OnOrientationChanged()
        {
            base.OnOrientationChanged();
        }
    }
}

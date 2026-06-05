namespace SYSi.Views.Pages
{
    [PageMeta("page_memory_title", "page_memory_summary", SymbolRegular.Memory16, 3, true)]
    public partial class MemoryPage : INavigableView<MemoryViewModel>
    {
        public MemoryViewModel ViewModel { get; }

        public MemoryPage(MemoryViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

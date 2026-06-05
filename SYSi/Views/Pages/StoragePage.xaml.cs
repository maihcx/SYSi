namespace SYSi.Views.Pages
{
    [PageMeta("page_storage_title", "page_storage_summary", SymbolRegular.HardDrive20, 4, true)]
    public partial class StoragePage : INavigableView<StorageViewModel>
    {
        public StorageViewModel ViewModel { get; }

        public StoragePage(StorageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

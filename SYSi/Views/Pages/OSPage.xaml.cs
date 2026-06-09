namespace SYSi.Views.Pages
{
    [PageMeta("page_os_title", "page_os_summary", SymbolRegular.DesktopPulse24, 6, true)]
    public partial class OSPage : INavigableView<OSViewModel>
    {
        public OSViewModel ViewModel { get; }

        public OSPage(OSViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

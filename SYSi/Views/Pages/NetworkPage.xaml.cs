namespace SYSi.Views.Pages
{
    [PageMeta("page_network_title", "page_network_summary", SymbolRegular.NetworkCheck24, 6, true)]
    public partial class NetworkPage : INavigableView<NetworkViewModel>
    {
        public NetworkViewModel ViewModel { get; }

        public NetworkPage(NetworkViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

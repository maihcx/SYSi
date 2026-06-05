namespace SYSi.Views.PagesBottom
{
    [PageMeta("page_about_title", "page_about_summary", SymbolRegular.Info24, 1000)]
    public partial class AboutPage : INavigableView<AboutViewModel>
    {
        public AboutViewModel ViewModel { get; }

        public AboutPage(AboutViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

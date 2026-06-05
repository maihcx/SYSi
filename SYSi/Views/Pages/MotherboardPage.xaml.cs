namespace SYSi.Views.Pages
{
    [PageMeta("page_motherboard_title", "page_motherboard_summary", SymbolRegular.Board24, 5, true)]
    public partial class MotherboardPage : INavigableView<MotherboardViewModel>
    {
        public MotherboardViewModel ViewModel { get; }

        public MotherboardPage(MotherboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

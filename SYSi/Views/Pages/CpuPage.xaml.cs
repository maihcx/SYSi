namespace SYSi.Views.Pages
{
    [PageMeta("page_cpu_title", "page_cpu_summary", SymbolRegular.BrainCircuit24, 1, true)]
    public partial class CpuPage : INavigableView<CpuViewModel>
    {
        public CpuViewModel ViewModel { get; }

        public CpuPage(CpuViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

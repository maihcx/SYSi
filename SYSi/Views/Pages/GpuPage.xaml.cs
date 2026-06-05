namespace SYSi.Views.Pages
{
    [PageMeta("page_gpu_title", "page_gpu_summary", SymbolRegular.Stream24, 2, true)]
    public partial class GpuPage : INavigableView<GpuViewModel>
    {
        public GpuViewModel ViewModel { get; }

        public GpuPage(GpuViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

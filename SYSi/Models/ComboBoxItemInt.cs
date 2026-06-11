namespace SYSi.Models
{
    public partial class ComboBoxItemInt : ObservableObject
    {
        public ComboBoxItemInt()
        {
            LanguageBase.LanguageChanged += LanguageBase_LanguageChanged;
        }

        [ObservableProperty]
        private int _value = 0;

        [ObservableProperty]
        private string _contentKey = string.Empty;

        [ObservableProperty]
        private string _content = string.Empty;

        public override string ToString() => Content;

        public override bool Equals(object? obj)
        {
            return obj is ComboBoxItemInt other && this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        partial void OnContentKeyChanged(string value)
        {
            Content = LanguageBase.GetLangValue(value);
        }

        private void LanguageBase_LanguageChanged(string language)
        {
            OnContentKeyChanged(ContentKey);
        }
    }
}

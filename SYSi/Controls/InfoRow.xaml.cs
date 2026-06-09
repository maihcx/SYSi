// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace SYSi.Controls;

public class InfoRow : ContentControl
{
    private Wpf.Ui.Controls.Button? _copyButton;

    public InfoRow()
    {

    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_copyButton != null)
        {
            _copyButton.Click -= CopyButton_Click;
        }

        _copyButton = GetTemplateChild("PART_CopyButton") as Wpf.Ui.Controls.Button;

        if (_copyButton != null)
        {
            _copyButton.Click += CopyButton_Click;
        }

        UpdateValueVisibility();
    }

    public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
        nameof(LabelText),
        typeof(string),
        typeof(InfoRow),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(InfoRow),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty IsLastProperty = DependencyProperty.Register(
        nameof(IsLast),
        typeof(bool),
        typeof(InfoRow),
        new PropertyMetadata(false)
    );

    public static readonly DependencyProperty NavigateUriProperty = DependencyProperty.Register(
        nameof(NavigateUri),
        typeof(string),
        typeof(InfoRow),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth),
        typeof(double),
        typeof(InfoRow),
        new PropertyMetadata((double)120)
    );

    public static readonly DependencyProperty ValueContentProperty = DependencyProperty.Register(
        nameof(ValueContent),
        typeof(object),
        typeof(InfoRow),
        new PropertyMetadata(null, OnValueContentChanged)
    );

    public static readonly DependencyProperty ValueHorizontalAlignProperty = DependencyProperty.Register(
        nameof(ValueHorizontalAlign),
        typeof(HorizontalAlignment),
        typeof(InfoRow),
        new PropertyMetadata(HorizontalAlignment.Left)
    );

    public string? LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string? ValueText
    {
        get => (string?)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public bool IsLast
    {
        get => (bool)GetValue(IsLastProperty);
        set => SetValue(IsLastProperty, value);
    }

    public string? NavigateUri
    {
        get => (string?)GetValue(NavigateUriProperty);
        set => SetValue(NavigateUriProperty, value);
    }

    public double? LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public object? ValueContent
    {
        get => GetValue(ValueContentProperty);
        set => SetValue(ValueContentProperty, value);
    }

    public HorizontalAlignment ValueHorizontalAlign
    {
        get => (HorizontalAlignment)GetValue(ValueHorizontalAlignProperty);
        set => SetValue(ValueHorizontalAlignProperty, value);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        string? text = ValueContent as string ?? ValueText;
        if (!string.IsNullOrWhiteSpace(text))
            Clipboard.SetText(text);
    }

    private static void OnValueContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoRow row)
        {
            row.UpdateValueVisibility();
        }
    }

    private void UpdateValueVisibility()
    {
        var contentPresenter = GetTemplateChild("PART_ValueContent") as ContentPresenter;
        var valueText = GetTemplateChild("PART_ValueText")    as FrameworkElement;
        var hyperlinkBtn = GetTemplateChild("PART_HyperlinkButton") as FrameworkElement;

        bool hasContent = ValueContent != null;

        if (contentPresenter != null)
            contentPresenter.Visibility = hasContent ? Visibility.Visible : Visibility.Collapsed;

        if (valueText != null)
            valueText.Visibility = hasContent ? Visibility.Collapsed : valueText.Visibility;

        if (hyperlinkBtn != null)
            hyperlinkBtn.Visibility = hasContent ? Visibility.Collapsed : hyperlinkBtn.Visibility;
    }
}

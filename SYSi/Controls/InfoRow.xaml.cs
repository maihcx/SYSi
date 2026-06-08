// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections;

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

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth),
        typeof(double),
        typeof(InfoRow),
        new PropertyMetadata((double)120)
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

    public double? LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ValueText))
            Clipboard.SetText(ValueText);
    }
}

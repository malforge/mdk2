using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A wrapper control that adds consistent validation display to any content.
///     Always reserves vertical space for validation messages and applies error styling when needed.
/// </summary>
public class ValidatedControl : TemplatedControl
{
    /// <summary>
    ///     Defines the <see cref="Content" /> property.
    /// </summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ValidatedControl, object?>(nameof(Content));

    /// <summary>
    ///     Defines the <see cref="ValidationMessage" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValidationMessageProperty =
        AvaloniaProperty.Register<ValidatedControl, string?>(nameof(ValidationMessage));

    /// <summary>
    ///     Defines the <see cref="ResolvedValidationMessage" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ResolvedValidationMessageProperty =
        AvaloniaProperty.Register<ValidatedControl, string?>(nameof(ResolvedValidationMessage));

    /// <summary>
    ///     Defines the <see cref="HasValidationMessage" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasValidationMessageProperty =
        AvaloniaProperty.Register<ValidatedControl, bool>(nameof(HasValidationMessage));

    /// <summary>
    ///     Gets or sets the content to be validated.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>
    ///     Gets or sets the external validation message.
    ///     This is used when the wrapped control doesn't implement ISupportValidation.
    /// </summary>
    public string? ValidationMessage
    {
        get => GetValue(ValidationMessageProperty);
        set => SetValue(ValidationMessageProperty, value);
    }

    /// <summary>
    ///     Gets the resolved validation message from either the content's ISupportValidation
    ///     or the external ValidationMessage property.
    /// </summary>
    public string? ResolvedValidationMessage
    {
        get => GetValue(ResolvedValidationMessageProperty);
        private set => SetValue(ResolvedValidationMessageProperty, value);
    }

    /// <summary>
    ///     Gets whether there is a validation message to display.
    /// </summary>
    public bool HasValidationMessage
    {
        get => GetValue(HasValidationMessageProperty);
        private set => SetValue(HasValidationMessageProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            UnsubscribeFromContent(change.OldValue);
            SubscribeToContent(change.NewValue);
            UpdateResolvedMessage();
            UpdateContentErrorClass();
        }
        else if (change.Property == ValidationMessageProperty)
        {
            UpdateResolvedMessage();
            UpdateContentErrorClass();
        }
    }

    void SubscribeToContent(object? content)
    {
        if (content is ISupportValidation validatable)
        {
            // Listen for property changes on the validatable control
            if (content is AvaloniaObject avaloniaObject)
            {
                avaloniaObject.PropertyChanged += OnContentPropertyChanged;
            }
        }
    }

    void UnsubscribeFromContent(object? content)
    {
        if (content is AvaloniaObject avaloniaObject)
        {
            avaloniaObject.PropertyChanged -= OnContentPropertyChanged;
        }
    }

    void OnContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // When content properties change, re-evaluate validation
        UpdateResolvedMessage();
        UpdateContentErrorClass();
    }

    void UpdateResolvedMessage()
    {
        // Priority: ISupportValidation.ValidationError > external ValidationMessage
        if (Content is ISupportValidation validatable)
        {
            ResolvedValidationMessage = validatable.ValidationError;
        }
        else
        {
            ResolvedValidationMessage = ValidationMessage;
        }
        
        // Update HasValidationMessage property for opacity binding
        HasValidationMessage = !string.IsNullOrEmpty(ResolvedValidationMessage);
    }

    void UpdateContentErrorClass()
    {
        // Only apply Classes.error if content does NOT implement ISupportValidation
        // (ISupportValidation controls handle their own styling)
        if (Content is not ISupportValidation && Content is Control control)
        {
            var hasError = !string.IsNullOrEmpty(ResolvedValidationMessage);
            control.Classes.Set("error", hasError);
        }
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.OnBoarding;

public partial class OnboardingPage : ContentPage
{
    private OnboardingViewModel _viewModel;

    public OnboardingPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        
        if (BindingContext is OnboardingViewModel vm)
        {
            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // Trigger animation on initial slide
            _ = AnimateSlideAsync(0);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingViewModel.SelectedIndex))
        {
            _ = AnimateSlideAsync(_viewModel.SelectedIndex);
        }
    }

    private async Task AnimateSlideAsync(int index)
    {
        // Select corresponding container
        VisualElement targetContainer = index switch
        {
            0 => Slide0Container,
            1 => Slide1Container,
            2 => Slide2Container,
            _ => null
        };

        if (targetContainer == null) return;

        // Reset state for all containers to prevent overlap/glitches
        if (Slide0Container != null && index != 0) { Slide0Container.Opacity = 0; Slide0Container.TranslationY = 30; }
        if (Slide1Container != null && index != 1) { Slide1Container.Opacity = 0; Slide1Container.TranslationY = 30; }
        if (Slide2Container != null && index != 2) { Slide2Container.Opacity = 0; Slide2Container.TranslationY = 30; }

        // Give a tiny delay for MAUI IsVisible bindings to apply first
        await Task.Delay(40);

        // Run premium fade and slide up animation
        targetContainer.Opacity = 0;
        targetContainer.TranslationY = 35;

        await Task.WhenAll(
            targetContainer.FadeTo(1, 400, Easing.CubicOut),
            targetContainer.TranslateTo(0, 0, 400, Easing.CubicOut)
        );
    }
}

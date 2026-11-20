namespace BULi_MAUI;

public partial class a2_StarterPage : ContentPage
{
	public a2_StarterPage()
	{
		InitializeComponent();
	}
    private async void OnGetStartedClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MainPage));
    }
}

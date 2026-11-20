
namespace BULi_MAUI;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

	}
    private async void OnStudentClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(b2_stdSignIn));
    }

    private async void OnTeacherClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(b1_tcrSignIn));

    }

}
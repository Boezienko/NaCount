namespace NaCount
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {

            // Open the camera to take a photo
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        // Save the photo to the local storage
                        var filePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                        using var stream = await photo.OpenReadAsync();
                        using var fileStream = File.OpenWrite(filePath);
                        await stream.CopyToAsync(fileStream);

                        // Display an alert with the file path
                        await DisplayAlert("Photo Captured", $"Photo saved to: {filePath}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Unsupported", "Camera capture is not supported on this device.", "OK");
            }

        }
    }

}

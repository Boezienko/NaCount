namespace NaCount
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Util;

    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        // Define the path to save the image in the Resources/Images directory
                        var resourceDir = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Images");
                        Directory.CreateDirectory(resourceDir); // Ensure the directory exists
                        var filePath = Path.Combine(resourceDir, photo.FileName);

                        // Save the photo to the specified directory
                        using (var stream = await photo.OpenReadAsync())
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                        }

                        // Load the image into Emgu CV Mat
                        Mat img = CvInvoke.Imread(filePath, ImreadModes.Color);

                        // Convert to grayscale
                        Mat gray = new Mat();
                        CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

                        // Apply Canny edge detection
                        Mat cannyEdges = new Mat();
                        CvInvoke.Canny(gray, cannyEdges, 100, 200);

                        // Find contours
                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        {
                            CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                            int shapeCount = contours.Size;

                            // Display the number of shapes detected
                            await DisplayAlert("Shapes Detected", $"Number of shapes detected: {shapeCount}", "OK");
                        }
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

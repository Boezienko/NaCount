namespace NaCount
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Util;
    using System.Text;

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

                        //AnalyzePhoto(filePath);

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


        private async void AnalyzePhoto(String filepath)
        {
            // Load the image into Emgu CV Mat
            Mat img = CvInvoke.Imread(filepath, ImreadModes.Color);

            // Convert to grayscale
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

            // Apply Gausian blur
            CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(3, 3), 1.0);

            // Apply Canny edge detection
            Mat cannyEdges = new Mat();
            CvInvoke.Canny(gray, cannyEdges, 75, 200);

            // Find contours
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                // Filter contours by size
                int minContourArea = 50; // Adjust this value based on your needs
                int shapeCount = 0;
                for (int i = 0; i < contours.Size; i++)
                {
                    if (CvInvoke.ContourArea(contours[i]) > minContourArea)
                    {
                        shapeCount++;
                    }
                }

                // Display the number of shapes detected
                await DisplayAlert("Shapes Detected", $"Number of shapes detected: {shapeCount}", "OK");
            }
        }

       



    }



}

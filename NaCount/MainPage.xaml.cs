namespace NaCount
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Util;
    using System.Text;
    using NaCount.DataStructures;
    using Microsoft.ML;
    using ObjectDetection;
    using NaCount.YoloParser;
    using System.Drawing.Drawing2D;
    using System.Drawing;
    using ObjectDetection.YoloParser;


    public partial class MainPage : ContentPage
    {
        private MLContext mlContext;
        private OnnxModelScorer modelScorer;
        private YoloOutputParser parser;

        public MainPage()
        {
            Console.WriteLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");

            InitializeComponent();

            // Initialize ML context and components here
            mlContext = new MLContext();
            

            var assetsRelativePath = FileSystem.AppDataDirectory;
            //string assetsPath = GetAbsolutePath(assetsRelativePath);
            var modelFilePath = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Raw", "assets", "Model", "TinyYolo2_model.onnx");
            var pathToImage = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Raw", "assets", "images", "input");

            modelScorer = new OnnxModelScorer(pathToImage, modelFilePath, mlContext);
            parser = new YoloOutputParser();
        }

        static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(MauiProgram).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
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
                        var resourceDir = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Raw", "assets", "images", "input");
                        //Directory.CreateDirectory(resourceDir);
                        var filePath = Path.Combine(resourceDir, photo.FileName);

                        using (var stream = await photo.OpenReadAsync())
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        // Analyze the photo using the ONNX model
                        DetectObjects(filePath);
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

        void DetectObjects(string filePath)
        {
            string pathToImage = "assets/Model/input";
            string pathToOutput = "assets/Model/output";

            // Load the captured image into ImageNetData
            var images = new List<ImageNetData> { new ImageNetData(filePath) };
            var imageDataView = mlContext.Data.LoadFromEnumerable(images);
            var probabilities = modelScorer.Score(imageDataView);
            var boundingBoxes = probabilities
                .Select(probability => parser.ParseOutputs(probability))
                .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

            // Draw bounding boxes and display results
            DrawBoundingBox(pathToImage, pathToOutput, Path.GetFileName(filePath), boundingBoxes.First());
        }

        void DrawBoundingBox(string inputImageLocation, string outputImageLocation, string imageName, IList<YoloBoundingBox> filteredBoundingBoxes)
        {
            Image image = Image.FromFile(Path.Combine(inputImageLocation, imageName));

            var originalImageHeight = image.Height;
            var originalImageWidth = image.Width;

            foreach (var box in filteredBoundingBoxes)
            {
                // Get Bounding Box Dimensions
                var x = (uint)Math.Max(box.Dimensions.X, 0);
                var y = (uint)Math.Max(box.Dimensions.Y, 0);
                var width = (uint)Math.Min(originalImageWidth - x, box.Dimensions.Width);
                var height = (uint)Math.Min(originalImageHeight - y, box.Dimensions.Height);

                // Resize To Image
                x = (uint)originalImageWidth * x / OnnxModelScorer.ImageNetSettings.imageWidth;
                y = (uint)originalImageHeight * y / OnnxModelScorer.ImageNetSettings.imageHeight;
                width = (uint)originalImageWidth * width / OnnxModelScorer.ImageNetSettings.imageWidth;
                height = (uint)originalImageHeight * height / OnnxModelScorer.ImageNetSettings.imageHeight;

                // Bounding Box Text
                string text = $"{box.Label} ({(box.Confidence * 100).ToString("0")}%)";

                using (Graphics thumbnailGraphic = Graphics.FromImage(image))
                {
                    thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality;
                    thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality;
                    thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // Define Text Options
                    Font drawFont = new Font("Arial", 12, FontStyle.Bold);
                    SizeF size = thumbnailGraphic.MeasureString(text, drawFont);
                    SolidBrush fontBrush = new SolidBrush(Color.Black);
                    Point atPoint = new Point((int)x, (int)y - (int)size.Height - 1);

                    // Define BoundingBox options
                    Pen pen = new Pen(box.BoxColor, 3.2f);
                    SolidBrush colorBrush = new SolidBrush(box.BoxColor);

                    // Draw text on image 
                    thumbnailGraphic.FillRectangle(colorBrush, (int)x, (int)(y - size.Height - 1), (int)size.Width, (int)size.Height);
                    thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint);

                    // Draw bounding box on image
                    thumbnailGraphic.DrawRectangle(pen, x, y, width, height);
                }
            }

            if (!Directory.Exists(outputImageLocation))
            {
                Directory.CreateDirectory(outputImageLocation);
            }

            image.Save(Path.Combine(outputImageLocation, imageName));
        }

        /*
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
        */
    }
}

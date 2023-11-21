using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace sprayPaintAssessment
{
    public partial class MainWindow : Window
    {
        // Fields to track the painting and erasing status, and to manage colors and randomness.
        private bool _isPainting = false;
        private bool _isErasing = false;
        private readonly Random _random = new Random();
        private Brush _paintColor = Brushes.Black; // Default paint color set to black.

        // Constructor to initialize components and set default color.
        public MainWindow()
        {
            InitializeComponent();
            colorPicker.SelectedIndex = 0;// Set default color to the first color in the ComboBox
            radiusSlider.Value = 7;
        }

        // Event handler to save the spray paint work.
        private void SaveSpray_Click(object sender, RoutedEventArgs e)
        {
            // Set up a save file dialog with a custom filter for spray files.
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Spray File (*.spray)|*.spray"
            };

            // Open the save file dialog and serialize the spray particles to a file if the user confirms.
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    var serializer = new XmlSerializer(typeof(List<SprayParticle>));
                    var particles = paintCanvas.Children.OfType<Ellipse>()
                                          .Select(ellipse => new SprayParticle
                                          {
                                              X = Canvas.GetLeft(ellipse),
                                              Y = Canvas.GetTop(ellipse),
                                              Color = new SerializableColor
                                              {
                                                  A = ((SolidColorBrush)ellipse.Fill).Color.A,
                                                  R = ((SolidColorBrush)ellipse.Fill).Color.R,
                                                  G = ((SolidColorBrush)ellipse.Fill).Color.G,
                                                  B = ((SolidColorBrush)ellipse.Fill).Color.B
                                              }
                                          }).ToList();

                    serializer.Serialize(stream, particles);
                }
            }
        }

        // Class to represent a spray particle with position and color.
        public class SprayParticle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public SerializableColor Color { get; set; }
        }

        // Struct to represent a color in a serializable way.
        public struct SerializableColor
        {
            public byte A;
            public byte R;
            public byte G;
            public byte B;
        }

        // Event handler to load spray paint from a file.
        private void LoadSpray_Click(object sender, RoutedEventArgs e)
        {
            // Set up an open file dialog with a custom filter for spray files.
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Spray File (*.spray)|*.spray"
            };

            // Open the file dialog and deserialize the spray particles from a file if the user confirms.
            if (openFileDialog.ShowDialog() == true)
            {
                using (var stream = new FileStream(openFileDialog.FileName, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(List<SprayParticle>));
                    var particles = (List<SprayParticle>)serializer.Deserialize(stream);

                    // Create and add ellipses to the canvas for each deserialized particle.
                    foreach (var particle in particles)
                    {
                        var ellipse = new Ellipse
                        {
                            Width = 1,
                            Height = 1,
                            Fill = new SolidColorBrush(Color.FromArgb(particle.Color.A, particle.Color.R, particle.Color.G, particle.Color.B))
                        };

                        Canvas.SetLeft(ellipse, particle.X);
                        Canvas.SetTop(ellipse, particle.Y);
                        paintCanvas.Children.Add(ellipse);
                    }
                }
            }
        }

        // Event handler to change the paint color based on the selected item in the colorPicker ComboBox.
        private void colorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colorPicker.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string content)
            {
                var brush = new BrushConverter().ConvertFromString(content) as Brush;
                if (brush != null)
                {
                    _paintColor = brush;
                }
            }
        }

        // Event handler for mouse movement to handle painting and erasing.
        private void paintCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPainting && !_isErasing)
            {
                Point mousePosition = e.GetPosition(paintCanvas);
                DrawSpray(mousePosition);
            }
            else if (_isErasing)
            {
                EraseSpray(e.GetPosition(paintCanvas));
            }
        }

        // Event handler to start painting when the mouse button is pressed.
        private void paintCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isErasing) // Start painting only if not in erase mode
            {
                _isPainting = true;
            }
        }

        // Event handler to stop painting when the mouse button is released.
        private void paintCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isErasing) // Stop painting only if not in erase mode
            {
                _isPainting = false;
            }
        }

        // Event handler to load an image to the canvas.
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    imageView.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading image: " + ex.Message);
                }
            }
        }

        // Event handler to toggle between erase and paint mode.
        private void ToggleEraseMode_Click(object sender, RoutedEventArgs e)
        {
            _isErasing = !_isErasing;

            // Change the button content for visual feedback.
            if (_isErasing)
            {
                ((Button)sender).Content = "Paint Mode";
            }
            else
            {
                ((Button)sender).Content = "Erase Mode";
            }
        }

        // Method to erase spray paint from the canvas.
        private void EraseSpray(Point position)
        {
            double eraseRadius = 20; // Radius around the cursor position for erasing.

            // Iterate backwards through the canvas children collection.
            for (int i = paintCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (paintCanvas.Children[i] is Ellipse ellipse)
                {
                    double left = Canvas.GetLeft(ellipse);
                    double top = Canvas.GetTop(ellipse);

                    // Check if the ellipse is within the erase radius.
                    if (Math.Pow(left - position.X, 2) + Math.Pow(top - position.Y, 2) <= eraseRadius * eraseRadius)
                    {
                        paintCanvas.Children.RemoveAt(i);
                    }
                }
            }
        }

        private void EraseAllSpray()
        {
            paintCanvas.Children.Clear();
        }


        private void EraseAllButton_Click(object sender, RoutedEventArgs e)
        {
            EraseAllSpray();
        }




        // Method to draw spray paint on the canvas.
        private void DrawSpray(Point position)
        {
            int particleCount = (int)densitySlider.Value; // Use the value from the slider for density.
            double radius = radiusSlider.Value; // Radius of the spray area.

            // Create and add a number of small ellipses (particles) to the canvas around the mouse position.
            for (int i = 0; i < particleCount; i++)
            {
                Ellipse ellipse = new Ellipse
                {
                    Fill = _paintColor,
                    Width = 0.8,
                    Height = 0.8
                };

                double offsetX = _random.NextDouble() * 2 * radius - radius;
                double offsetY = _random.NextDouble() * 2 * radius - radius;

                // Add the ellipse to the canvas if it is within the defined radius.
                if (offsetX * offsetX + offsetY * offsetY <= radius * radius)
                {
                    Canvas.SetLeft(ellipse, position.X + offsetX);
                    Canvas.SetTop(ellipse, position.Y + offsetY);
                    paintCanvas.Children.Add(ellipse);
                }
            }
        }
    }
}

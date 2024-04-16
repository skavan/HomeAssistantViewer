using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
// using System.Diagnostics;
using System.Text.Json;

public class MainForm : Form
{
    private WebView2 webView;
    private MenuForm menuForm;
    private bool isShown = false;

    public MainForm()
    {
        // Get the current working directory
        string workingDirectory = System.IO.Directory.GetCurrentDirectory();
        // Console.WriteLine(workingDirectory);

        // Load the application icon
        Bitmap bitmap = new Bitmap("icons/homeassistant.png");
        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);

        // Set the form title and icon
        this.Text = "HomeAssistant";
        this.Icon = icon;

        // Get the settings from the JSON file
        (string uri, Dictionary<string, Size> sizes) = GetSettingsFromJsonFile("settings.json");

        // Initialize the WebView2 control
        this.webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Source = new Uri(uri)
        };

        // Set the form size to the default size
        this.Width = sizes["default"].Width;
        this.Height = sizes["default"].Height;

        // Add the WebView2 control to the form
        this.Controls.Add(this.webView);

        // Initialize the MenuForm and set its location when the MainForm is moved
        this.menuForm = new MenuForm(this, sizes);
        this.Move += (sender, e) => this.menuForm.SetLocation();
    }

    // Method to get the settings from the JSON file
    private (string Uri, Dictionary<string, Size> Sizes) GetSettingsFromJsonFile(string filePath)
    {
        string json = System.IO.File.ReadAllText(filePath);
        Dictionary<string, Size> sizes = new Dictionary<string, Size>();
        string uri = "http://bbc.com"; // Default URI

        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            JsonElement root = doc.RootElement;

            // Get the URI from the JSON file
            if (root.TryGetProperty("uri", out JsonElement uriElement))
            {
                string? uriValue = uriElement.GetString();
                if (!string.IsNullOrEmpty(uriValue))
                {
                    uri = uriValue;
                }
            }

            // Get the sizes from the JSON file
            if (root.TryGetProperty("sizes", out JsonElement sizesElement) && sizesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty sizeProperty in sizesElement.EnumerateObject())
                {
                    if (sizeProperty.Value.TryGetProperty("width", out JsonElement widthElement) &&
                        sizeProperty.Value.TryGetProperty("height", out JsonElement heightElement))
                    {
                        int width = widthElement.GetInt32();
                        int height = heightElement.GetInt32();
                        sizes[sizeProperty.Name] = new Size(width, height);
                    }
                }
            }
        }

        return (uri, sizes);
    }

    // Event handler for the Resize event
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (this.isShown)
        {
            // Update the location of the MenuForm when the MainForm is resized
            this.menuForm.SetLocation();
            // Debug.WriteLine("MainForm size: " + this.Size);
        }
    }

    // Event handler for the Shown event
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        this.isShown = true;

        // Show the MenuForm and set its location when the MainForm is shown
        this.menuForm.Show(this);
        this.menuForm.SetLocation();
    }
}

public class MenuForm : Form
{
    private MainForm mainForm;
    private IconButton phoneButton;
    private IconButton tabletButton;
    private IconButton desktopButton;

    // Constructor for the MenuForm class
    public MenuForm(MainForm mainForm, Dictionary<string, Size> sizes)
    {
        // Store a reference to the MainForm
        this.mainForm = mainForm;

        // Set the form properties
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.BackColor = Color.Magenta; // Set the BackColor to a color that is not used in your .png images
        this.TransparencyKey = Color.Magenta; // Set the TransparencyKey to the same color
        this.AutoSize = true;
        this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // Create the buttons for the different sizes
        this.phoneButton = CreateButton("icons/phone.png", sizes["phone"]);
        this.tabletButton = CreateButton("icons/tablet.png", sizes["tablet"]);
        this.desktopButton = CreateButton("icons/desktop.png", sizes["desktop"]);

        // Create a FlowLayoutPanel for the buttons
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Magenta // Set the BackColor of the FlowLayoutPanel to the same color
        };

        // Add the buttons to the FlowLayoutPanel
        layout.Controls.Add(this.phoneButton);
        layout.Controls.Add(this.tabletButton);
        layout.Controls.Add(this.desktopButton);

        // Add the FlowLayoutPanel to the form
        this.Controls.Add(layout);
    }

    // Method to create a button with a specific icon and size
    private IconButton CreateButton(string iconPath, Size size)
    {
        var button = new IconButton
        {
            Image = AdjustImageBrightness(Image.FromFile(iconPath), 0.5f), // 50% brightness
            Size = new Size(24, 24),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255); // Set border color to transparent
        button.Click += (sender, e) => this.mainForm.Size = size; // Set the MainForm size when the button is clicked
        return button;
    }

    // Method to adjust the brightness of an image
    public static Image AdjustImageBrightness(Image image, float brightness)
    {
        Bitmap bmp = new Bitmap(image.Width, image.Height);
        using (Graphics graphics = Graphics.FromImage(bmp))
        {
            ColorMatrix colormatrix = new ColorMatrix();
            colormatrix.Matrix40 = brightness;
            colormatrix.Matrix41 = brightness;
            colormatrix.Matrix42 = brightness;
            ImageAttributes imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgAttribute);
        }
        return bmp;
    }

    // Method to set the location of the MenuForm
    public void SetLocation()
    {
        this.Location = new Point(this.mainForm.Location.X + this.mainForm.Width / 2 - this.Width / 2,
                                  this.mainForm.Location.Y + 2);
    }
}

// The IconButton class extends the Button class and overrides some of its methods
public class IconButton : Button
{
    public new Image? Image { get; set; }

    // Override the OnPaint method to draw the image on the button
    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        if (this.Image != null)
        {
            pevent.Graphics.DrawImage(this.Image, this.ClientRectangle);
        }
    }

    // Override the OnMouseEnter method to change the cursor to a hand cursor
    protected override void OnMouseEnter(EventArgs e)
    {
        this.Cursor = Cursors.Hand;
        base.OnMouseEnter(e);
    }

    // Override the OnMouseLeave method to change the cursor back to the default cursor
    protected override void OnMouseLeave(EventArgs e)
    {
        this.Cursor = Cursors.Default;
        base.OnMouseLeave(e);
    }
}

namespace BULi_MAUI;

public static class PlotImageHelper
{

    public static ImageSource Render(ScottPlot.Plot plot, double widthDip, double heightDip)
    {
        double density = DeviceDisplay.MainDisplayInfo.Density;
        if (density <= 0) density = 1;

        int pxW = Math.Max(1, (int)Math.Round(widthDip * density));
        int pxH = Math.Max(1, (int)Math.Round(heightDip * density));

        plot.ScaleFactor = (float)density; // keeps fonts and ticks readable

        byte[] bytes = plot.GetImageBytes(pxW, pxH, ScottPlot.ImageFormat.Png);
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }
}

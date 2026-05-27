using System.Drawing;
using System.Runtime.Versioning;

namespace GuildfordBsac.Web.Tests;

public class TeamImageTests
{
    private const long MaxFileSizeBytes = 500 * 1024;
    private const int MinDimension = 200;
    private const int MaxDimension = 1000;

    private static string FindTeamImagesDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "wwwroot", "Content", "Images", "team");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"Could not locate wwwroot/Content/Images/team walking up from {AppContext.BaseDirectory}");
    }

    public static IEnumerable<object[]> TeamImagePaths()
    {
        var dir = FindTeamImagesDirectory();
        return Directory
            .GetFiles(dir, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            .Select(f => new object[] { f });
    }

    [Theory]
    [MemberData(nameof(TeamImagePaths))]
    [SupportedOSPlatform("windows")]
    public void TeamImage_MeetsConstraints(string imagePath)
    {
        var file = new FileInfo(imagePath);
        var name = file.Name;

        Assert.True(file.Length < MaxFileSizeBytes,
            $"{name}: {file.Length / 1024}KB exceeds the 500KB limit");

        using var img = Image.FromFile(imagePath);

        Assert.True(img.Width == img.Height,
            $"{name}: {img.Width}x{img.Height} is not square");

        Assert.True(img.Width >= MinDimension,
            $"{name}: {img.Width}px wide is below the {MinDimension}px minimum");

        Assert.True(img.Width <= MaxDimension,
            $"{name}: {img.Width}px wide exceeds the {MaxDimension}px maximum");
    }
}

using AI_Evlo_Test;
using AI_Evlo_Test.Objects;
namespace AI_Evlo_WPF.UnitTests;
[TestClass]
public class TestSessionFixture
{
    private static string directory = null!;
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AI-Evlo-tests-" + Guid.NewGuid());
        System.IO.Directory.CreateDirectory(directory);
        MainWindow.SessionDirectoryOverride = directory;
        WindowBoundsStore.FilePathOverride = System.IO.Path.Combine(directory, "window-sizes.json");
    }
    [AssemblyCleanup]
    public static void Cleanup()
    {
        MainWindow.SessionDirectoryOverride = null;
        WindowBoundsStore.FilePathOverride = null;
        if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
    }
}

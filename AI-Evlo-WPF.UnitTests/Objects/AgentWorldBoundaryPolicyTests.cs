using AI_Evlo_Test.Objects;
using System.Windows;

namespace AI_Evlo_WPF.UnitTests.Objects;

[TestClass]
public class AgentWorldBoundaryPolicyTests
{
    [TestMethod]
    public void ShouldRetire_WhenLivingAgentIsWithinFiveHundredPixelGraceArea_ReturnsFalse()
    {
        var frog = new Frog();
        frog.SetLocation(600, -500);

        bool shouldRetire = AgentWorldBoundaryPolicy.ShouldRetire(frog, 100, 100);

        Assert.IsFalse(shouldRetire);
    }

    [TestMethod]
    public void ShouldRetire_WhenLivingAgentExceedsFiveHundredPixelGraceArea_ReturnsTrue()
    {
        var frog = new Frog();
        frog.SetLocation(600.1, 50);

        bool shouldRetire = AgentWorldBoundaryPolicy.ShouldRetire(frog, 100, 100);

        Assert.IsTrue(shouldRetire);
    }

    [TestMethod]
    public void NormalizeSpawnLocation_WhenBeyondGraceArea_ClampsToExtendedWorld()
    {
        Point spawn = AgentWorldBoundaryPolicy.NormalizeSpawnLocation(
            new Point(1000, -1000),
            worldWidth: 100,
            worldHeight: 80);

        Assert.AreEqual(600, spawn.X, 0.000001);
        Assert.AreEqual(-500, spawn.Y, 0.000001);
    }
}

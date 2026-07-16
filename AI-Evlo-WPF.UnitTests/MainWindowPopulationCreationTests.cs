using AI_Evlo_Test;
using AI_Evlo_Test.ConfigLib;

namespace AI_Evlo_WPF.UnitTests;

[STATestClass]
public class MainWindowPopulationCreationTests
{
    [TestMethod]
    public void ResolvePopulationTemplateForCreation_WithCustomEditorLabel_FallsBackToSmallTemplate()
    {
        NeuroNetStructure template = MainWindow.ResolvePopulationTemplateForCreation("Custom (2 layers)");

        Assert.IsNotNull(template);
        Assert.AreEqual("Small", template.Id);
        Assert.HasCount(1, template.LayerDefinitions);
    }
}

using AI_Evlo_Test;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;

namespace AI_Evlo_WPF.UnitTests;

[STATestClass]
public class VisualizeNetworkTests
{
    [TestMethod]
    public void Status_RoundTripsThroughStatusBar()
    {
        var window = new VisualizeNetwork();

        window.Status = "Population network ready";

        Assert.AreEqual("Population network ready", window.Status);
        window.Close();
    }

    [TestMethod]
    public void ShowNNet_UpdatesDisplayedSnapshot()
    {
        var window = new VisualizeNetwork();
        INeuralNetwork network = NeuralNetworkFactory.GetInstance().Create(3, 2, 1, 4);

        window.ShowNNet(network);

        Assert.AreSame(network, window.DisplayedNetwork);
        Assert.AreEqual("Neural network graph rendered.", window.Status);
        window.Close();
    }

    [TestMethod]
    public void NeuralNetworkView_FilterIsClampedAndFitPreservesNetwork()
    {
        INeuralNetwork network = NeuralNetworkFactory.GetInstance().Create(3, 2, 1, 4);
        var view = new NeuralNetworkView();

        view.SetSnapshot(network, new[] { true, false });
        view.MinimumAbsoluteWeight = -1;
        view.FitToView();

        Assert.AreSame(network, view.Network);
        Assert.AreEqual(0, view.MinimumAbsoluteWeight);
    }
}

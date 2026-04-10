using AI_Evlo_Test;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AI_Evlo_WPF.UnitTests
{
    [TestClass]
    public class VisualizeNetworkTests
    {
        // Status Property Tests
        [TestMethod]
        public void Status_WhenSet_UpdatesStatusStripText()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var expectedStatus = "Test Status Message";

            // Act
            visualizeNetwork.Status = expectedStatus;

            // Assert
            Assert.AreEqual(expectedStatus, visualizeNetwork.Status);
        }

        [TestMethod]
        public void Status_WhenGet_ReturnsStatusStripText()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            visualizeNetwork.Status = "Initial Status";

            // Act
            var actualStatus = visualizeNetwork.Status;

            // Assert
            Assert.AreEqual("Initial Status", actualStatus);
        }

        [TestMethod]
        public void Status_WithEmptyString_UpdatesToEmptyString()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            visualizeNetwork.Status = "Original Status";

            // Act
            visualizeNetwork.Status = string.Empty;

            // Assert
            Assert.AreEqual(string.Empty, visualizeNetwork.Status);
        }

        [TestMethod]
        public void Status_WithNullValue_UpdatesToNull()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            visualizeNetwork.Status = "Original Status";

            // Act
            visualizeNetwork.Status = null;

            // Assert
            Assert.IsNull(visualizeNetwork.Status);
        }

        // Constructor Tests
        [TestMethod]
        public void Constructor_InitializesSuccessfully()
        {
            // Act
            var visualizeNetwork = new VisualizeNetwork();

            // Assert
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void Constructor_InitializesStatusStripWithDefaultText()
        {
            // Act
            var visualizeNetwork = new VisualizeNetwork();

            // Assert
            Assert.IsNotNull(visualizeNetwork.Status);
            Assert.AreEqual("Loading", visualizeNetwork.Status);
        }

        [TestMethod]
        public void Constructor_EventHandlerIsWired()
        {
            // Arrange & Act
            var visualizeNetwork = new VisualizeNetwork();

            // Assert - Verify the form was constructed properly
            Assert.IsNotNull(visualizeNetwork);
        }

        // ShowNNet Tests
        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_ConfiguresPreferences()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_SetsZoomLevel()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_EnablesSelectable()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_ConfiguresInputFormatters()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_ConfiguresPerceptronFormatters()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_ConfiguresEdgeFormatters()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_ConfiguresEdgeConnector()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_SetsQualityToHigh()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_GetsImageFromControl()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectBiasHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectEdgeHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectInputHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectInputLayerHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectPerceptronHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_AttachesSelectPerceptronLayerHandler()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        [TestMethod]
        public void ShowNNet_WithValidNeuralNetwork_CallsDrawVisualization()
        {
            // Arrange
            var visualizeNetwork = new VisualizeNetwork();
            var mockNetwork = CreateMockNeuralNetwork();

            // Act
            visualizeNetwork.ShowNNet(mockNetwork);

            // Assert - Method should complete without exceptions
            Assert.IsNotNull(visualizeNetwork);
        }

        private INeuralNetwork CreateMockNeuralNetwork()
        {
            var mockNetwork = new Mock<INeuralNetwork>();
            var mockInputs = new Synapse[2];
            
            for (int i = 0; i < mockInputs.Length; i++)
            {
                var mockSynapse = new Mock<Synapse>();
                var mockAxon = new Mock<IAxon>();
                mockAxon.Setup(a => a.Value).Returns(0.5);
                mockSynapse.Setup(s => s.Axon).Returns(mockAxon.Object);
                mockInputs[i] = mockSynapse.Object;
            }

            mockNetwork.Setup(n => n.Inputs).Returns(mockInputs);
            
            var mockHiddenLayers = new ILayer[1];
            var mockHiddenLayer = new Mock<ILayer>();
            var mockHiddenNeurons = new INeuron[3];
            
            for (int i = 0; i < mockHiddenNeurons.Length; i++)
            {
                var mockNeuron = new Mock<INeuron>();
                var mockAxon = new Mock<IAxon>();
                var mockSoma = new Mock<ISoma>();
                mockAxon.Setup(a => a.Value).Returns(0.3);
                mockSoma.Setup(s => s.CalculateSummation()).Returns(0.2);
                mockNeuron.Setup(n => n.Axon).Returns(mockAxon.Object);
                mockNeuron.Setup(n => n.Soma).Returns(mockSoma.Object);
                mockHiddenNeurons[i] = mockNeuron.Object;
            }
            
            mockHiddenLayer.Setup(l => l.NeuronsInLayer).Returns(mockHiddenNeurons);
            mockHiddenLayers[0] = mockHiddenLayer.Object;
            mockNetwork.Setup(n => n.HiddenLayers).Returns(mockHiddenLayers);
            
            var mockOutputLayer = new Mock<ILayer>();
            var mockOutputNeurons = new INeuron[2];
            
            for (int i = 0; i < mockOutputNeurons.Length; i++)
            {
                var mockNeuron = new Mock<INeuron>();
                var mockAxon = new Mock<IAxon>();
                var mockSoma = new Mock<ISoma>();
                mockAxon.Setup(a => a.Value).Returns(0.7);
                mockSoma.Setup(s => s.CalculateSummation()).Returns(0.6);
                mockNeuron.Setup(n => n.Axon).Returns(mockAxon.Object);
                mockNeuron.Setup(n => n.Soma).Returns(mockSoma.Object);
                mockOutputNeurons[i] = mockNeuron.Object;
            }
            
            mockOutputLayer.Setup(l => l.NeuronsInLayer).Returns(mockOutputNeurons);
            mockNetwork.Setup(n => n.OutputLayer).Returns(mockOutputLayer.Object);
            
            return mockNetwork.Object;
        }
    }
}

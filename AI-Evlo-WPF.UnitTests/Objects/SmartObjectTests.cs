using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class SmartObjectTests
    {
        [TestMethod]
        public void Fitness_WhenCyclesAndOfspringsSet_ReturnsDifference()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 100,
                Ofsprings = 30
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(70.0, fitness);
        }

        [TestMethod]
        public void Fitness_WhenOfspringsGreaterThanCycles_ReturnsNegativeValue()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 50,
                Ofsprings = 80
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(-30.0, fitness);
        }

        [TestMethod]
        public void Fitness_WhenCyclesAndOfspringsZero_ReturnsZero()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 0,
                Ofsprings = 0
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(0.0, fitness);
        }

        [TestMethod]
        public void HP_SetValidValue_ReturnsValue()
        {
            // Arrange
            var smartObject = new SmartObject();
            double expectedValue = 150.0;

            // Act
            smartObject.HP = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetValueAboveMaxHp_ClampsToMaxHp()
        {
            // Arrange
            var smartObject = new SmartObject();
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            smartObject.HP = 500.0;

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void HP_SetValueBelowZero_ClampsToZero()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            smartObject.HP = -50.0;

            // Assert
            Assert.AreEqual(0.0, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetZero_ReturnsZero()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            smartObject.HP = 0.0;

            // Assert
            Assert.AreEqual(0.0, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetMaxHpBoundary_ReturnsMaxHp()
        {
            // Arrange
            var smartObject = new SmartObject();
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            smartObject.HP = 300.0;

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void CachedInputs_WhenNull_CreatesNewArrayWithCorrectLength()
        {
            // Arrange
            var smartObject = new SmartObject();
            int expectedLength = smartObject.Perception.Signals.Length + 2;

            // Act
            var cachedInputs = smartObject.CachedInputs;

            // Assert
            Assert.IsNotNull(cachedInputs);
            Assert.HasCount(expectedLength, cachedInputs);
        }

        [TestMethod]
        public void CachedInputs_WhenAccessedMultipleTimes_ReturnsSameInstance()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            var firstAccess = smartObject.CachedInputs;
            var secondAccess = smartObject.CachedInputs;

            // Assert
            Assert.AreSame(firstAccess, secondAccess);
        }

        [TestMethod]
        public void Act_WhenNNetworkIsNull_ReturnsEmptyArray()
        {
            // Arrange
            var smartObject = new SmartObject();
            smartObject.NNetwork = null;
            double[] inputs = new double[5];

            // Act
            var result = smartObject.Act(inputs);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Act_WhenNNetworkIsNull_IncrementsOnlyCycles()
        {
            // Arrange
            var smartObject = new SmartObject();
            smartObject.NNetwork = null;
            smartObject.Cycles = 0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(1, smartObject.Cycles);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_IncrementsCycles()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Cycles = 10;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(11, smartObject.Cycles);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsSetInputs()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.SetInputs(inputs), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsProcess()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.Process(), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsGetOutputs()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.GetOutputs(), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_ReturnsOutputs()
        {
            // Arrange
            var expectedOutputs = new double[] { 0.5, 0.5 };
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(expectedOutputs);

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            var result = smartObject.Act(inputs);

            // Assert
            Assert.AreSame(expectedOutputs, result);
        }

        [TestMethod]
        public void Act_WithHighOutputs_DrainsStamina()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 1.0, 1.0 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Stamina = 200.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.IsLessThan(smartObject.Stamina, 200.0);
        }

        [TestMethod]
        public void Act_WhenStaminaBelowZero_ClampsToZero()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 1.0, 1.0 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Stamina = 0.1;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(0.0, smartObject.Stamina);
        }

        [TestMethod]
        public void Act_WithZeroStamina_SetsLastSpeedToZero()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Stamina = 0.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(0.0, smartObject.LastSpeed);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_RegeneratesStamina()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.0 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Stamina = 100.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.IsGreaterThan(smartObject.Stamina, 100.0);
        }

        [TestMethod]
        public void Act_StaminaRegeneration_DoesNotExceedMaxStamina()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.0 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double originalMaxStamina = SmartObject.MaxStamina;
            SmartObject.MaxStamina = 200;
            smartObject.Stamina = 199.9;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(200.0, smartObject.Stamina);

            // Cleanup
            SmartObject.MaxStamina = originalMaxStamina;
        }

        [TestMethod]
        public void Act_WithNegativeOutputs_DrainsStaminaUsingAbsoluteValues()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { -1.0, -1.0 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Stamina = 200.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.IsLessThan(smartObject.Stamina, 200.0);
        }

        [TestMethod]
        public void Act_WithZeroMaxStamina_SetsStaminaFractionToZero()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double originalMaxStamina = SmartObject.MaxStamina;
            SmartObject.MaxStamina = 0;
            smartObject.Stamina = 100.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(0.0, smartObject.LastSpeed);

            // Cleanup
            SmartObject.MaxStamina = originalMaxStamina;
        }

        [TestMethod]
        public void Act_WithFullStamina_MovesAtFullSpeed()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double originalMaxStamina = SmartObject.MaxStamina;
            SmartObject.MaxStamina = 200;
            smartObject.Stamina = 200.0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.IsGreaterThan(smartObject.LastSpeed, 0.0);

            // Cleanup
            SmartObject.MaxStamina = originalMaxStamina;
        }

        [TestMethod]
        public void Constructor_InitializesHPToMaxHp()
        {
            // Arrange
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void Constructor_InitializesPerceptionWithCenterRayMultiplier()
        {
            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.IsNotNull(smartObject.Perception);
        }

        [TestMethod]
        public void Constructor_InitializesPerception_WithSignalsArray()
        {
            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.IsNotNull(smartObject.Perception.Signals);
            Assert.IsNotEmpty(smartObject.Perception.Signals);
        }
    }
}

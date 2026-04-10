using AI_Evlo_Test.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class TargetTests
    {
        [TestMethod]
        public void Underwater_SetValidValue_ReturnsValue()
        {
            // Arrange
            var target = new TargetObj();
            double expectedValue = 50.0;

            // Act
            target.Underwater = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetValueBelowMinimum_ClampsToMinusTen()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = -15.0;

            // Assert
            Assert.AreEqual(-10.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetValueAboveMaximum_ClampsToHundred()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 150.0;

            // Assert
            Assert.AreEqual(100.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetMinimumBoundary_ReturnsMinusTen()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = -10.0;

            // Assert
            Assert.AreEqual(-10.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetMaximumBoundary_ReturnsHundred()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 100.0;

            // Assert
            Assert.AreEqual(100.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetZero_ReturnsZero()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 0.0;

            // Assert
            Assert.AreEqual(0.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetNegativeValueWithinRange_ReturnsValue()
        {
            // Arrange
            var target = new TargetObj();
            double expectedValue = -5.0;

            // Act
            target.Underwater = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetJustAboveMinimum_ReturnsValue()
        {
            // Arrange
            var target = new TargetObj();
            double expectedValue = -9.99;

            // Act
            target.Underwater = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetJustBelowMaximum_ReturnsValue()
        {
            // Arrange
            var target = new TargetObj();
            double expectedValue = 99.99;

            // Act
            target.Underwater = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetJustBelowMinimum_ClampsToMinusTen()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = -10.01;

            // Assert
            Assert.AreEqual(-10.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetJustAboveMaximum_ClampsToHundred()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 100.01;

            // Assert
            Assert.AreEqual(100.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_DefaultValue_ReturnsZero()
        {
            // Arrange & Act
            var target = new TargetObj();

            // Assert
            Assert.AreEqual(0.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetMultipleTimes_ReturnsLastValidValue()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 25.0;
            target.Underwater = 75.0;

            // Assert
            Assert.AreEqual(75.0, target.Underwater);
        }

        [TestMethod]
        public void Underwater_SetMultipleTimesWithClamping_ReturnsLastClampedValue()
        {
            // Arrange
            var target = new TargetObj();

            // Act
            target.Underwater = 150.0;
            target.Underwater = -20.0;

            // Assert
            Assert.AreEqual(-10.0, target.Underwater);
        }
    }
}

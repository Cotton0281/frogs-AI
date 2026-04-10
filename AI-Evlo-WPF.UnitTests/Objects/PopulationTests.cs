using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class PopulationTests
    {
        [TestMethod]
        public void Population_Add_AddsISmartObjectToMembers()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
            Assert.AreSame(mockSmartObject.Object, population.Members[0]);
        }

        [TestMethod]
        public void Population_Add_IncrementsTotalMembersCount()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.AreEqual(1, population.TotalMembersCount);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsNull_DoesNotThrow()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            mockSmartObject.Setup(m => m.VisibleShape).Returns((System.Windows.FrameworkElement)null!);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsShape_SetsShapeFillToPopulationColorBrush()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            var shape = new Rectangle();
            mockSmartObject.Setup(m => m.VisibleShape).Returns(shape);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.AreSame(population.PopulationColorBrush, shape.Fill);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsNotShape_DoesNotSetFill()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            var mockFrameworkElement = new Mock<System.Windows.FrameworkElement>();
            mockSmartObject.Setup(m => m.VisibleShape).Returns(mockFrameworkElement.Object);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
        }

        [TestMethod]
        public void Population_Add_MultipleMembers_IncrementsCountCorrectly()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject1 = new Mock<ISmartObject>();
            var mockSmartObject2 = new Mock<ISmartObject>();
            var mockSmartObject3 = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject1.Object);
            population.Add(mockSmartObject2.Object);
            population.Add(mockSmartObject3.Object);

            // Assert
            Assert.HasCount(3, population.Members);
            Assert.AreEqual(3, population.TotalMembersCount);
        }

        [TestMethod]
        public void Population_ToString_ReturnsName()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation"
            };

            // Act
            var result = population.ToString();

            // Assert
            Assert.AreEqual("TestPopulation", result);
        }

        [TestMethod]
        public void Population_ToString_WhenNameIsDefault_ReturnsDefaultName()
        {
            // Arrange
            var population = new Population();

            // Act
            var result = population.ToString();

            // Assert
            Assert.AreEqual("PopulationX", result);
        }

        [TestMethod]
        public void Population_ToJson_ReturnsJsonString()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation",
                TotalMembersCount = 5,
                SizeLimit = 10
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.IsNotNull(result);
            Assert.Contains("\"Name\":\"TestPopulation\"", result);
        }

        [TestMethod]
        public void Population_ToJson_ContainsExpectedProperties()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation",
                TotalMembersCount = 5
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.Contains("Name", result);
            Assert.Contains("TotalMembersCount", result);
        }

        [TestMethod]
        public void Population_Add_WithMultipleShapes_SetsAllFillsCorrectly()
        {
            // Arrange
            var population = new Population();
            var customBrush = new SolidColorBrush(Colors.Red);
            population.PopulationColorBrush = customBrush;

            var mockSmartObject1 = new Mock<ISmartObject>();
            var shape1 = new Ellipse();
            mockSmartObject1.Setup(m => m.VisibleShape).Returns(shape1);

            var mockSmartObject2 = new Mock<ISmartObject>();
            var shape2 = new Rectangle();
            mockSmartObject2.Setup(m => m.VisibleShape).Returns(shape2);

            // Act
            population.Add(mockSmartObject1.Object);
            population.Add(mockSmartObject2.Object);

            // Assert
            Assert.AreSame(customBrush, shape1.Fill);
            Assert.AreSame(customBrush, shape2.Fill);
        }

        [TestMethod]
        public void Population_Being_DefaultsToFrog()
        {
            // Arrange
            var population = new Population();

            // Act
            var result = population.Being;

            // Assert
            Assert.AreEqual(PopulationBeing.Frog, result);
        }

        [TestMethod]
        public void Population_ToJson_ContainsBeing()
        {
            // Arrange
            var population = new Population
            {
                Name = "BirdPopulation",
                Being = PopulationBeing.Bird
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.Contains("\"Being\":1", result);
        }
    }
}

using AI_Evlo_Test.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Windows;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class BasicObjectsTests
    {
        // Location Property Tests
        [TestMethod]
        public void Location_WhenCreated_ReturnsDefaultPoint()
        {
            // Arrange
            var basicObject = new BasicObject();

            // Act
            var location = basicObject.Location;

            // Assert
            Assert.AreEqual(0, location.X);
            Assert.AreEqual(0, location.Y);
        }

        [TestMethod]
        public void Location_AfterSetLocation_ReturnsUpdatedPoint()
        {
            // Arrange
            var basicObject = new BasicObject();
            basicObject.SetLocation(10, 20);

            // Act
            var location = basicObject.Location;

            // Assert
            Assert.AreEqual(10, location.X);
            Assert.AreEqual(20, location.Y);
        }

        // VisibleShape Property Tests
        [TestMethod]
        public void VisibleShape_WhenCreated_ReturnsNull()
        {
            // Arrange
            var basicObject = new BasicObject();

            // Act
            var visibleShape = basicObject.VisibleShape;

            // Assert
            Assert.IsNull(visibleShape);
        }

        [TestMethod]
        public void VisibleShape_WhenSet_ReturnsSetValue()
        {
            // Arrange
            var basicObject = new BasicObject();
            var mockShape = new Mock<FrameworkElement>();

            // Act
            basicObject.VisibleShape = mockShape.Object;

            // Assert
            Assert.AreSame(mockShape.Object, basicObject.VisibleShape);
        }

        [TestMethod]
        public void VisibleShape_WhenSetToNull_ReturnsNull()
        {
            // Arrange
            var basicObject = new BasicObject();
            var mockShape = new Mock<FrameworkElement>();
            basicObject.VisibleShape = mockShape.Object;

            // Act
            basicObject.VisibleShape = null;

            // Assert
            Assert.IsNull(basicObject.VisibleShape);
        }

        // OnLocationChanged Event Tests
        [TestMethod]
        public void OnLocationChanged_WhenSubscribedThroughInterface_AddsHandler()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            bool eventRaised = false;
            Point? receivedLocation = null;

            LocationChanged_Handler handler = (obj, loc) =>
            {
                eventRaised = true;
                receivedLocation = loc;
            };

            // Act
            basicObject.OnLocationChanged += handler;
            basicObject.SetLocation(5, 10);

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.IsNotNull(receivedLocation);
            Assert.AreEqual(5, receivedLocation.Value.X);
            Assert.AreEqual(10, receivedLocation.Value.Y);
        }

        [TestMethod]
        public void OnLocationChanged_WhenUnsubscribedThroughInterface_RemovesHandler()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            int eventCallCount = 0;

            LocationChanged_Handler handler = (obj, loc) =>
            {
                eventCallCount++;
            };

            basicObject.OnLocationChanged += handler;
            basicObject.SetLocation(5, 10);

            // Act
            basicObject.OnLocationChanged -= handler;
            basicObject.SetLocation(15, 20);

            // Assert
            Assert.AreEqual(1, eventCallCount);
        }

        [TestMethod]
        public void OnLocationChanged_WhenMultipleHandlers_InvokesAllHandlers()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            int handler1CallCount = 0;
            int handler2CallCount = 0;

            LocationChanged_Handler handler1 = (obj, loc) => { handler1CallCount++; };
            LocationChanged_Handler handler2 = (obj, loc) => { handler2CallCount++; };

            // Act
            basicObject.OnLocationChanged += handler1;
            basicObject.OnLocationChanged += handler2;
            basicObject.SetLocation(5, 10);

            // Assert
            Assert.AreEqual(1, handler1CallCount);
            Assert.AreEqual(1, handler2CallCount);
        }

        [TestMethod]
        public void OnLocationChanged_WhenLocationNotChanged_DoesNotInvokeHandler()
        {
            // Arrange
            var basicObject = new BasicObject();
            IBasicObject iBasicObject = basicObject;
            int eventCallCount = 0;

            LocationChanged_Handler handler = (obj, loc) =>
            {
                eventCallCount++;
            };

            iBasicObject.OnLocationChanged += handler;
            basicObject.SetLocation(5, 10);

            // Act
            basicObject.SetLocation(5, 10);

            // Assert
            Assert.AreEqual(1, eventCallCount);
        }

        [TestMethod]
        public void OnLocationChanged_PassesCorrectSenderAndLocation()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            IBasicObject? receivedSender = null;
            Point? receivedLocation = null;

            LocationChanged_Handler handler = (obj, loc) =>
            {
                receivedSender = obj;
                receivedLocation = loc;
            };

            // Act
            basicObject.OnLocationChanged += handler;
            basicObject.SetLocation(15, 25);

            // Assert
            Assert.IsNotNull(receivedSender);
            Assert.AreSame(basicObject, receivedSender);
            Assert.IsNotNull(receivedLocation);
            Assert.AreEqual(15, receivedLocation.Value.X);
            Assert.AreEqual(25, receivedLocation.Value.Y);
        }

        // SetLocation(Point) Method Tests
        [TestMethod]
        public void SetLocationPoint_WithNewLocation_UpdatesLocation()
        {
            // Arrange
            var basicObject = new BasicObject();
            var newPoint = new Point(30, 40);

            // Act
            basicObject.SetLocation(newPoint);

            // Assert
            Assert.AreEqual(30, basicObject.Location.X);
            Assert.AreEqual(40, basicObject.Location.Y);
        }

        [TestMethod]
        public void SetLocationPoint_TriggersOnLocationChangedEvent()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            bool eventRaised = false;
            var newPoint = new Point(50, 60);

            basicObject.OnLocationChanged += (obj, loc) => { eventRaised = true; };

            // Act
            basicObject.SetLocation(newPoint);

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void SetLocationPoint_WithSameLocation_DoesNotTriggerEvent()
        {
            // Arrange
            var basicObject = new BasicObject();
            IBasicObject iBasicObject = basicObject;
            int eventCallCount = 0;

            basicObject.SetLocation(10, 20);
            iBasicObject.OnLocationChanged += (obj, loc) => { eventCallCount++; };

            // Act
            basicObject.SetLocation(new Point(10, 20));

            // Assert
            Assert.AreEqual(0, eventCallCount);
        }

        // SetLocation(double, double) Method Tests
        [TestMethod]
        public void SetLocationDoubles_WithNewCoordinates_UpdatesLocation()
        {
            // Arrange
            var basicObject = new BasicObject();

            // Act
            basicObject.SetLocation(100, 200);

            // Assert
            Assert.AreEqual(100, basicObject.Location.X);
            Assert.AreEqual(200, basicObject.Location.Y);
        }

        [TestMethod]
        public void SetLocationDoubles_TriggersOnLocationChangedEvent()
        {
            // Arrange
            IBasicObject basicObject = new BasicObject();
            bool eventRaised = false;

            basicObject.OnLocationChanged += (obj, loc) => { eventRaised = true; };

            // Act
            basicObject.SetLocation(75, 85);

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void SetLocationDoubles_WithSameCoordinates_DoesNotTriggerEvent()
        {
            // Arrange
            var basicObject = new BasicObject();
            IBasicObject iBasicObject = basicObject;
            int eventCallCount = 0;

            basicObject.SetLocation(50, 60);
            iBasicObject.OnLocationChanged += (obj, loc) => { eventCallCount++; };

            // Act
            basicObject.SetLocation(50, 60);

            // Assert
            Assert.AreEqual(0, eventCallCount);
        }

        [TestMethod]
        public void SetLocationDoubles_WithChangedXOnly_TriggersEvent()
        {
            // Arrange
            var basicObject = new BasicObject();
            IBasicObject iBasicObject = basicObject;
            int eventCallCount = 0;

            basicObject.SetLocation(10, 20);
            iBasicObject.OnLocationChanged += (obj, loc) => { eventCallCount++; };

            // Act
            basicObject.SetLocation(15, 20);

            // Assert
            Assert.AreEqual(1, eventCallCount);
            Assert.AreEqual(15, basicObject.Location.X);
            Assert.AreEqual(20, basicObject.Location.Y);
        }

        [TestMethod]
        public void SetLocationDoubles_WithChangedYOnly_TriggersEvent()
        {
            // Arrange
            var basicObject = new BasicObject();
            IBasicObject iBasicObject = basicObject;
            int eventCallCount = 0;

            basicObject.SetLocation(10, 20);
            iBasicObject.OnLocationChanged += (obj, loc) => { eventCallCount++; };

            // Act
            basicObject.SetLocation(10, 25);

            // Assert
            Assert.AreEqual(1, eventCallCount);
            Assert.AreEqual(10, basicObject.Location.X);
            Assert.AreEqual(25, basicObject.Location.Y);
        }

        [TestMethod]
        public void SetLocationDoubles_WithNegativeCoordinates_UpdatesLocation()
        {
            // Arrange
            var basicObject = new BasicObject();

            // Act
            basicObject.SetLocation(-10, -20);

            // Assert
            Assert.AreEqual(-10, basicObject.Location.X);
            Assert.AreEqual(-20, basicObject.Location.Y);
        }

        [TestMethod]
        public void SetLocationDoubles_WithZeroCoordinates_UpdatesLocation()
        {
            // Arrange
            var basicObject = new BasicObject();
            basicObject.SetLocation(10, 20);

            // Act
            basicObject.SetLocation(0, 0);

            // Assert
            Assert.AreEqual(0, basicObject.Location.X);
            Assert.AreEqual(0, basicObject.Location.Y);
        }
    }
}

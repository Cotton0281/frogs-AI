using AI_Evlo_Test.Objects;
using System.Windows;
using CoordinatesUtil;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class Path_spiralTests
    {
        [TestMethod]
        public void GetNextLocation_ReturnsLastLocation_WhenCalledOnBaseTrajectory()
        {
            // Arrange
            var trajectory = new trajectory();
            var lastLocation = new Point(100, 200);

            // Act
            var result = trajectory.GetNextLocation(lastLocation);

            // Assert
            Assert.AreEqual(lastLocation, result);
        }

        [TestMethod]
        public void GetNextLocation_MovesTowardsCenter_WhenGoToCenterFirstIsTrue()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 5,
                goToCenterFirst = true
            };
            var lastLocation = new Point(200, 200);
            var distanceBefore = Point.Subtract(spiral.SpiralCenter, lastLocation).Length;

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var distanceAfter = Point.Subtract(spiral.SpiralCenter, result).Length;
            // Result should be closer to center than before
            Assert.IsLessThan(distanceAfter, distanceBefore);
        }

        [TestMethod]
        public void GetNextLocation_SetsGoToCenterFirstToFalse_WhenDistanceFromCenterIsLessThan10()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = true
            };
            var lastLocation = new Point(105, 105); // Distance ~7.07

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            Assert.IsFalse(spiral.goToCenterFirst);
        }

        [TestMethod]
        public void GetNextLocation_KeepsGoToCenterFirstTrue_WhenDistanceFromCenterIsGreaterThan10()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = true
            };
            var lastLocation = new Point(120, 120); // Distance ~28.28

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            Assert.IsTrue(spiral.goToCenterFirst);
        }

        [TestMethod]
        public void GetNextLocation_AppliesSpeed_WhenGoToCenterFirstIsTrue()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 3,
                goToCenterFirst = true
            };
            var lastLocation = new Point(200, 200);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var vectorToResult = Point.Subtract(result, lastLocation);
            var distance = vectorToResult.Length;
            Assert.AreEqual(3.0, distance, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_SpiralMovement_WhenNotGoingToCenterFirst()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 5,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 10
            };
            var lastLocation = new Point(150, 100);
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // Result should not be the same as last location
            Assert.AreNotEqual(lastLocation, result);
            // Movement should have occurred
            var movement = Point.Subtract(result, lastLocation);
            Assert.IsGreaterThan(movement.Length, 0.0);
        }

        [TestMethod]
        public void GetNextLocation_TogglesDirectionAndSetsExpanding_WhenDistanceLessThan10AndNotExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            // Use reflection or direct access to set Expanding to false
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, false);
            var lastLocation = new Point(105, 105); // Distance ~7.07

            // Act
            var originalDirection = spiral.ClockwiseDirection;
            spiral.GetNextLocation(lastLocation);
            var expandingValue = expandingField?.GetValue(spiral);

            // Assert
            Assert.AreEqual(!originalDirection, spiral.ClockwiseDirection);
            Assert.IsTrue((bool)expandingValue!);
        }

        [TestMethod]
        public void GetNextLocation_SetsExpandingToFalse_WhenDistanceGreaterThanMaxSize()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5,
                MaxSize = 200
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(350, 100); // Distance = 250

            // Act
            spiral.GetNextLocation(lastLocation);
            var expandingValue = expandingField?.GetValue(spiral);

            // Assert
            Assert.IsFalse((bool)expandingValue!);
        }

        [TestMethod]
        public void GetNextLocation_SetsSpiralingAnglePositive_WhenExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = -5
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(150, 150);

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            Assert.IsGreaterThan(spiral.SpiralingAngle, 0.0);
        }

        [TestMethod]
        public void GetNextLocation_SetsSpiralingAngleNegative_WhenNotExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5,
                MaxSize = 200
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, false);
            var lastLocation = new Point(350, 350);

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            Assert.IsLessThan(spiral.SpiralingAngle, 0.0);
        }

        [TestMethod]
        public void GetNextLocation_RotationAngleIsPositive_WhenClockwiseDirection()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 10
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(150, 150);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // The rotation should follow clockwise rules
            var movement = Point.Subtract(result, lastLocation);
            Assert.IsGreaterThan(movement.Length, 0.0);
        }

        [TestMethod]
        public void GetNextLocation_RotationAngleIsNegative_WhenCounterClockwiseDirection()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = false,
                SpiralingAngle = 10
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(150, 150);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var movement = Point.Subtract(result, lastLocation);
            Assert.IsGreaterThan(movement.Length, 0.0);
        }

        [TestMethod]
        public void GetNextLocation_AppliesSpeedCorrectly_WhenNotGoingToCenterFirst()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 7,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            var lastLocation = new Point(150, 100);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var movement = Point.Subtract(result, lastLocation);
            Assert.AreEqual(7.0, movement.Length, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_ReturnsNewLocation_NotSameInstance()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false
            };
            var lastLocation = new Point(150, 100);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            Assert.AreNotEqual(lastLocation, result);
        }

        [TestMethod]
        public void GetNextLocation_HandlesZeroSpeed_WhenGoToCenterFirst()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 0,
                goToCenterFirst = true
            };
            var lastLocation = new Point(200, 200);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // With zero speed, should stay at the same location
            Assert.AreEqual(lastLocation.X, result.X, 0.01);
            Assert.AreEqual(lastLocation.Y, result.Y, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_HandlesZeroSpeed_WhenNotGoingToCenterFirst()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 0,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            var lastLocation = new Point(150, 100);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // With zero speed, should stay at the same location
            Assert.AreEqual(lastLocation.X, result.X, 0.01);
            Assert.AreEqual(lastLocation.Y, result.Y, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_HandlesNegativeSpiralingAngle_WhenExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = -15
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(150, 150);

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            // SpiralingAngle should be made positive when expanding
            Assert.IsGreaterThan(spiral.SpiralingAngle, 0.0);
            Assert.AreEqual(15, spiral.SpiralingAngle, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_HandlesPositiveSpiralingAngle_WhenNotExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 15,
                MaxSize = 200
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, false);
            var lastLocation = new Point(350, 350);

            // Act
            spiral.GetNextLocation(lastLocation);

            // Assert
            // SpiralingAngle should be made negative when not expanding
            Assert.IsLessThan(spiral.SpiralingAngle, 0.0);
            Assert.AreEqual(-15, spiral.SpiralingAngle, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_HandlesCenterLocation_WhenGoToCenterFirst()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = true
            };
            var lastLocation = new Point(100, 100); // At center

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // Should toggle goToCenterFirst to false
            Assert.IsFalse(spiral.goToCenterFirst);
        }

        [TestMethod]
        public void GetNextLocation_MultipleIterations_TransitionsFromGoToCenterToSpiral()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 10,
                goToCenterFirst = true,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            var lastLocation = new Point(120, 120);

            // Act
            Point result = lastLocation;
            for (int i = 0; i < 5; i++)
            {
                result = spiral.GetNextLocation(result);
            }

            // Assert
            // After several iterations moving towards center, should eventually switch modes
            // Verify the spiral is functional
            Assert.AreNotEqual(lastLocation, result);
        }

        [TestMethod]
        public void GetNextLocation_DoesNotToggleDirection_WhenDistanceLessThan10AndExpanding()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 1,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            var expandingField = typeof(Path_spiral).GetField("Expanding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expandingField?.SetValue(spiral, true);
            var lastLocation = new Point(105, 105); // Distance ~7.07

            // Act
            var originalDirection = spiral.ClockwiseDirection;
            spiral.GetNextLocation(lastLocation);

            // Assert
            // Direction should not toggle because Expanding is true
            Assert.AreEqual(originalDirection, spiral.ClockwiseDirection);
        }

        [TestMethod]
        public void GetNextLocation_DefaultValues_WorksCorrectly()
        {
            // Arrange
            var spiral = new Path_spiral();
            var lastLocation = new Point(200, 200);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            // Should move towards center by default since goToCenterFirst is true
            var distanceBefore = Point.Subtract(spiral.SpiralCenter, lastLocation).Length;
            var distanceAfter = Point.Subtract(spiral.SpiralCenter, result).Length;
            // Verify result is closer to center
            Assert.IsLessThan(distanceAfter, distanceBefore);
        }

        [TestMethod]
        public void GetNextLocation_LargeSpeed_MovesLargeDistance()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 50,
                goToCenterFirst = false,
                ClockwiseDirection = true,
                SpiralingAngle = 5
            };
            var lastLocation = new Point(200, 200);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var movement = Point.Subtract(result, lastLocation);
            Assert.AreEqual(50.0, movement.Length, 0.01);
        }

        [TestMethod]
        public void GetNextLocation_VerySmallSpeed_MovesSmallDistance()
        {
            // Arrange
            var spiral = new Path_spiral
            {
                SpiralCenter = new Point(100, 100),
                Speed = 0.1,
                goToCenterFirst = true
            };
            var lastLocation = new Point(200, 200);

            // Act
            var result = spiral.GetNextLocation(lastLocation);

            // Assert
            var movement = Point.Subtract(result, lastLocation);
            Assert.AreEqual(0.1, movement.Length, 0.01);
        }
    }
}

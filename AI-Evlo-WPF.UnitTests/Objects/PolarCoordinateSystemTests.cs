using System;
using System.Windows;
using CoordinatesUtil;

namespace AI_Evlo_WPF.UnitTests.Objects;

[TestClass]
public class PolarCoordinateSystemTests
{
    [TestMethod]
    public void AngleToXAxis_PointDirectlyRight_Returns0Degrees()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(0.0, angle, 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_PointDirectlyUp_Returns90Degrees()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(0, -10);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(90.0, angle, 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_PointDirectlyLeft_Returns180Degrees()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(-10, 0);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(180.0, Math.Abs(angle), 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_PointDirectlyDown_ReturnsNegative90Degrees()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(0, 10);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(-90.0, angle, 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_Point45DegreesUpperRight_Returns45Degrees()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(10, -10);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(45.0, angle, 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_NonOriginStart_CalculatesCorrectly()
    {
        // Arrange
        var start = new Point(5, 5);
        var end = new Point(15, 5);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(0.0, angle, 0.001);
    }

    [TestMethod]
    public void AngleToXAxis_SamePoints_Returns0()
    {
        // Arrange
        var start = new Point(10, 10);
        var end = new Point(10, 10);

        // Act
        var angle = PolarCoordinateSystem.AngleToXAxis(start, end);

        // Assert
        Assert.AreEqual(0.0, angle, 0.001);
    }

    [TestMethod]
    public void CartesianToPolarCoordinates_PointRight_ReturnsCorrectPolarLocation()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        // Act
        var polar = PolarCoordinateSystem.CartesianToPolarCoordinates(start, end);

        // Assert
        Assert.AreEqual(10.0, polar.Distance, 0.001);
        Assert.AreEqual(0.0, polar.Angle, 0.001);
    }

    [TestMethod]
    public void CartesianToPolarCoordinates_PointUp_ReturnsCorrectPolarLocation()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(0, -10);

        // Act
        var polar = PolarCoordinateSystem.CartesianToPolarCoordinates(start, end);

        // Assert
        Assert.AreEqual(10.0, polar.Distance, 0.001);
        Assert.AreEqual(90.0, polar.Angle, 0.001);
    }

    [TestMethod]
    public void CartesianToPolarCoordinates_Diagonal_ReturnsCorrectPolarLocation()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(3, -4);

        // Act
        var polar = PolarCoordinateSystem.CartesianToPolarCoordinates(start, end);

        // Assert
        Assert.AreEqual(5.0, polar.Distance, 0.001);
        Assert.AreEqual(53.130, polar.Angle, 0.001);
    }

    [TestMethod]
    public void CartesianToPolarCoordinates_NonOriginStart_ReturnsCorrectPolarLocation()
    {
        // Arrange
        var start = new Point(1, 1);
        var end = new Point(4, 5);

        // Act
        var polar = PolarCoordinateSystem.CartesianToPolarCoordinates(start, end);

        // Assert
        Assert.AreEqual(5.0, polar.Distance, 0.001);
    }

    [TestMethod]
    public void CartesianToPolarCoordinates_SamePoints_ReturnsZeroDistance()
    {
        // Arrange
        var start = new Point(5, 5);
        var end = new Point(5, 5);

        // Act
        var polar = PolarCoordinateSystem.CartesianToPolarCoordinates(start, end);

        // Assert
        Assert.AreEqual(0.0, polar.Distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_HorizontalLine_ReturnsCorrectDistance()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(10.0, distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_VerticalLine_ReturnsCorrectDistance()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(0, 10);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(10.0, distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_3_4_5Triangle_Returns5()
    {
        // Arrange
        var start = new Point(0, 0);
        var end = new Point(3, 4);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_NegativeCoordinates_ReturnsCorrectDistance()
    {
        // Arrange
        var start = new Point(-5, -5);
        var end = new Point(-2, -1);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_SamePoints_ReturnsZero()
    {
        // Arrange
        var start = new Point(10, 10);
        var end = new Point(10, 10);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(0.0, distance, 0.001);
    }

    [TestMethod]
    public void DistanceOnCartesianMap_LargeCoordinates_ReturnsCorrectDistance()
    {
        // Arrange
        var start = new Point(1000, 1000);
        var end = new Point(1003, 1004);

        // Act
        var distance = PolarCoordinateSystem.DistanceOnCartesianMap(start, end);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_HorizontalLine_ReturnsCorrectDistance()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(0, 0, 10, 0);

        // Assert
        Assert.AreEqual(10.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_VerticalLine_ReturnsCorrectDistance()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(0, 0, 0, 10);

        // Assert
        Assert.AreEqual(10.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_3_4_5Triangle_Returns5()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(0, 0, 3, 4);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_NegativeCoordinates_ReturnsCorrectDistance()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(-5, -5, -2, -1);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_SamePoints_ReturnsZero()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(10, 10, 10, 10);

        // Assert
        Assert.AreEqual(0.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_LargeCoordinates_ReturnsCorrectDistance()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(1000, 1000, 1003, 1004);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void getDistance_DecimalCoordinates_ReturnsCorrectDistance()
    {
        // Arrange & Act
        var distance = PolarCoordinateSystem.getDistance(0.5, 0.5, 3.5, 4.5);

        // Assert
        Assert.AreEqual(5.0, distance, 0.001);
    }

    [TestMethod]
    public void DegToRad_0Degrees_Returns0Radians()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(0);

        // Assert
        Assert.AreEqual(0.0, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_90Degrees_ReturnsHalfPi()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(90);

        // Assert
        Assert.AreEqual(Math.PI / 2, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_180Degrees_ReturnsPi()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(180);

        // Assert
        Assert.AreEqual(Math.PI, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_360Degrees_ReturnsTwoPi()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(360);

        // Assert
        Assert.AreEqual(2 * Math.PI, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_45Degrees_ReturnsQuarterPi()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(45);

        // Assert
        Assert.AreEqual(Math.PI / 4, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_NegativeDegrees_ReturnsNegativeRadians()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(-90);

        // Assert
        Assert.AreEqual(-Math.PI / 2, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_LargeAngle_ReturnsCorrectRadians()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(720);

        // Assert
        Assert.AreEqual(4 * Math.PI, radians, 0.001);
    }

    [TestMethod]
    public void DegToRad_DecimalDegrees_ReturnsCorrectRadians()
    {
        // Arrange & Act
        var radians = PolarCoordinateSystem.DegToRad(30.5);

        // Assert
        Assert.AreEqual(30.5 * Math.PI / 180, radians, 0.001);
    }
}

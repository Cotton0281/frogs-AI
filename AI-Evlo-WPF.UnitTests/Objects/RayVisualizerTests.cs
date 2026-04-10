using AI_Evlo_Test.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [TestClass]
    public class RayVisualizerTests
    {
        private Canvas _canvas = null!;

        [TestInitialize]
        public void Setup()
        {
            _canvas = new Canvas();
        }

        // IsVisible Property Tests
        [TestMethod]
        public void IsVisible_WhenSetToTrue_MakesAllRayLinesVisible()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            visualizer.IsVisible = false;

            // Act
            visualizer.IsVisible = true;

            // Assert
            foreach (var child in _canvas.Children)
            {
                if (child is Line line)
                {
                    Assert.AreEqual(Visibility.Visible, line.Visibility);
                }
            }
        }

        [TestMethod]
        public void IsVisible_WhenSetToTrue_MakesAllHitDotsVisible()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            visualizer.IsVisible = false;

            // Act
            visualizer.IsVisible = true;

            // Assert
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse dot)
                {
                    Assert.AreEqual(Visibility.Visible, dot.Visibility);
                }
            }
        }

        [TestMethod]
        public void IsVisible_WhenSetToFalse_CollapsesAllRayLines()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            visualizer.IsVisible = true;

            // Act
            visualizer.IsVisible = false;

            // Assert
            foreach (var child in _canvas.Children)
            {
                if (child is Line line)
                {
                    Assert.AreEqual(Visibility.Collapsed, line.Visibility);
                }
            }
        }

        [TestMethod]
        public void IsVisible_WhenSetToFalse_CollapsesAllHitDots()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            visualizer.IsVisible = true;

            // Act
            visualizer.IsVisible = false;

            // Assert
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse dot)
                {
                    Assert.AreEqual(Visibility.Collapsed, dot.Visibility);
                }
            }
        }

        [TestMethod]
        public void IsVisible_Get_ReturnsCurrentValue()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            visualizer.IsVisible = false;

            // Act
            var isVisible = visualizer.IsVisible;

            // Assert
            Assert.IsFalse(isVisible);
        }

        // Constructor Tests
        [TestMethod]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var visualizer = new RayVisualizer(_canvas, 5);

            // Assert
            Assert.IsNotNull(visualizer);
        }

        [TestMethod]
        public void Constructor_WithRayCount_CreatesCorrectNumberOfLines()
        {
            // Arrange
            int rayCount = 5;

            // Act
            var visualizer = new RayVisualizer(_canvas, rayCount);

            // Assert
            int lineCount = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Line)
                    lineCount++;
            }
            Assert.AreEqual(rayCount, lineCount);
        }

        [TestMethod]
        public void Constructor_WithRayCount_CreatesCorrectNumberOfDots()
        {
            // Arrange
            int rayCount = 5;

            // Act
            var visualizer = new RayVisualizer(_canvas, rayCount);

            // Assert
            int dotCount = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse)
                    dotCount++;
            }
            Assert.AreEqual(rayCount, dotCount);
        }

        [TestMethod]
        public void Constructor_WithZeroRayCount_CreatesNoElements()
        {
            // Act
            var visualizer = new RayVisualizer(_canvas, 0);

            // Assert
            Assert.AreEqual(0, _canvas.Children.Count);
        }

        // Draw Method Tests
        [TestMethod]
        public void Draw_WhenIsVisibleFalse_DoesNotDrawRays()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 2);
            visualizer.IsVisible = false;
            var perception = new RayPerception(2, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            foreach (var child in _canvas.Children)
            {
                if (child is Line line)
                {
                    Assert.AreEqual(0, line.X1);
                }
            }
        }

        [TestMethod]
        public void Draw_WhenPerceptionNull_DoesNotCrash()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 2);
            var agentLocation = new Point(50, 50);

            // Act & Assert - no exception thrown
            visualizer.Draw(agentLocation, null);
        }

        [TestMethod]
        public void Draw_WithValidHit_SetsLineCoordinates()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            // Set up a valid hit
            perception.Hits[0] = new RayHit
            {
                IsValid = true,
                HitPoint = new Point(100, 100),
                Category = ObjectCategory.Food,
                Distance = 50
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Line? line = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Line l)
                {
                    line = l;
                    break;
                }
            }

            Assert.IsNotNull(line);
            Assert.AreEqual(50, line.X1);
            Assert.AreEqual(50, line.Y1);
            Assert.AreEqual(100, line.X2);
            Assert.AreEqual(100, line.Y2);
        }

        [TestMethod]
        public void Draw_WithValidHit_MakesLineVisible()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = true,
                HitPoint = new Point(100, 100),
                Category = ObjectCategory.Food,
                Distance = 50
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Line? line = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Line l)
                {
                    line = l;
                    break;
                }
            }

            Assert.IsNotNull(line);
            Assert.AreEqual(Visibility.Visible, line.Visibility);
        }

        [TestMethod]
        public void Draw_WithInvalidHit_CollapsesLine()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = false
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Line? line = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Line l)
                {
                    line = l;
                    break;
                }
            }

            Assert.IsNotNull(line);
            Assert.AreEqual(Visibility.Collapsed, line.Visibility);
        }

        [TestMethod]
        public void Draw_WithInvalidHit_CollapsesDot()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = false
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Ellipse? dot = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse d)
                {
                    dot = d;
                    break;
                }
            }

            Assert.IsNotNull(dot);
            Assert.AreEqual(Visibility.Collapsed, dot.Visibility);
        }

        [TestMethod]
        public void Draw_WithHitHavingCategory_MakesDotVisible()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = true,
                HitPoint = new Point(100, 100),
                Category = ObjectCategory.Food,
                Distance = 50
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Ellipse? dot = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse d)
                {
                    dot = d;
                    break;
                }
            }

            Assert.IsNotNull(dot);
            Assert.AreEqual(Visibility.Visible, dot.Visibility);
        }

        [TestMethod]
        public void Draw_WithHitHavingCategory_SetsDotPosition()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = true,
                HitPoint = new Point(100, 100),
                Category = ObjectCategory.Food,
                Distance = 50
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Ellipse? dot = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse d)
                {
                    dot = d;
                    break;
                }
            }

            Assert.IsNotNull(dot);
            double left = Canvas.GetLeft(dot);
            double top = Canvas.GetTop(dot);
            Assert.AreEqual(97, left);
            Assert.AreEqual(97, top);
        }

        [TestMethod]
        public void Draw_WithHitNoCategory_CollapsesDot()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 1);
            var perception = new RayPerception(1, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit
            {
                IsValid = true,
                HitPoint = new Point(100, 100),
                Category = null,
                Distance = 50
            };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            Ellipse? dot = null;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse d)
                {
                    dot = d;
                    break;
                }
            }

            Assert.IsNotNull(dot);
            Assert.AreEqual(Visibility.Collapsed, dot.Visibility);
        }

        [TestMethod]
        public void Draw_WithMultipleHits_DrawsAllValidRays()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 3);
            var perception = new RayPerception(3, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit { IsValid = true, HitPoint = new Point(100, 100), Category = ObjectCategory.Food };
            perception.Hits[1] = new RayHit { IsValid = true, HitPoint = new Point(150, 150), Category = ObjectCategory.Raft };
            perception.Hits[2] = new RayHit { IsValid = false };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            int visibleLines = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Line line && line.Visibility == Visibility.Visible)
                {
                    visibleLines++;
                }
            }
            Assert.AreEqual(2, visibleLines);
        }

        [TestMethod]
        public void Draw_WithFewerHitsThanPooledElements_HidesExtraLines()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);
            var perception = new RayPerception(2, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit { IsValid = true, HitPoint = new Point(100, 100), Category = ObjectCategory.Food };
            perception.Hits[1] = new RayHit { IsValid = true, HitPoint = new Point(150, 150), Category = ObjectCategory.Raft };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            int collapsedLines = 0;
            int lineIndex = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Line line)
                {
                    if (lineIndex >= 2 && line.Visibility == Visibility.Collapsed)
                    {
                        collapsedLines++;
                    }
                    lineIndex++;
                }
            }
            Assert.AreEqual(3, collapsedLines);
        }

        [TestMethod]
        public void Draw_WithFewerHitsThanPooledElements_HidesExtraDots()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);
            var perception = new RayPerception(2, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            perception.Hits[0] = new RayHit { IsValid = true, HitPoint = new Point(100, 100), Category = ObjectCategory.Food };
            perception.Hits[1] = new RayHit { IsValid = true, HitPoint = new Point(150, 150), Category = ObjectCategory.Raft };

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            int collapsedDots = 0;
            int dotIndex = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse dot)
                {
                    if (dotIndex >= 2 && dot.Visibility == Visibility.Collapsed)
                    {
                        collapsedDots++;
                    }
                    dotIndex++;
                }
            }
            Assert.AreEqual(3, collapsedDots);
        }

        [TestMethod]
        public void Draw_WithMoreHitsThanPooledElements_CreatesNewElements()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 2);
            var perception = new RayPerception(4, 100, 180, 1.0);
            var agentLocation = new Point(50, 50);

            for (int i = 0; i < 4; i++)
            {
                perception.Hits[i] = new RayHit
                {
                    IsValid = true,
                    HitPoint = new Point(100 + i * 10, 100 + i * 10),
                    Category = ObjectCategory.Food
                };
            }

            // Act
            visualizer.Draw(agentLocation, perception);

            // Assert
            int lineCount = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Line)
                    lineCount++;
            }
            Assert.AreEqual(4, lineCount);
        }

        // Clear Method Tests
        [TestMethod]
        public void Clear_RemovesAllLinesFromCanvas()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);

            // Act
            visualizer.Clear();

            // Assert
            int lineCount = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Line)
                    lineCount++;
            }
            Assert.AreEqual(0, lineCount);
        }

        [TestMethod]
        public void Clear_RemovesAllDotsFromCanvas()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);

            // Act
            visualizer.Clear();

            // Assert
            int dotCount = 0;
            foreach (var child in _canvas.Children)
            {
                if (child is Ellipse)
                    dotCount++;
            }
            Assert.AreEqual(0, dotCount);
        }

        [TestMethod]
        public void Clear_RemovesAllElements()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);

            // Act
            visualizer.Clear();

            // Assert
            Assert.AreEqual(0, _canvas.Children.Count);
        }

        [TestMethod]
        public void Clear_AfterClear_CanCreateNewElements()
        {
            // Arrange
            var visualizer = new RayVisualizer(_canvas, 5);
            visualizer.Clear();

            // Act
            var perception = new RayPerception(3, 100, 180, 1.0);
            perception.Hits[0] = new RayHit { IsValid = true, HitPoint = new Point(100, 100), Category = ObjectCategory.Food };
            visualizer.Draw(new Point(50, 50), perception);

            // Assert
            Assert.IsGreaterThan(_canvas.Children.Count, 0);
        }
    }
}

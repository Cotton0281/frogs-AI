using AI_Evlo_Test.Objects;
using AI_Evlo_Test;
using AI_Evlo_Test.Enumerators;
using ArtificialNeuralNetwork;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Interop;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [STATestClass]
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
        public void Bird_InteractWithRafts_WhenLanded_DrainsOneFifthOfFlyingHp()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var landed = new Bird { HP = 100 };
            landed.SetLocation(0, 0);
            var flying = new Bird { HP = 100 };
            flying.SetLocation(200, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            landed.InteractWithRafts(ctx);
            flying.InteractWithRafts(ctx);

            double landedDrain = 100 - landed.HP;
            double flyingDrain = 100 - flying.HP;
            Assert.AreEqual(flyingDrain / 5.0, landedDrain, 0.000001);
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenFlyingAndHungry_RegistersAsHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var bird = new Bird { HP = 100 };
            bird.SetLocation(200, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            bird.InteractWithRafts(ctx);

            Assert.IsFalse(bird.IsLanded);
            Assert.AreSame(bird, ctx.HungryBirds[0]);
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenLandedAndHungry_DoesNotRegisterAsHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var bird = new Bird { HP = 100 };
            bird.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            bird.InteractWithRafts(ctx);

            Assert.IsTrue(bird.IsLanded);
            Assert.IsEmpty(ctx.HungryBirds);
            Assert.AreEqual(100 - Bird.LandedHpDrain, bird.HP, 0.000001);
            Assert.IsFalse(bird.IsGettingHP);
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenLandedAndHungry_RegistersAsLandedHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var bird = new Bird { HP = 100 };
            bird.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            bird.InteractWithRafts(ctx);

            Assert.AreSame(bird, ctx.HungryLandedBirds[0]);
        }

        [TestMethod]
        public void ResolveBirdHuntsForTick_WhenBirdBitesShark_TransfersConfiguredBiteHp()
        {
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                BiteHpAmount = 100,
                BiteCooldownTicks = 5,
                PredatorBiteHpThreshold = 0.8
            };

            try
            {
                var bird = new Bird { HP = 100 };
                bird.SetLocation(0, 0);
                var shark = new Shark { HP = 123 };
                shark.SetLocation(10, 0);

                var eaten = AI_Evlo_Test.MainWindow.ResolveBirdHuntsForTick(
                    new System.Collections.Generic.List<Bird> { bird },
                    new System.Collections.Generic.List<Shark> { shark });

                Assert.AreEqual(200, bird.HP);
                Assert.AreEqual(23, shark.HP);
                Assert.AreEqual(0, bird.SharksEaten);
                Assert.AreEqual(5, bird.BiteCooldownTicksRemaining);
                Assert.IsEmpty(eaten);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void ResolveBirdHuntsForTick_WhenBiteKillsShark_CountsKillAndDisposesShark()
        {
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                BiteHpAmount = 100,
                BiteCooldownTicks = 5,
                PredatorBiteHpThreshold = 0.8
            };

            try
            {
                var bird = new Bird { HP = 100 };
                bird.SetLocation(0, 0);
                var shark = new Shark { HP = 40 };
                shark.SetLocation(10, 0);

                var eaten = AI_Evlo_Test.MainWindow.ResolveBirdHuntsForTick(
                    new System.Collections.Generic.List<Bird> { bird },
                    new System.Collections.Generic.List<Shark> { shark });

                Assert.AreEqual(140, bird.HP);
                Assert.AreEqual(0, shark.HP);
                Assert.AreEqual(1, bird.SharksEaten);
                Assert.AreSame(shark, eaten[0]);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void ResolveBirdHuntsForTick_WhenBirdCooldownActive_DoesNotBite()
        {
            var bird = new Bird { HP = 100, BiteCooldownTicksRemaining = 3 };
            bird.SetLocation(0, 0);
            var shark = new Shark { HP = 123 };
            shark.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveBirdHuntsForTick(
                new System.Collections.Generic.List<Bird> { bird },
                new System.Collections.Generic.List<Shark> { shark });

            Assert.AreEqual(100, bird.HP);
            Assert.AreEqual(123, shark.HP);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveBirdHuntsForTick_WhenBirdIsLanded_DoesNotBiteShark()
        {
            var bird = new Bird { HP = 100, IsLanded = true };
            bird.SetLocation(0, 0);
            var shark = new Shark { HP = 123 };
            shark.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveBirdHuntsForTick(
                new System.Collections.Generic.List<Bird> { bird },
                new System.Collections.Generic.List<Shark> { shark });

            Assert.AreEqual(100, bird.HP);
            Assert.AreEqual(123, shark.HP);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveLandedBirdHuntsForTick_WhenBirdBitesRaftFrog_TransfersBirdBiteHp()
        {
            var bird = new Bird { HP = 100, IsLanded = true };
            bird.SetLocation(0, 0);
            var frog = new Frog { HP = 123 };
            frog.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveLandedBirdHuntsForTick(
                new System.Collections.Generic.List<Bird> { bird },
                new System.Collections.Generic.List<Frog> { frog });

            Assert.AreEqual(200, bird.HP);
            Assert.AreEqual(23, frog.HP);
            Assert.AreEqual(5, bird.BiteCooldownTicksRemaining);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveRaftFrogHuntsForTick_WhenFrogBitesLandedBird_TransfersFiveHp()
        {
            var frog = new Frog { HP = 100 };
            frog.SetLocation(0, 0);
            var bird = new Bird { HP = 123, IsLanded = true };
            bird.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveRaftFrogHuntsForTick(
                new System.Collections.Generic.List<Frog> { frog },
                new System.Collections.Generic.List<Bird> { bird });

            Assert.AreEqual(105, frog.HP);
            Assert.AreEqual(118, bird.HP);
            Assert.AreEqual(5, frog.BiteCooldownTicksRemaining);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveWaterFrogHuntsForTick_WhenFrogBitesShark_TransfersFiveHp()
        {
            var frog = new Frog { HP = 100 };
            frog.SetLocation(0, 0);
            var shark = new Shark { HP = 123 };
            shark.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveWaterFrogHuntsForTick(
                new System.Collections.Generic.List<Frog> { frog },
                new System.Collections.Generic.List<Shark> { shark });

            Assert.AreEqual(105, frog.HP);
            Assert.AreEqual(118, shark.HP);
            Assert.AreEqual(5, frog.BiteCooldownTicksRemaining);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveSharkHuntsForTick_WhenSharkBitesFlyingBird_TransfersThirtyHp()
        {
            var shark = new Shark { HP = 100 };
            shark.SetLocation(0, 0);
            var bird = new Bird { HP = 123, IsLanded = false };
            bird.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveSharkHuntsForTick(
                new System.Collections.Generic.List<Shark> { shark },
                new System.Collections.Generic.List<Bird> { bird });

            Assert.AreEqual(130, shark.HP);
            Assert.AreEqual(93, bird.HP);
            Assert.AreEqual(5, shark.BiteCooldownTicksRemaining);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveSharkHuntsForTick_WhenBirdIsLanded_DoesNotBite()
        {
            var shark = new Shark { HP = 100 };
            shark.SetLocation(0, 0);
            var bird = new Bird { HP = 123, IsLanded = true };
            bird.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveSharkHuntsForTick(
                new System.Collections.Generic.List<Shark> { shark },
                new System.Collections.Generic.List<Bird> { bird });

            Assert.AreEqual(100, shark.HP);
            Assert.AreEqual(123, bird.HP);
            Assert.IsEmpty(eaten);
        }

        [TestMethod]
        public void ResolveSharkHuntsForTick_WhenSharkBitesWaterFrog_TransfersConfiguredBiteHp()
        {
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                BiteHpAmount = 100,
                BiteCooldownTicks = 5,
                PredatorBiteHpThreshold = 0.8
            };

            try
            {
                var shark = new Shark { HP = 100 };
                shark.SetLocation(0, 0);
                var frog = new Frog { HP = 123 };
                frog.SetLocation(10, 0);

                var eaten = AI_Evlo_Test.MainWindow.ResolveSharkHuntsForTick(
                    new System.Collections.Generic.List<Shark> { shark },
                    new System.Collections.Generic.List<Frog> { frog });

                Assert.AreEqual(200, shark.HP);
                Assert.AreEqual(23, frog.HP);
                Assert.AreEqual(0, shark.FrogsEaten);
                Assert.AreEqual(5, shark.BiteCooldownTicksRemaining);
                Assert.IsEmpty(eaten);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void ResolveSharkHuntsForTick_WhenWaterFrogAndFlyingBirdAreValid_BitesNearestTarget()
        {
            var shark = new Shark { HP = 100 };
            shark.SetLocation(0, 0);
            var frog = new Frog { HP = 123 };
            frog.SetLocation(20, 0);
            var bird = new Bird { HP = 123, IsLanded = false };
            bird.SetLocation(10, 0);

            var result = AI_Evlo_Test.MainWindow.ResolveSharkHuntsForTick(
                new System.Collections.Generic.List<Shark> { shark },
                new System.Collections.Generic.List<Frog> { frog },
                new System.Collections.Generic.List<Bird> { bird });

            Assert.AreEqual(130, shark.HP);
            Assert.AreEqual(123, frog.HP);
            Assert.AreEqual(93, bird.HP);
            Assert.AreEqual(5, shark.BiteCooldownTicksRemaining);
            Assert.IsEmpty(result.FrogsToDispose);
            Assert.IsEmpty(result.BirdsToDispose);
        }

        [TestMethod]
        public void BirdAndShark_IsHungry_UseConfiguredEightyPercentThreshold()
        {
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                BiteHpAmount = 100,
                BiteCooldownTicks = 5,
                PredatorBiteHpThreshold = 0.8
            };

            try
            {
                var bird = new Bird { HP = Bird.BirdMaxHp * 0.79 };
                var fullBird = new Bird { HP = Bird.BirdMaxHp * 0.8 };
                var shark = new Shark { HP = Shark.SharkMaxHp * 0.79 };
                var fullShark = new Shark { HP = Shark.SharkMaxHp * 0.8 };

                Assert.IsTrue(bird.IsHungry);
                Assert.IsFalse(fullBird.IsHungry);
                Assert.IsTrue(shark.IsHungry);
                Assert.IsFalse(fullShark.IsHungry);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void Frog_IsHungry_UsesConfiguredEightyPercentThreshold()
        {
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                PredatorBiteHpThreshold = 0.8
            };

            try
            {
                var hungryFrog = new Frog { HP = SmartObject.MaxHp * 0.79 };
                var fullFrog = new Frog { HP = SmartObject.MaxHp * 0.8 };

                Assert.IsTrue(hungryFrog.IsHungry);
                Assert.IsFalse(fullFrog.IsHungry);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void Frog_InteractWithRafts_WhenHungryOnRaft_RegistersAsRaftHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var frog = new Frog { HP = SmartObject.MaxHp * 0.79 };
            frog.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            frog.InteractWithRafts(ctx);

            Assert.AreSame(frog, ctx.FrogsOnRafts[0]);
            Assert.AreSame(frog, ctx.HungryFrogsOnRafts[0]);
            Assert.IsEmpty(ctx.FrogsInWater);
            Assert.IsEmpty(ctx.HungryFrogsInWater);
        }

        [TestMethod]
        public void Frog_InteractWithRafts_WhenHungryInWater_RegistersAsWaterHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(200, 0);
            var frog = new Frog { HP = SmartObject.MaxHp * 0.79 };
            frog.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            frog.InteractWithRafts(ctx);

            Assert.AreSame(frog, ctx.FrogsInWater[0]);
            Assert.AreSame(frog, ctx.HungryFrogsInWater[0]);
            Assert.IsEmpty(ctx.FrogsOnRafts);
            Assert.IsEmpty(ctx.HungryFrogsOnRafts);
        }

        [TestMethod]
        public void Bird_IgnoredCategories_DoesNotIgnoreSharks()
        {
            var bird = new Bird();

            bool ignoresShark = System.Array.Exists(
                bird.IgnoredCategories,
                category => category == ObjectCategory.Shark);

            Assert.IsFalse(ignoresShark);
        }

        [TestMethod]
        public void Shark_IgnoredCategories_SeesBirdsAndWaterFrogsButIgnoresRaftFrogs()
        {
            var shark = new Shark();

            bool ignoresBird = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Bird || category == ObjectCategory.Bird_Landed);
            bool ignoresWaterFrog = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Frog);
            bool ignoresRaftFrog = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Frog_OnRaft);

            Assert.IsFalse(ignoresBird);
            Assert.IsFalse(ignoresWaterFrog);
            Assert.IsTrue(ignoresRaftFrog);
        }

        [TestMethod]
        public void RayPerception_WhenFrogOnRaftIgnored_DetectsWaterFrogBehindIt()
        {
            var perception = new RayPerception(1, 100, 0, 1.0);
            var raftFrog = new TargetObj { Size = 10, Category = ObjectCategory.Frog_OnRaft };
            raftFrog.SetLocation(20, 0);
            var waterFrog = new TargetObj { Size = 10, Category = ObjectCategory.Frog };
            waterFrog.SetLocation(40, 0);
            var objects = new System.Collections.Generic.List<ISensable> { raftFrog, waterFrog };

            perception.Update(
                new Point(0, 0),
                new System.Windows.Vector(1, 0),
                objects,
                ignoredCategories: new[] { ObjectCategory.Frog_OnRaft });

            Assert.AreEqual(ObjectCategory.Frog, perception.Hits[0].Category);
        }

        [TestMethod]
        public void CachedInputs_WhenNull_CreatesNewArrayWithCorrectLength()
        {
            // Arrange
            var smartObject = new SmartObject();
            int expectedLength = smartObject.Perception.Signals.Length + 1;

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
        public void Act_WithOutputs_StoresRequestedMovement()
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
            Assert.AreEqual(1.0, smartObject.LastSpeed);
            Assert.AreEqual(1.5, smartObject.LastRotation);
        }

        [TestMethod]
        public void MovementSettings_DefaultsToSuggestedCosts()
        {
            // Act
            var settings = new MovementSettings();

            // Assert
            Assert.AreEqual(0.01, settings.RotationHpCost);
            Assert.AreEqual(0.04, settings.ThrustHpCost);
            Assert.AreEqual(0.1, settings.LandedBirdSpeedMultiplier);
            Assert.AreEqual(100, settings.BiteHpAmount);
            Assert.AreEqual(5, settings.BiteCooldownTicks);
            Assert.AreEqual(0.8, settings.PredatorBiteHpThreshold);
        }

        [TestMethod]
        public void MovementSettings_Normalize_RejectsNegativeCosts()
        {
            // Arrange
            var settings = new MovementSettings
            {
                RotationHpCost = -1,
                ThrustHpCost = -2,
                LandedBirdSpeedMultiplier = -0.5,
                BiteHpAmount = -100,
                BiteCooldownTicks = -5,
                PredatorBiteHpThreshold = 2
            };

            // Act
            settings.Normalize();

            // Assert
            Assert.AreEqual(0.0, settings.RotationHpCost);
            Assert.AreEqual(0.0, settings.ThrustHpCost);
            Assert.AreEqual(0.1, settings.LandedBirdSpeedMultiplier);
            Assert.AreEqual(1, settings.BiteHpAmount);
            Assert.AreEqual(0, settings.BiteCooldownTicks);
            Assert.AreEqual(0.8, settings.PredatorBiteHpThreshold);
        }

        [TestMethod]
        public void SaveAndLoadMovementSettings_RoundTripsConfiguredCosts()
        {
            // Arrange
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var settings = new MovementSettings
            {
                RotationHpCost = 0.025,
                ThrustHpCost = 0.075,
                LandedBirdSpeedMultiplier = 0.2,
                BiteHpAmount = 75,
                BiteCooldownTicks = 9,
                PredatorBiteHpThreshold = 0.65
            };

            try
            {
                // Act
                MainWindow.SaveMovementSettingsToPath(path, settings);
                MovementSettings loaded = MainWindow.LoadMovementSettingsFromPath(path);

                // Assert
                Assert.AreEqual(0.025, loaded.RotationHpCost);
                Assert.AreEqual(0.075, loaded.ThrustHpCost);
                Assert.AreEqual(0.2, loaded.LandedBirdSpeedMultiplier);
                Assert.AreEqual(75, loaded.BiteHpAmount);
                Assert.AreEqual(9, loaded.BiteCooldownTicks);
                Assert.AreEqual(0.65, loaded.PredatorBiteHpThreshold);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void CleanupLegacyPopulationFiles_PreservesMovementSettingsJson()
        {
            // Arrange
            string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            string sessionPath = Path.Combine(dir, "session.json");
            string movementSettingsPath = Path.Combine(dir, "movement-settings.json");
            string legacyPath = Path.Combine(dir, "old-population.json");
            File.WriteAllText(sessionPath, "{}");
            File.WriteAllText(movementSettingsPath, "{}");
            File.WriteAllText(legacyPath, "{}");

            try
            {
                // Act
                MainWindow.CleanupLegacyPopulationFiles(dir);

                // Assert
                Assert.IsTrue(File.Exists(sessionPath));
                Assert.IsTrue(File.Exists(movementSettingsPath));
                Assert.IsFalse(File.Exists(legacyPath));
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
        }

        [TestMethod]
        public void MovementSettingsWindow_Constructor_DoesNotThrowDuringSliderInitialization()
        {
            // Arrange
            var settings = new MovementSettings();

            // Act
            var window = new MovementSettingsWindow(settings);

            // Assert
            Assert.IsNotNull(window);
            window.Close();
        }

        [TestMethod]
        public void MovementSettingsWindow_WhenEscapePressed_ClosesWithoutSaving()
        {
            // Arrange
            var window = new MovementSettingsWindow(new MovementSettings());
            bool closed = false;
            window.Closed += (sender, args) => closed = true;

            // Act
            window.RaiseEvent(new System.Windows.Input.KeyEventArgs(
                Keyboard.PrimaryDevice,
                new HwndSource(0, 0, 0, 0, 0, string.Empty, nint.Zero),
                0,
                Key.Escape)
            {
                RoutedEvent = System.Windows.UIElement.PreviewKeyDownEvent
            });

            // Assert
            Assert.IsTrue(closed);
            Assert.AreNotEqual(true, window.DialogResult);
        }

        [TestMethod]
        public void Act_WithMovementCosts_ReducesHpByAppliedRotationAndThrust()
        {
            // Arrange
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                RotationHpCost = 0.1,
                ThrustHpCost = 0.2,
                LandedBirdSpeedMultiplier = 0.1
            };

            try
            {
                var mockNeuralNetwork = new Mock<INeuralNetwork>();
                mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

                var smartObject = new SmartObject
                {
                    NNetwork = mockNeuralNetwork.Object,
                    HP = 100
                };

                // Act
                smartObject.Act(new double[5]);

                // Assert: rotation 1.5 * 0.1 + thrust 1.0 * 0.2
                Assert.AreEqual(99.65, smartObject.HP, 0.000001);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void Bird_Act_WhenLanded_CapsSpeedToOneTenthOfFlyingMaxSpeed()
        {
            // Arrange
            MovementSettings originalSettings = SmartObject.MovementSettings;
            SmartObject.MovementSettings = new MovementSettings
            {
                RotationHpCost = 0,
                ThrustHpCost = 0,
                LandedBirdSpeedMultiplier = 0.1
            };

            try
            {
                var mockNeuralNetwork = new Mock<INeuralNetwork>();
                mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.5 });

                var bird = new Bird
                {
                    NNetwork = mockNeuralNetwork.Object,
                    IsLanded = true
                };
                Point originalLocation = bird.Location;

                // Act
                bird.Act(new double[5]);

                // Assert
                Assert.AreEqual(SmartObject.MaxSpeed * 0.1, bird.LastSpeed, 0.000001);
                Assert.AreEqual(originalLocation.Y - (SmartObject.MaxSpeed * 0.1), bird.Location.Y, 0.000001);
            }
            finally
            {
                SmartObject.MovementSettings = originalSettings;
            }
        }

        [TestMethod]
        public void Act_WithOutputs_MovesObject()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double originalY = smartObject.Location.Y;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreNotEqual(originalY, smartObject.Location.Y);
            Assert.AreEqual(1.0, smartObject.LastSpeed);
        }

        [TestMethod]
        public void TargetObj_CompleteMovementTick_ReportsMovementDelta()
        {
            // Arrange
            var raft = new TargetObj();
            raft.SetLocation(10, 20);

            // Act
            raft.BeginMovementTick();
            raft.SetLocation(14, 13);
            raft.CompleteMovementTick();

            // Assert
            Assert.AreEqual(10, raft.PreviousLocation.X);
            Assert.AreEqual(20, raft.PreviousLocation.Y);
            Assert.AreEqual(4, raft.MovementDelta.X);
            Assert.AreEqual(-7, raft.MovementDelta.Y);
        }

        [TestMethod]
        public void ApplyRaftCarryToAgent_WhenFrogWasOnPreviousRaftLocation_AddsRaftMovement()
        {
            // Arrange
            var raft = new TargetObj { Size = 100 };
            raft.SetLocation(0, 0);
            raft.BeginMovementTick();
            raft.SetLocation(10, 4);
            raft.CompleteMovementTick();

            var frog = new Frog();
            frog.SetLocation(0, 0);

            // Act
            bool carried = MainWindow.ApplyRaftCarryToAgent(frog, new[] { raft });

            // Assert
            Assert.IsTrue(carried);
            Assert.AreEqual(10, frog.Location.X);
            Assert.AreEqual(4, frog.Location.Y);
        }

        [TestMethod]
        public void ApplyRaftCarryToAgent_WhenBirdWasOnPreviousRaftLocation_AddsRaftMovement()
        {
            // Arrange
            var raft = new TargetObj { Size = 100 };
            raft.SetLocation(0, 0);
            raft.BeginMovementTick();
            raft.SetLocation(-3, 7);
            raft.CompleteMovementTick();

            var bird = new Bird();
            bird.SetLocation(0, 0);

            // Act
            bool carried = MainWindow.ApplyRaftCarryToAgent(bird, new[] { raft });

            // Assert
            Assert.IsTrue(carried);
            Assert.AreEqual(-3, bird.Location.X);
            Assert.AreEqual(7, bird.Location.Y);
        }

        [TestMethod]
        public void ApplyRaftCarryToAgent_WhenSharkWasOnPreviousRaftLocation_DoesNotMove()
        {
            // Arrange
            var raft = new TargetObj { Size = 100 };
            raft.SetLocation(0, 0);
            raft.BeginMovementTick();
            raft.SetLocation(10, 4);
            raft.CompleteMovementTick();

            var shark = new Shark();
            shark.SetLocation(0, 0);

            // Act
            bool carried = MainWindow.ApplyRaftCarryToAgent(shark, new[] { raft });

            // Assert
            Assert.IsFalse(carried);
            Assert.AreEqual(0, shark.Location.X);
            Assert.AreEqual(0, shark.Location.Y);
        }

        [TestMethod]
        public void Frog_InteractWithRafts_WhenOnRaft_IncrementsFrogsOnTop()
        {
            // Arrange
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var frog = new Frog { HP = 100 };
            frog.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new List<TargetObj> { raft }
            };

            // Act
            frog.InteractWithRafts(ctx);

            // Assert
            Assert.AreEqual(1, raft.ObjectsOnTop);
            Assert.AreEqual(1, raft.FrogsOnTop);
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenOnRaft_DoesNotIncrementFrogsOnTop()
        {
            // Arrange
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var bird = new Bird { HP = 100 };
            bird.SetLocation(0, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new List<TargetObj> { raft }
            };

            // Act
            bird.InteractWithRafts(ctx);

            // Assert
            Assert.AreEqual(1, raft.ObjectsOnTop);
            Assert.AreEqual(0, raft.FrogsOnTop);
        }

        [TestMethod]
        public void ShouldRaftSink_UsesOneThirdOfFrogPopulationSizeLimit()
        {
            // Arrange
            var populations = new List<Population>
            {
                new Population { Being = PopulationBeing.Frog, SizeLimit = 50, Members = new List<ISmartObject> { new Frog { HP = 100 }, new Frog { HP = 100 } } },
                new Population { Being = PopulationBeing.Bird, SizeLimit = 100, Members = new List<ISmartObject> { new Bird { HP = 100 } } }
            };

            // Act / Assert
            Assert.IsFalse(MainWindow.ShouldRaftSink(2, populations));
            Assert.IsFalse(MainWindow.ShouldRaftSink(16, populations));
            Assert.IsTrue(MainWindow.ShouldRaftSink(17, populations));
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_Test
{
    // Agent creation, shape factories, offspring, disposal, and population regrowth.
    public partial class MainWindow
    {
        private Population CreatePopulation(int PopulationSize, string PopulationName, string nnType, PopulationBeing being)
        {
            if (PopulationSize < 1)
                return null;
            Population newPopulation = new Population();
            newPopulation.Name = PopulationName;
            newPopulation.SizeLimit = PopulationSize;
            newPopulation.Being = being;

            // Set NNet Size based on the captured NN type
            if (nnType == "Small")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Small_1Lx9N();
            else if (nnType == "Medium")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Mid_3Lx10N();
            else if (nnType == "Large")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Big_5Lx20N();

            // give a collor
            switch (lsPopulations.Count)
            {
                case 0: newPopulation.PopulationColor = Colors.SteelBlue; break;
                case 1: newPopulation.PopulationColor = Colors.Orange; break;
                case 2: newPopulation.PopulationColor = Colors.Aqua; break;
                case 3: newPopulation.PopulationColor = Colors.BlueViolet; break;
                case 4: newPopulation.PopulationColor = Colors.BurlyWood; break;
                case 5: newPopulation.PopulationColor = Colors.Gold; break;
                default: newPopulation.PopulationColor = Colors.Aquamarine; break;
            }
            newPopulation.ObjectType = GetObjectTypeForBeing(being);

            // Create and configure objects
            for (int i = 0; i < PopulationSize; i++)
            {
                ISmartObject newObj = CreatePopulationMember(newPopulation);
                newPopulation.Add(newObj);
                lsObjects.Add(newObj);
                newObj.ID = newPopulation.GenerateMemberId();

            }
            return newPopulation;
        }

        private void ReGrowPopulation(Population objPopulation)
        {
            if (objPopulation.Count == 0)
            {
                for (int i = 0; i < objPopulation.SizeLimit; i++)
                {
                    ISmartObject newObj = CreatePopulationMember(objPopulation);

                    lsObjects.Add(newObj);
                    objPopulation.Add(newObj);
                    if (objPopulation.lsBestGenes.Count > i)
                    {
                        GenomeRecord ParentGeno = objPopulation.lsBestGenes[i];
                        NeuralNetworkFactory NNetworkFactory = NeuralNetworkFactory.GetInstance();
                        newObj.NNetwork = NNetworkFactory.Create(ParentGeno.Gene);
                        newObj.Generation = ParentGeno.Generation + 1;
                        newObj.ParentId = ParentGeno.ID;
                        newObj.ID = objPopulation.GenerateMemberId(ParentGeno);
                        ParentGeno.Ofsprings++;
                    }
                    else
                        newObj.ID = objPopulation.GenerateMemberId();

                }
                if (objPopulation.lsBestGenes.Count > 0)
                {
                    double minBestFitness = objPopulation.lsBestGenes[objPopulation.lsBestGenes.Count - 1].Fitness;
                    double maxBestFitness = objPopulation.lsBestGenes[0].Fitness;
                    Log(objPopulation.Name + $" restarted. Used {objPopulation.lsBestGenes.Count} best genes from archive with fithnes from {minBestFitness} to {maxBestFitness}");
                }
                return;
            }

            /// Use life agents as parents to repopulate. Get only ones that have more than minimum HP
            List<ISmartObject> lsParentsOrderBy = objPopulation.Members
                                                .Where(ob => ob.HP > SmartObject.MaxHp / 10)
                                                .OrderByDescending(o => o.Fitness).ToList();
            if (lsParentsOrderBy.Count > 0)
            {
                // loop through parents until generation is created
                int newSize = (int)((objPopulation.SizeLimit * 1.2) - objPopulation.Members.Count);
                int indexParent = 0;
                for (int i = 0; i < newSize; i++)
                {
                    if (indexParent >= lsParentsOrderBy.Count)
                        indexParent = 0;

                    ISmartObject newObj = CreateOffspring(lsParentsOrderBy[indexParent] as ISmartObject, objPopulation);
                    lsObjects.Add(newObj);
                    objPopulation.Add(newObj);
                    indexParent++;
                }
            }
        }

        private SmartObject NewSmartObject2(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            SmartObject newObj = new SmartObject(NeuroNetTemplate, ref randomInit);
            newObj.VisibleShape = CreateNewTrianglePolygon(ColorBrush);

            newObj.VisibleShape.MouseDown += ObjectInterface_MouseDown;
            shapeToObjectMap[newObj.VisibleShape] = newObj;
            double initLocationX = (panlUniverseView.ActualWidth / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            double initLocationY = (panlUniverseView.ActualHeight / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            newObj.SetLocation(initLocationX, initLocationY);
            DrawImage(newObj.VisibleShape, newObj.Location);
            // newObj.ID = (ObjectsIdCounter++).ToString();
            return newObj;
        }

        private Bird NewBird(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Bird newObj = new Bird(NeuroNetTemplate, ref randomInit);
            newObj.VisibleShape = CreateNewBirdImage();
            Canvas.SetZIndex(newObj.VisibleShape, 1);
            newObj.VisibleShape.MouseDown += ObjectInterface_MouseDown;
            shapeToObjectMap[newObj.VisibleShape] = newObj;
            double initLocationX = (panlUniverseView.ActualWidth / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            double initLocationY = (panlUniverseView.ActualHeight / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            newObj.SetLocation(initLocationX, initLocationY);
            DrawImage(newObj.VisibleShape, newObj.Location);
            return newObj;
        }

        private Shark NewShark(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Shark newObj = new Shark(NeuroNetTemplate, ref randomInit);
            newObj.VisibleShape = CreateNewSharkImage();
            Canvas.SetZIndex(newObj.VisibleShape, -1); // under water — beneath rafts and birds
            newObj.VisibleShape.MouseDown += ObjectInterface_MouseDown;
            shapeToObjectMap[newObj.VisibleShape] = newObj;
            double initLocationX = (panlUniverseView.ActualWidth / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            double initLocationY = (panlUniverseView.ActualHeight / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            newObj.SetLocation(initLocationX, initLocationY);
            DrawImage(newObj.VisibleShape, newObj.Location);
            return newObj;
        }

        private Frog NewFrog(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Frog newObj = new Frog(NeuroNetTemplate, ref randomInit);
            newObj.VisibleShape = CreateNewFrogImage("frog9_64.png");
            newObj.VisibleShape.MouseDown += ObjectInterface_MouseDown;
            shapeToObjectMap[newObj.VisibleShape] = newObj;
            double initLocationX = (panlUniverseView.ActualWidth / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            double initLocationY = (panlUniverseView.ActualHeight / 2) + NextRandom(0, (int)(Target.Size)) - (Target.Size / 2);
            newObj.SetLocation(initLocationX, initLocationY);
            DrawImage(newObj.VisibleShape, newObj.Location);
            // newObj.ID = (ObjectsIdCounter++).ToString();
            return newObj;
        }

        private void DisposeObject(ISmartObject obj)
        {
            if (obj != null)
            {
                if (obj.VisibleShape != null)
                {
                    obj.VisibleShape.MouseDown -= ObjectInterface_MouseDown;
                    shapeToObjectMap.Remove(obj.VisibleShape);
                    panlUniverseView.Children.Remove(obj.VisibleShape);
                }
                obj.VisibleShape = null;


                /// Add gene to the list of the best genes in the population if it is good.
                foreach (Population pop in lsPopulations)
                {
                    if (!pop.Members.Contains(obj))
                        continue;

                    // Use Count*2 instead Size/2 because these are integers. Library of genes is half the population
                    double worstBestFitness = pop.lsBestGenes.Count > 0
                        ? pop.lsBestGenes[pop.lsBestGenes.Count - 1].Fitness
                        : double.MinValue;
                    if (pop.lsBestGenes.Count * 2 <= pop.SizeLimit || obj.Fitness > worstBestFitness)
                    {
                        GenomeRecord newGeneEval = new GenomeRecord()
                        {
                            Fitness = obj.Fitness,
                            Gene = obj.NNetwork.GetGenes(),
                            Generation = obj.Generation,
                            ID = obj.ID.ToString()
                        };

                        // Binary search insert to keep list sorted descending — avoids full re-sort
                        int insertIdx = pop.lsBestGenes.FindIndex(g => g.Fitness <= newGeneEval.Fitness);
                        if (insertIdx < 0)
                            pop.lsBestGenes.Add(newGeneEval);
                        else
                            pop.lsBestGenes.Insert(insertIdx, newGeneEval);
                    }

                    //remove the worst of the best. This also will shrink lsBestGenes after population resizing.
                    // List is kept sorted descending, so worst entries are at the end
                    while (pop.lsBestGenes.Count > 0 && pop.lsBestGenes.Count * 2 >= pop.SizeLimit)
                    {
                        int lastIdx = pop.lsBestGenes.Count - 1;
                        pop.lsBestGenes[lastIdx].Gene = null;
                        pop.lsBestGenes.RemoveAt(lastIdx);
                    }
                }

                //remove from lists
                lsObjects.Remove(obj);
                foreach (Population pop in lsPopulations)
                    pop.Members.Remove(obj);
                //obj.OnLocationChanged -= ObjectMoved;

                obj.Dispose();
                obj = null;
            }
        }

        private ISmartObject CreateOffspring(ISmartObject objParent, Population population)
        {
            // Create new NeuroNet with modifications
            INeuralNetwork NNetworkMutated = Utils.CloneNeuroNet(objParent.NNetwork);
            objParent.Ofsprings++;
            NNetworkMutated = evoChember.MutateNN(NNetworkMutated, 1, false);
            ISmartObject newGenerationObj;
            if (objParent is Bird || population.Being == PopulationBeing.Bird)
                newGenerationObj = new Bird(NNetworkMutated);
            else if (objParent is Shark || population.Being == PopulationBeing.Shark)
                newGenerationObj = new Shark(NNetworkMutated);
            else if (objParent is Frog)
                newGenerationObj = new Frog(NNetworkMutated);
            else
                newGenerationObj = new SmartObject(NNetworkMutated);
            if (!isHeadlessMode)
                EnsureVisualForObject(newGenerationObj, population);

            // newGenerationObj.OnLocationChanged += ObjectMoved;
            //double initLocationX = Rnd.Next(0, (int)panlUniverseView.ActualWidth);
            //double initLocationY = Rnd.Next(0, (int)panlUniverseView.ActualHeight);
            newGenerationObj.SetLocation(objParent.Location.X + 1, objParent.Location.Y + 1);
            if (!isHeadlessMode && newGenerationObj.VisibleShape != null)
            {
                DrawImage(newGenerationObj.VisibleShape, newGenerationObj.Location);
            }
            newGenerationObj.ID = population.GenerateMemberId(objParent);
            newGenerationObj.Generation = objParent.Generation + 1;
            newGenerationObj.HP = objParent.HP;
            newGenerationObj.ParentId = objParent.ID;
            return newGenerationObj;
        }

        private void EnsureVisualForObject(ISmartObject smartObject, Population populationHint = null)
        {
            if (smartObject == null || smartObject.VisibleShape != null)
                return;

            if (smartObject is Frog)
            {
                smartObject.VisibleShape = CreateNewFrogImage("frog9_64.png");
            }
            else if (smartObject is Bird)
            {
                smartObject.VisibleShape = CreateNewBirdImage();
                Canvas.SetZIndex(smartObject.VisibleShape, 1);
            }
            else if (smartObject is Shark)
            {
                smartObject.VisibleShape = CreateNewSharkImage();
                Canvas.SetZIndex(smartObject.VisibleShape, -1);
            }
            else
            {
                Population population = populationHint ?? lsPopulations.FirstOrDefault(p => p.Members.Contains(smartObject));
                SolidColorBrush brush = population != null
                    ? population.PopulationColorBrush
                    : Brushes.SteelBlue;
                smartObject.VisibleShape = CreateNewTrianglePolygon(brush);
            }

            if (smartObject.VisibleShape != null)
            {
                smartObject.VisibleShape.MouseDown += ObjectInterface_MouseDown;
                shapeToObjectMap[smartObject.VisibleShape] = smartObject;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FillBrush">Poplations use shared brush</param>
        /// <returns></returns>
        private FrameworkElement CreateNewShape(SolidColorBrush FillBrush)
        {
            // Create a   Ellipse.
            Ellipse myEllipse = new Ellipse();
            myEllipse.Fill = FillBrush;
            myEllipse.StrokeThickness = 1;
            myEllipse.Stroke = Brushes.WhiteSmoke;

            // Set the width and height of the Ellipse.
            myEllipse.Width = 20;
            myEllipse.Height = 15;

            panlUniverseView.Children.Add(myEllipse);
            return myEllipse;
        }

        private FrameworkElement CreateNewImage(String imageFile = "raft.png")
        {
            System.IO.Directory.GetCurrentDirectory();
            Uri ur = new Uri(System.IO.Directory.GetCurrentDirectory() + "\\img\\" + imageFile);

            // Create Image and set its width and height  
            Image dynamicImage = new Image();
            dynamicImage.Width = 200;
            dynamicImage.Height = 200;

            // Create a BitmapSource  
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = ur;
            bitmap.EndInit();

            // Set Image.Source  
            dynamicImage.Source = bitmap;
            // Add Image to Window  
            panlUniverseView.Children.Add(dynamicImage);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewBirdImage()
        {
            Image dynamicImage = new Image();
            dynamicImage.Source = BirdSpriteCache.FlightFrames[0];
            dynamicImage.Width = 40;
            dynamicImage.Height = 40;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            panlUniverseView.Children.Add(dynamicImage);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewSharkImage()
        {
            Image dynamicImage = new Image();
            dynamicImage.Source = SharkSpriteCache.SwimFrames[0];
            dynamicImage.Width = 50;
            dynamicImage.Height = 50;
            dynamicImage.Opacity = 0.85;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            panlUniverseView.Children.Add(dynamicImage);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewFrogImage(String imageFile = "frog9_64.png")
        {
            // Create Image and set its width and height  
            Image dynamicImage = new Image();

            // Set Image.Source
            dynamicImage.Source = FrogSpriteCache.IdleFrames[0];
            // scale dynamicImage to half size
            dynamicImage.Width = 32;
            dynamicImage.Height = 32;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            // Add Image to Window  
            panlUniverseView.Children.Add(dynamicImage);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewImage(ISmartObject objParent)
        {
            var oldImage= (objParent.VisibleShape as Image);

            System.IO.Directory.GetCurrentDirectory();
            //Uri ur = new Uri(System.IO.Directory.GetCurrentDirectory() + "\\img\\" + imageFile);

            // Create Image and set its width and height  
            Image dynamicImage = new Image();
            dynamicImage.Width = oldImage.ActualWidth;
            dynamicImage.Height = oldImage.ActualHeight;

            // Create a BitmapSource  
            //BitmapImage bitmap = new BitmapImage();
            //bitmap.BeginInit();
            //bitmap.UriSource = ur;
            //bitmap.EndInit();

            // Set Image.Source  
            dynamicImage.Source = oldImage.Source.CloneCurrentValue();
            // Add Image to Window  
            panlUniverseView.Children.Add(dynamicImage);

            // get random location close to the parent
            Canvas.SetTop(dynamicImage, objParent.Location.Y + NextRandom(-20, 20));
            Canvas.SetLeft(dynamicImage, objParent.Location.X + NextRandom(-20, 20));

            return dynamicImage;
        }

        private Polygon CreateNewTrianglePolygon(SolidColorBrush FillBrush)
        {
            // Add the Polygon Element
            Polygon myPolygon = new Polygon();
            myPolygon.Stroke = System.Windows.Media.Brushes.Gold;
            myPolygon.Fill = FillBrush;
            myPolygon.StrokeThickness = 1;
            myPolygon.HorizontalAlignment = HorizontalAlignment.Center;
            myPolygon.VerticalAlignment = VerticalAlignment.Center;
            System.Windows.Point Point1 = new System.Windows.Point(0, -15);
            System.Windows.Point Point2 = new System.Windows.Point(8, 8);
            System.Windows.Point Point3 = new System.Windows.Point(-8, 8);
            PointCollection myPointCollection = new PointCollection();
            myPointCollection.Add(Point1);
            myPointCollection.Add(Point2);
            myPointCollection.Add(Point3);
            myPolygon.Points = myPointCollection;
            panlUniverseView.Children.Add(myPolygon);
            return myPolygon;
        }

        private static Type GetObjectTypeForBeing(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird: return typeof(Bird);
                case PopulationBeing.Shark: return typeof(Shark);
                default: return typeof(Frog);
            }
        }

        private ISmartObject CreatePopulationMember(Population population)
        {
            switch (population.Being)
            {
                case PopulationBeing.Bird:
                    return NewBird(population.NeuroNetTemplate, population.PopulationColorBrush);
                case PopulationBeing.Shark:
                    return NewShark(population.NeuroNetTemplate, population.PopulationColorBrush);
                default:
                    return NewFrog(population.NeuroNetTemplate, population.PopulationColorBrush);
            }
        }
    }
}

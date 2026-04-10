using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    public interface ILocation
    {
        double X { get; set; }
        double Y { get; set; }
    }

    /// <summary>
    /// Location in 2D with X and Y coordinates
    /// </summary>
    //public class Location2D : ILocation
    //{
    //    private double _x = 0;
    //    private double _y = 0;
         
    //    public double X { get => _x; set => _x = value; }
    //    public double Y { get => _y; set => _y = value; }

    //    public Location2D() { }
    //    public Location2D(double x, double y)
    //    {
    //        X = x;
    //        Y = y;
    //    }

        /// <summary>
        /// Get distance between 2 locations. Using Pythagorean Theorem a*a+b*b = c*c
        /// </summary>

        //public static double GetDistance(System.Windows.Point L1, System.Windows.Point L2)
        //{
        //    return Math.Sqrt(Math.Pow(L1.X - L2.X, 2) + Math.Pow(L1.Y - L2.Y, 2));
        //}

    //}
}

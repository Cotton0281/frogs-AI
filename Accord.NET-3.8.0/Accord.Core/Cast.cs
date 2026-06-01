// Accord Core Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord
{
    using System;

    /// <summary>
    ///   Runtime cast.
    /// </summary>
    /// 
    /// <typeparam name="T">The target type.</typeparam>
    /// <typeparam name="U">The source type.</typeparam>
    /// 
    internal struct CastValue<T, U>
    {
        private T value;

        /// <summary>
        ///   Gets the value being casted.
        /// </summary>
        /// 
        public T Value { get { return value; } }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CastValue{T, U}"/> struct.
        /// </summary>
        /// 
        public CastValue(U value)
        {
            this.value = (T)System.Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Performs an implicit conversion from <typeparamref name="U"/> to <see cref="CastValue{T, U}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T, U>(U value)
        {
            return new CastValue<T, U>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CastValue{T, U}"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator T(CastValue<T, U> value)
        {
            return value.Value;
        }
    }

    /// <summary>
    ///   Runtime cast.
    /// </summary>
    /// 
    /// <typeparam name="T">The target type.</typeparam>
    /// 
    internal struct CastValue<T>
    {
        private T value;

        /// <summary>
        ///   Gets the value being casted.
        /// </summary>
        /// 
        public T Value { get { return value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="CastValue{T}"/> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public CastValue(object value)
        {
            this.value = (T)System.Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Double"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(double value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Single"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(float value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Decimal"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(Decimal value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Byte"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(Byte value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SByte"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(SByte value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Int16"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(Int16 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt16"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(UInt16 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Int32"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(Int32 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt32"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(UInt32 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Int64"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(Int64 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt64"/> to <see cref="CastValue{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator CastValue<T>(UInt64 value)
        {
            return new CastValue<T>(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CastValue{T}"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator T(CastValue<T> value)
        {
            return value.Value;
        }
    }
}

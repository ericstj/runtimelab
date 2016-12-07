// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayBoundsTests
    {
        private const int MaxArraySize = 0X7FEFFFFF;

        private class BogusCollection<T> : IList<T>
        {
            public T this[int index]
            {
                get { return default(T); }
                set { throw new NotSupportedException(); }
            }

            public int Count => -1;

            public bool IsReadOnly => true;

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item) => false;

            public void CopyTo(T[] array, int arrayIndex) { }

            public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();

            public int IndexOf(T item) => -1;

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class BogusReadOnlyCollection<T> : ReadOnlyCollection<T>
        {
            public BogusReadOnlyCollection() : base(new BogusCollection<T>()) { }
        }

        public static IEnumerable<object> Bounds_TestData()
        {
            yield return new byte[] { 0, 1, byte.MaxValue };
            yield return new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            yield return new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            yield return new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            yield return new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            yield return new uint[] { 0, 1, uint.MaxValue };
            yield return new ulong[] { 0, 1, ulong.MaxValue };
            yield return new ushort[] { 0, 1, ushort.MaxValue };

            yield return new byte?[] { null, 0, 1, byte.MaxValue };
            yield return new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            yield return new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            yield return new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            yield return new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            yield return new uint?[] { null, 0, 1, uint.MaxValue };
            yield return new ulong?[] { null, 0, 1, ulong.MaxValue };
            yield return new ushort?[] { null, 0, 1, ushort.MaxValue };
        }

        public static IEnumerable<object[]> TestData()
        {
            foreach (Array sizes in Bounds_TestData())
            {
                Type sizeType = sizes.GetType().GetElementType();
                foreach (object size in sizes)
                {
                    // ValueType
                    yield return new object[] { typeof(bool), size, sizeType, false };
                    yield return new object[] { typeof(byte), size, sizeType, (byte)0 };
                    yield return new object[] { typeof(char), size, sizeType, (char)0 };
                    yield return new object[] { typeof(decimal), size, sizeType, (decimal)0 };
                    yield return new object[] { typeof(double), size, sizeType, (double)0 };
                    yield return new object[] { typeof(float), size, sizeType, (float)0 };
                    yield return new object[] { typeof(int), size, sizeType, 0 };
                    yield return new object[] { typeof(long), size, sizeType, (long)0 };
                    yield return new object[] { typeof(S), size, sizeType, new S() };
                    yield return new object[] { typeof(sbyte), size, sizeType, (sbyte)0 };
                    yield return new object[] { typeof(Sc), size, sizeType, new Sc() };
                    yield return new object[] { typeof(Scs), size, sizeType, new Scs() };
                    yield return new object[] { typeof(short), size, sizeType, (short)0 };
                    yield return new object[] { typeof(Sp), size, sizeType, new Sp() };
                    yield return new object[] { typeof(Ss), size, sizeType, new Ss() };
                    yield return new object[] { typeof(uint), size, sizeType, (uint)0 };
                    yield return new object[] { typeof(ulong), size, sizeType, (ulong)0 };
                    yield return new object[] { typeof(ushort), size, sizeType, (ushort)0 };

                    // Object
                    yield return new object[] { typeof(C), size, sizeType, null };
                    yield return new object[] { typeof(D), size, sizeType, null };
                    yield return new object[] { typeof(Delegate), size, sizeType, null };
                    yield return new object[] { typeof(E), size, sizeType, (E)0 };
                    yield return new object[] { typeof(El), size, sizeType, (El)0 };
                    yield return new object[] { typeof(Func<object>), size, sizeType, null };
                    yield return new object[] { typeof(I), size, sizeType, null };
                    yield return new object[] { typeof(IEquatable<C>), size, sizeType, null };
                    yield return new object[] { typeof(IEquatable<D>), size, sizeType, null };
                    yield return new object[] { typeof(object), size, sizeType, null };
                    yield return new object[] { typeof(string), size, sizeType, null };
                }
            }
        }

        [Theory]
        [PerCompilationType(nameof(TestData))]
        public static void NewArrayBounds(Type arrayType, object size, Type sizeType, object defaultValue, bool useInterpreter)
        {
            Expression newArrayExpression = Expression.NewArrayBounds(arrayType, Expression.Constant(size, sizeType));
            Expression <Func<Array>> e = Expression.Lambda<Func<Array>>(newArrayExpression);
            Func<Array> f = e.Compile(useInterpreter);

            if (sizeType == typeof(sbyte) || sizeType == typeof(short) || sizeType == typeof(int) || sizeType == typeof(long) ||
                sizeType == typeof(sbyte?) || sizeType == typeof(short?) || sizeType == typeof(int?) || sizeType == typeof(long?))
            {
                VerifyArrayGenerator(f, arrayType, size == null ? (long?)null : Convert.ToInt64(size), defaultValue);
            }
            else
            {
                VerifyArrayGenerator(f, arrayType, size == null ? (long?)null : (long)Convert.ToUInt64(size), defaultValue);
            }
        }

        [Fact]
        public static void ThrowOnNegativeSizedCollection()
        {
            // This is an obscure case, and it doesn't much matter what is thrown, as long as is thrown before such
            // an edge case could cause more obscure damage. A class derived from ReadOnlyCollection is used to catch
            // assumptions that such a type is safe.
            Assert.ThrowsAny<Exception>(() => Expression.NewArrayBounds(typeof(int), new BogusReadOnlyCollection<Expression>()));
        }

        private static void VerifyArrayGenerator(Func<Array> func, Type arrayType, long? size, object defaultValue)
        {
            if (!size.HasValue)
            {
                Assert.Throws<InvalidOperationException>(() => func());
            }
            else if ((ulong)size > int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => func());
            }
            else if (size > MaxArraySize)
            {
                Assert.Throws<OutOfMemoryException>(() => func());
            }
            else
            {
                Array array = func();
                Assert.Equal(arrayType, array.GetType().GetElementType());
                Assert.Equal(0, array.GetLowerBound(0));
                Assert.Equal(size, array.Length);
                foreach (object value in array)
                {
                    Assert.Equal(defaultValue, value);
                }
            }
        }

        [Fact]
        public static void NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.NewArrayBounds(null, Expression.Constant(2)));
        }

        [Fact]
        public static void VoidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(void), Expression.Constant(2)));
        }

        [Fact]
        public static void NullBounds_ThrowsArgumentnNullException()
        {
            Assert.Throws<ArgumentNullException>("bounds", () => Expression.NewArrayBounds(typeof(int), default(Expression[])));
            Assert.Throws<ArgumentNullException>("bounds", () => Expression.NewArrayBounds(typeof(int), default(IEnumerable<Expression>)));
        }

        [Fact]
        public static void EmptyBounds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("bounds", () => Expression.NewArrayBounds(typeof(int)));
        }

        [Fact]
        public static void NullBoundInBounds_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), new Expression[] { null, null }));
            Assert.Throws<ArgumentNullException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), new List<Expression> { null, null }));
        }

        [Fact]
        public static void NonIntegralBoundInBounds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), Expression.Constant(2.0)));
        }

        [Fact]
        public static void ByRefType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(int).MakeByRefType(), Expression.Constant(2)));
        }

        [Fact]
        public static void PointerType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(int).MakePointerType(), Expression.Constant(2)));
        }

        [Fact]
        public static void OpenGenericType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>), Expression.Constant(2)));
        }

        [Fact]
        public static void TypeContainsGenericParameters_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>.Enumerator), Expression.Constant(2)));
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(2)));
        }

        [Fact]
        public static void UpdateSameReturnsSame()
        {
            Expression bound0 = Expression.Constant(2);
            Expression bound1 = Expression.Constant(3);
            NewArrayExpression newArrayExpression = Expression.NewArrayBounds(typeof(string), bound0, bound1);
            Assert.Same(newArrayExpression, newArrayExpression.Update(new [] {bound0, bound1}));
        }

        [Fact]
        public static void UpdateDifferentReturnsDifferent()
        {
            Expression bound0 = Expression.Constant(2);
            Expression bound1 = Expression.Constant(3);
            NewArrayExpression newArrayExpression = Expression.NewArrayBounds(typeof(string), bound0, bound1);
            Assert.NotSame(newArrayExpression, newArrayExpression.Update(new[] { bound0 }));
            Assert.NotSame(newArrayExpression, newArrayExpression.Update(new[] { bound0, bound1, bound0, bound1 }));
            Assert.NotSame(newArrayExpression, newArrayExpression.Update(newArrayExpression.Expressions.Reverse()));
        }
    }
}

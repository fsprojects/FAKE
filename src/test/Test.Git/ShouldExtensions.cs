using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Test.Git
{
    public static class ShouldExtensions
    {
        /// <summary>
        ///   Asserts that the item supplied by
        ///   <paramref name = "item" />
        ///   is equal to the
        ///   item supplied by
        ///   <paramref name = "expected" />
        ///   .
        /// </summary>
        public static T ShouldEqual<T>(this T item, T expected)
        {
            Assert.AreEqual(expected, item);
            return item;
        }

        /// <summary>
        ///   Asserts that the extended boolean value is
        ///   <c>true</c>
        ///   .
        /// </summary>
        public static void ShouldBeTrue(this bool item)
        {
            Assert.True(item);
        }

        /// <summary>
        ///   Asserts that the extended boolean value is
        ///   <c>false</c>
        ///   .
        /// </summary>
        public static void ShouldBeFalse(this bool item)
        {
            Assert.False(item);
        }

        /// <summary>
        ///   Asserts that the collection supplied by
        ///   <paramref name = "collection" />
        ///   is empty.
        /// </summary>
        public static void ShouldBeEmpty<T>(this IEnumerable<T> collection)
        {
            collection.Any().ShouldBeFalse();
        }

        /// <summary>
        ///   Shoulds the be empty.
        /// </summary>
        /// <param name = "text">The text.</param>
        public static void ShouldBeEmpty(this string text)
        {
            Assert.IsEmpty(text);
        }

        /// <summary>
        ///   Asserts that the collection supplied by
        ///   <paramref name = "collection" />
        ///   is not empty.
        /// </summary>
        public static IEnumerable<T> ShouldNotBeEmpty<T>(this IEnumerable<T> collection)
        {
            collection.Any().ShouldBeTrue();
            return collection;
        }
    }
}
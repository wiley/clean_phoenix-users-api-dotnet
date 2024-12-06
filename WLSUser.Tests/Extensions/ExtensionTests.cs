using System.Collections.Generic;
using WLSUser.Domain.Extensions;
using Xunit;

namespace WLSUser.Tests.Extensions
{
    public class ExtensionTests
    {
        #region ListExtensions Tests

        [Fact]
        public void ListMergeMissingItemReturnsNotEqual()
        {
            var list1 = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "watermelon"
            };

            var list2 = new List<string>
            {
                "apple",
                "banana",
                "grapefruit",
                "orange",
                "pineapple"
            };

            var expected = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "grapefruit",
                "orange",
                "pineapple"
            };

            // Merge list2 into list1
            list1.Merge(list2);

            Assert.NotEqual(expected.Count, list1.Count);
            list1.Sort();
            expected.Sort();
            Assert.NotEqual(expected, list1);
        }

        [Fact]
        public void ListMergeUnsortedReturnsNotEqual()
        {
            var list1 = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "watermelon"
            };

            var list2 = new List<string>
            {
                "apple",
                "banana",
                "grapefruit",
                "orange",
                "pineapple"
            };

            var expected = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "grapefruit",
                "orange",
                "pineapple",
                "watermelon"
            };

            // Merge list2 into list1
            list1.Merge(list2);

            Assert.Equal(expected.Count, list1.Count);
            Assert.NotEqual(expected, list1);
        }

        [Fact]
        public void ListMergeUnqiueItemsReturnsExpected()
        {
            var list1 = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "watermelon"
            };

            var list2 = new List<string>
            {
                "apple",
                "banana",
                "grapefruit",
                "orange",
                "pineapple"
            };

            var expected = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "grapefruit",
                "orange",
                "pineapple",
                "watermelon"
            };

            // Merge list2 into list1
            list1.Merge(list2);

            Assert.Equal(expected.Count, list1.Count);
            list1.Sort();
            expected.Sort();
            Assert.Equal(expected, list1);
        }

        [Fact]
        public void ListMergeDuplicateItemsReturnsExpected()
        {
            var list1 = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "watermelon",
            };

            var list2 = new List<string>
            {
                "apple",
                "banana",
                "banana",
                "grapefruit",
                "grapefruit",
                "orange",
                "orange",
                "orange",
                "pineapple"
            };

            var expected = new List<string>
            {
                "apple",
                "banana",
                "cantelope",
                "grapefruit",
                "orange",
                "pineapple",
                "watermelon"
            };

            // Merge list2 into list1
            list1.Merge(list2);
            Assert.Equal(expected.Count, list1.Count);
            list1.Sort();
            expected.Sort();
            Assert.Equal(expected, list1);
        }

        #endregion
    }
}
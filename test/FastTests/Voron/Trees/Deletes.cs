﻿using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Voron;
using Voron.Data.BTrees;
using Voron.Impl;

namespace FastTests.Voron.Trees
{
    public class Deletes : StorageTest
    {

        protected override void Configure(StorageEnvironmentOptions options)
        {
            options.PageSize = 4 * Constants.Size.Kilobyte;
            base.Configure(options);
        }

        [Fact]
        public void CanAddVeryLargeValueAndThenDeleteIt()
        {
            var random = new Random();
            var buffer = new byte[8192];
            random.NextBytes(buffer);

            using (var tx = Env.WriteTransaction())
            {
                var tree = tx.CreateTree("foo");
                tree.Add("a", new MemoryStream(buffer));

                tx.Commit();
            }

            using (var tx = Env.ReadTransaction())
            {
                var tree = tx.ReadTree("foo");
                Assert.Equal(4, tree.State.PageCount);
                Assert.Equal(3, tree.State.OverflowPages);
            }

            using (var tx = Env.WriteTransaction())
            {
                var tree = tx.ReadTree("foo");
                tree.Delete("a");

                tx.Commit();
            }


            using (var tx = Env.WriteTransaction())
            {
                var tree = tx.ReadTree("foo");
                Assert.Equal(1, tree.State.PageCount);
                Assert.Equal(0, tree.State.OverflowPages);
            }

            using (var tx = Env.ReadTransaction())
            {
                var tree = tx.ReadTree("foo");
                Assert.Null(tree.Read("a"));

                tx.Commit();
            }


        }

        [Fact]
        public void CanDeleteAtRoot()
        {
            using (var tx = Env.WriteTransaction())
            {
                var tree = tx.CreateTree("foo");
                for (int i = 0; i < 1000; i++)
                {
                    tree.Add(string.Format("{0,5}", i), StreamFor("abcdefg"));
                }
                tx.Commit();
            }

            var expected = new List<Slice>();
            for (int i = 15; i < 1000; i++)
            {
                expected.Add(string.Format("{0,5}", i));
            }

            using (var tx = Env.WriteTransaction())
            {
                var tree = tx.ReadTree("foo");
                for (int i = 0; i < 15; i++)
                {
                    tree.Delete(string.Format("{0,5}", i));
                }
                tx.Commit();
            }


            using (var tx = Env.ReadTransaction())
            {
                var tree = tx.ReadTree("foo");
                var list = Keys(tree, tx);
                Assert.Equal(expected, list);
            }
        }

        public List<Slice> Keys(Tree t, Transaction tx)
        {
            var results = new List<Slice>();
            using (var it = t.Iterate())
            {
                if (it.Seek(Slice.BeforeAllKeys) == false)
                    return results;
                do
                {
                    results.Add(it.CurrentKey);
                } while (it.MoveNext());
            }
            return results;
        }
    }
}
﻿using System.Linq;
using Raven.NewClient.Client.Commands;
using Raven.NewClient.Client.Data;
using Raven.NewClient.Client.Indexes;
using Sparrow.Json;
using Xunit;

namespace FastTests.Issues
{
    public class RavenDB_5297 : RavenNewTestBase
    {
        private class User
        {
            public string Name { get; set; }
        }

        private class UsersByName : AbstractIndexCreationTask<User>
        {
            public override string IndexName
            {
                get { return "Users/ByName"; }
            }

            public UsersByName()
            {
                Map = users => from user in users
                               select new
                               {
                                   user.Name
                               };
            }
        }

        [Fact]
        public void QueryLuceneMinusOperator()
        {
            using (var store = GetDocumentStore())
            {
                store.Initialize();
                new UsersByName().Execute(store);

                using (var session = store.OpenSession())
                {
                    session.Store(new User
                    {
                        Name = "First"
                    }, "users/1");

                    session.Store(new User
                    {
                        Name = "Second"
                    }, "users/2");

                    session.SaveChanges();
                }

                WaitForIndexing(store);

                var requestExecuter = store.GetRequestExecuterForDefaultDatabase();

                JsonOperationContext context;
                using (requestExecuter.ContextPool.AllocateOperationContext(out context))
                {
                    var command = new QueryCommand(store.Conventions, context, "Users/ByName", new IndexQuery { Query = "Name:* -First" });

                    requestExecuter.Execute(command, context);

                    var query = command.Result;
                    Assert.Equal(query.TotalResults, 1);
                }
            }
        }

        [Fact]
        public void QueryLuceneNotOperator()
        {
            using (var store = GetDocumentStore())
            {
                store.Initialize();
                new UsersByName().Execute(store);

                using (var session = store.OpenSession())
                {
                    session.Store(new User
                    {
                        Name = "First"
                    }, "users/1");

                    session.Store(new User
                    {
                        Name = "Second"
                    }, "users/2");

                    session.SaveChanges();
                }

                WaitForIndexing(store);

                var requestExecuter = store.GetRequestExecuterForDefaultDatabase();

                JsonOperationContext context;
                using (requestExecuter.ContextPool.AllocateOperationContext(out context))
                {
                    var command = new QueryCommand(store.Conventions, context, "Users/ByName", new IndexQuery { Query = "Name:* NOT Second" });

                    requestExecuter.Execute(command, context);

                    var query = command.Result;
                    Assert.Equal(query.TotalResults, 1);
                }
            }
        }
    }
}
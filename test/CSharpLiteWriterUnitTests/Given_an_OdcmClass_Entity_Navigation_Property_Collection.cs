﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using CSharpLiteWriterUnitTests;
using FluentAssertions;
using Microsoft.Its.Recipes;
using Microsoft.MockService;
using Microsoft.MockService.Extensions.ODataV4;
using Microsoft.OData.ProxyExtensions.Lite;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Threading.Tasks;
using FluentAssertions.Common;

namespace CSharpLiteWriterUnitTests
{
    public class Given_an_OdcmClass_Entity_Navigation_Property_Collection : NavigationPropertyTestBase
    {
        private MockService _mockedService;
        public Given_an_OdcmClass_Entity_Navigation_Property_Collection()
        {
            base.Init(m =>
            {
                var @namespace = m.Namespaces[0];
                NavTargetClass = Any.OdcmEntityClass(@namespace);
                @namespace.Types.Add(NavTargetClass);

                var @class = @namespace.Classes.First();
                NavigationProperty = Any.OdcmProperty(p =>
                {
                    p.Class = @class;                    
                    p.Projection = NavTargetClass.DefaultProjection;
                    p.IsCollection = true;
                });

                m.Namespaces[0].Classes.First().Properties.Add(NavigationProperty);
            });
        }

        [Fact]
        public void The_Concrete_class_exposes_a_List_Of_Concrete_Type_property()
        {
            ConcreteType.Should().HaveProperty(
                CSharpAccessModifiers.Public,
                null,
                typeof(IList<>).MakeGenericType(NavTargetConcreteType),
                NavigationProperty.Name);
        }

        [Fact]
        public void The_Fetcher_interface_exposes_a_readonly_CollectionInterface_property_with_default_projection()
        {
            FetcherInterface.Should().HaveProperty(
                CSharpAccessModifiers.Public,
                null,
                NavTargetCollectionInterface,
                NavigationProperty.Name);
        }

        [Fact]
        public void The_Fetcher_class_exposes_a_readonly_CollectionInterface_property_with_default_projection()
        {
            FetcherType.Should().HaveProperty(
                CSharpAccessModifiers.Public,
                null,
                NavTargetCollectionInterface,
                NavigationProperty.Name);
        }


        [Fact]
        public void The_Concrete_interface_exposes_a_readonly_IListOfConcreteInterface_property()
        {
            ConcreteInterface.Should().HaveProperty(
                CSharpAccessModifiers.Public,
                null,
                typeof(IList<>).MakeGenericType(NavTargetConcreteInterface),
                NavigationProperty.Name);
        }

        [Fact]
        public void The_Concrete_class_explicitly_implements_readonly_ConcreteInterface_IListOfConcreteInterface_property()
        {
            ConcreteType.Should().HaveExplicitProperty(
                ConcreteInterface,
                CSharpAccessModifiers.Public,
                null,
                typeof(IList<>).MakeGenericType(NavTargetConcreteInterface),
                NavigationProperty.Name);
        }

        [Fact]
        public void The_Collection_class_does_not_expose_it()
        {
            CollectionType.Should().NotHaveProperty(NavigationProperty.Name);
        }

        [Fact]
        public void The_Collection_interface_does_not_expose_it()
        {
            CollectionInterface.Should().NotHaveProperty(NavigationProperty.Name);
        }

        [Fact(Skip = "Issue #24 https://github.com/Microsoft/vipr/issues/24")]
        public void When_retrieved_through_Concrete_ConcreteInterface_Property_then_request_is_sent_with_original_name()
        {
            using (_mockedService = new MockService()
                .SetupPostEntity(TargetEntity)
                .SetupGetEntity(TargetEntity))
            {
                var instance = _mockedService
                    .GetDefaultContext(Model)
                    .CreateConcrete(ConcreteType);

                instance.SetPropertyValues(Class.GetSampleKeyArguments());

                var propertyValue = instance.GetPropertyValue<IPagedCollection>(ConcreteInterface,
                    NavigationProperty.Name);

                propertyValue.GetNextPageAsync().Wait();
            }
        }

        [Fact]
        public void When_retrieved_through_Fetcher_then_request_is_sent_to_server_with_original_name()
        {
            var entityKeyValues = Class.GetSampleKeyArguments().ToArray();

            using (_mockedService = new MockService()
                    .OnGetEntityPropertyRequest(Class.GetDefaultEntityPath(entityKeyValues), NavigationProperty.Name)
                    .RespondWithGetEntity(Class.GetDefaultEntitySetName(), Class.GetSampleJObject(Class.GetSampleKeyArguments())))
            {
                var fetcher = _mockedService
                    .GetDefaultContext(Model)
                    .CreateFetcher(FetcherType, Class.GetDefaultEntityPath(entityKeyValues));

                var propertyFetcher = fetcher.GetPropertyValue<ReadOnlyQueryableSetBase>(NavigationProperty.Name);

                propertyFetcher.ExecuteAsync().Wait();
            }
        }

        [Fact]
        public void When_updated_through_Collection_Fetcher_then_request_is_sent_to_server_with_original_name()
        {
            var entityKeyValues = Class.GetSampleKeyArguments().ToArray();

            using (_mockedService = new MockService()
                .SetupPostEntity(TargetEntity, entityKeyValues)
                .SetupPostEntityPropertyChanges(TargetEntity, entityKeyValues, NavigationProperty))
            {
                var context = _mockedService
                    .GetDefaultContext(Model);

                var instance = context
                    .CreateConcrete(ConcreteType);

                var fetcher = context.CreateFetcher(FetcherType, Class.GetDefaultEntityPath(entityKeyValues));

                var collectionFetcher = fetcher.GetPropertyValue(NavigationProperty.Name);

                var addMethod = "Add" + NavTargetConcreteType.Name + "Async";

                var relatedInstance = Activator.CreateInstance(NavTargetConcreteType);

                collectionFetcher.InvokeMethod<Task>(addMethod, new object[] { relatedInstance, System.Type.Missing }).Wait();
            }
        }
    }
}

public class Given_an_OdcmClass_Entity_Uninitialized : NavigationPropertyTestBase
{
    public Given_an_OdcmClass_Entity_Uninitialized()
    {
        base.Init(m =>
        {
            var @namespace = m.Namespaces[0];
            NavTargetClass = Any.OdcmEntityClass(@namespace);
            @namespace.Types.Add(NavTargetClass);

            var @class = @namespace.Classes.First();
            NavigationProperty = Any.OdcmProperty(p =>
            {
                p.Class = @class;
                //p.Projection.Type = NavTargetClass;
                p.Projection = NavTargetClass.DefaultProjection;
                p.IsCollection = true;
            });

            m.Namespaces[0].Classes.First().Properties.Add(NavigationProperty);
        });
    }

    [Fact]
    public void When_not_bound_to_Context_and_updated_through_Collection_Fetcher_then_throws_InvalidOperationException()
    {
        var relatedInstance = Activator.CreateInstance(NavTargetConcreteType);

        var collectionFetcher = Activator.CreateInstance(NavTargetCollectionType, BindingFlags.NonPublic | BindingFlags.Instance, null,
            new object[] {null, null, null, ""}, null);

        var addMethod = "Add" + NavTargetConcreteType.Name + "Async";

        Action act = () => collectionFetcher.InvokeMethod<Task>(addMethod, new object[] { relatedInstance, System.Type.Missing }).Wait();

        act.ShouldThrow<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithInnerMessage("Not Initialized");
    }
}
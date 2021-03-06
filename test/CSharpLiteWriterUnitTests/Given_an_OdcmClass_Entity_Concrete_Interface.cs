﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.OData.ProxyExtensions.Lite;
using Xunit;

namespace CSharpLiteWriterUnitTests
{
    public class Given_an_OdcmClass_Entity_Concrete_Interface : EntityTestBase
    {
        
        public Given_an_OdcmClass_Entity_Concrete_Interface()
        {
            base.Init();
        }

        [Fact]
        public void The_Concrete_interface_is_Public()
        {
            ConcreteInterface.IsPublic
                .Should().BeTrue("Because it is used when adding new instances to the model.");
        }

        [Fact]
        public void It_is_decorated_with_LowerCasePropertyAttribute()
        {
            ConcreteInterface.Should()
                .BeDecoratedWith<LowerCasePropertyAttribute>();
        }
    }
}

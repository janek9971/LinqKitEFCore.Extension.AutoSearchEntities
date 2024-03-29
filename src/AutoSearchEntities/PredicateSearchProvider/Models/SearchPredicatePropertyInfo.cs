﻿using System;
using System.Linq.Expressions;
using AutoSearchEntities.PredicateSearchProvider.CustomUtilities.Attributes;

#pragma warning disable 1591

namespace AutoSearchEntities.PredicateSearchProvider.Models
{
    public class SearchPredicatePropertyInfo
    {
        public Type InstanceTypeOfProperty { get; set; }
        public object PropertyValue { get; set; }
        public string EntityName { get; set; }
        public bool IsEntityTypeProperty { get; set; } = true;
        public MemberExpression PropertyOrField { get; set; }
        public BitwiseOperation PredicateBitwiseOperation { get; set; } = BitwiseOperation.And;
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using AutoSearchEntities.PredicateSearchProvider.CustomUtilities.Attributes;
using AutoSearchEntities.PredicateSearchProvider.CustomUtilities.Enums;
using AutoSearchEntities.PredicateSearchProvider.Helpers;
using AutoSearchEntities.PredicateSearchProvider.Models;
using LinqKit;

namespace AutoSearchEntities.PredicateSearchProvider
{
    internal partial class AutoPredicateBuilder<TEntity> where TEntity : class, new()
    {


        internal AutoPredicateBuilder()
        {
            Item = Expression.Parameter(typeof(TEntity), "entity");
            AutoPredicate = PredicateBuilder.New<TEntity>(true);
        }
        internal AutoPredicateBuilder(string assemblyName, string typeName)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            Item = Expression.Parameter(typeof(TEntity), "entity");
            AutoPredicate = PredicateBuilder.New<TEntity>(true);
        }
        private string AssemblyName { get; }
        private string TypeName { get; }
        internal ExpressionStarter<TEntity> AutoPredicate { get; set; }
        private ParameterExpression Item { get; }
        private object FilterPropertyValue { get; set; }
        private MemberExpression PropertyOrField { get; set; }
        private string EntityName { get; set; }
        private Type FilterPropertyType { get; set; }
        private Type EntityPropertyType { get; set; }


        internal void PredicateCore<TU>(TU filter, List<Expression<Func<TEntity,bool>>> list =default) where TU : class

        {
            var propertyInfoByPropName = ComputeProperties(filter);
            if (!propertyInfoByPropName.Any()) return;
            {
                foreach (var (key, value) in propertyInfoByPropName)
                {
                    if (!value.InstanceTypeOfProperty.HasProperty(key.PropertyType, value.EntityName))
                        throw new ArgumentException(
                            $"Given property with this type = {key.PropertyType} does not exist for instance: {value.InstanceTypeOfProperty.Name}\r\nDid you forget to add NotThisEntityPropertyAttribute or passed wrong type?",
                            nameof(key.Name)); //TODO TEST
                    //                        propertyInfoByPropName.Remove(key); 
                }

                PredicateBuilderByExpressions(propertyInfoByPropName);
            }
        }

//        internal List<Expression<Func<TEntity, bool>>> GetCustomExpressions<TU>(TU filter)
//            where TU : class, ICustomExpressions<TEntity>
//        {
//            var expressionsBuilder = new ModelExpressions<TEntity>.ExpressionsBuilder(Item);
//            filter?.Expressions(expressionsBuilder);
//
//            return expressionsBuilder.Build().GetExpressions();
//        }
    }
}
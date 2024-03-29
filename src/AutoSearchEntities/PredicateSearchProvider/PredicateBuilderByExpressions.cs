﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoSearchEntities.PredicateSearchProvider.CustomUtilities.Enums;
using AutoSearchEntities.PredicateSearchProvider.Helpers;
using AutoSearchEntities.PredicateSearchProvider.Models;
using CollectionExtensions = AutoSearchEntities.PredicateSearchProvider.Helpers.CollectionExtensions;

namespace AutoSearchEntities.PredicateSearchProvider
{
    internal partial class AutoPredicateBuilder<TEntity>
    {
        private void PredicateBuilderByExpressions(
            IDictionary<PropertyInfo, SearchPredicatePropertyInfo> propertyInfoByPropName)
        {
            foreach (var (prop, searchPredicatePropertyInfo) in propertyInfoByPropName)
            {
                PropertyOrField = searchPredicatePropertyInfo.PropertyOrField;
                EntityName = searchPredicatePropertyInfo.EntityName;
                FilterPropertyValue = searchPredicatePropertyInfo.PropertyValue;
                FilterPropertyType = prop.PropertyType;
                EntityPropertyType = searchPredicatePropertyInfo.InstanceTypeOfProperty
                    .GetProperty(EntityName.ToUpper())?.PropertyType;
                var predicateBitwiseOperation = searchPredicatePropertyInfo.PredicateBitwiseOperation;

                if (FilterPropertyValue is string _)
                {
                    var expression = StringContainsExpr();

                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }
                else if (FilterPropertyValue is StringFilter stringFilter)
                {
                    var expression = StringFilterContainsExpr(stringFilter);


                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }
                else if (FilterPropertyValue is DateTimeFromToFilter dateTimeFromToFilter)
                {
                    var expression = DateTimeExpr(dateTimeFromToFilter, Item);

                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }
                else if (FilterPropertyValue.GetType().IsGenericType &&
                         FilterPropertyValue.GetType().GetGenericTypeDefinition() == typeof(NumericFilter<>))
                {
                    var expression = NumericFilterExpr(Item);

                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }
                else if (!searchPredicatePropertyInfo.IsEntityTypeProperty)
                {
                    var expression = CollectionContainsExpr();

                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }

                else
                {
                    var expression = DefaultExpr();

                    AutoPredicate.PredicateByOperationType(predicateBitwiseOperation, expression);
                }
            }
        }

        private Expression<Func<TEntity, bool>> DefaultExpr()
        {
            var right = Expression.Constant(FilterPropertyValue);
            BinaryExpression binaryExpression;
            if (EntityPropertyType.IsNullable())
            {
                var leftUnaryExpression = Expression.Convert(PropertyOrField, FilterPropertyValue.GetType());

                binaryExpression = Expression.Equal(leftUnaryExpression, right);
            }
            else
            {
                binaryExpression = Expression.Equal(PropertyOrField, right);
            }

            var lambda = binaryExpression.LambdaExpressionBuilder<TEntity>(Item);
            return lambda;
        }

        private Expression<Func<TEntity, bool>> CollectionContainsExpr()
        {
            var genericType = FilterPropertyType.GetGenericArguments().First();
            var methodInfo = typeof(CollectionExtensions)
                .GetMethod(nameof(CollectionExtensions.ContainsMethod), BindingFlags.Public | BindingFlags.Static);

            var valueToEquals = Expression.Constant(FilterPropertyValue);

            Type[] genericArguments = {genericType};
            MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(genericArguments);
            Delegate @delegate = (Delegate) genericMethodInfo.Invoke(null,
                new[] {FilterPropertyValue, Delegates.ContainsEqualityComparer});

            var methodCallExpression = Expression.Call(null, @delegate.Method, valueToEquals, PropertyOrField);

            var lambda = methodCallExpression.LambdaExpressionBuilder<TEntity>(Item);

            return lambda;
        }

        private Expression<Func<TEntity, bool>> StringContainsExpr()
        {
            var equalsMethodInfo =
                typeof(string).GetMethod(StringSearchOption.Equals.ToString(),
                    new[] {typeof(string), typeof(StringComparison)});
            var valueToEquals = Expression.Constant(FilterPropertyValue);
            var comparisonType = Expression.Constant(StringComparison.InvariantCultureIgnoreCase);

            var methodCallExpression = Expression.Call(PropertyOrField, equalsMethodInfo, valueToEquals,
                comparisonType);
            var lambda = methodCallExpression.LambdaExpressionBuilder<TEntity>(Item);

            return lambda;
        }

        private Expression<Func<TEntity, bool>> NumericFilterExpr(ParameterExpression item)
        {
            #region NumericFilter

            var numericFilter = FilterPropertyValue.GetType().GetProperties();
            var value1 = numericFilter[0].GetValue(FilterPropertyValue);
            var value2 = numericFilter[1].GetValue(FilterPropertyValue);
//            var bitOperation = (BitwiseOperation) numericFilter[2].GetValue(FilterPropertyValue);

            #endregion


            #region NumericValue

            var numericValues1 = value1.GetType().GetProperties();
            var numericValue1 = numericValues1[0].GetValue(value1);
            var exprTypeValue1 = (CompareExpressionType) numericValues1[1].GetValue(value1);

            #endregion

            var value1ExprConst = Expression.Constant(numericValue1);

            var expr1 = Expression.MakeBinary(exprTypeValue1.ConvertByName<ExpressionType>(),
                PropertyOrField,
                value1ExprConst);

            BinaryExpression expr2 = null;
            if (value2 != null)
            {
                #region NumericValue

                var numericValues2 = value2.GetType().GetProperties();
                var numericValue2 = numericValues2[0].GetValue(value2);
                var exprTypeValue2 =
                    (CompareExpressionType) numericValues2[1].GetValue(value2);

                #endregion

                var value2ExprConst = Expression.Constant(numericValue2);
                expr2 = Expression.MakeBinary(exprTypeValue2.ConvertByName<ExpressionType>(),
                    PropertyOrField,
                    value2ExprConst);
            }

            var lambda = expr1.LambdaExpressionBuilder<TEntity>(item, expr2, BitwiseOperationExpressions.Or);
            return lambda;
        }

        private Expression<Func<TEntity, bool>> StringFilterContainsExpr(StringFilter stringFilter)
        {
            var methodName =
                stringFilter.StringSearchOption.ToString();
            var equalsMethodInfo =
                typeof(string).GetMethod(methodName, new[] {typeof(string), typeof(StringComparison)});
            var valueToEquals = Expression.Constant(stringFilter.Str);
            var comparisonType = Expression.Constant(stringFilter.StringComparison);

            var methodCallExpression = Expression.Call(PropertyOrField, equalsMethodInfo, valueToEquals,
                comparisonType);
            var lambda = methodCallExpression.LambdaExpressionBuilder<TEntity>(Item);
            return lambda;
        }

        private Expression<Func<TEntity, bool>> DateTimeExpr(DateTimeFromToFilter dateTimeFromToFilter,
            ParameterExpression item)
        {
            var dateTimeValueDateFrom = dateTimeFromToFilter.DateFrom;
            //                        (DateTimeValue) propertiesDateTime[0].GetValue(propertyValue);
            var dateTimeValueDateTo = dateTimeFromToFilter.DateTo;
//            var bitwiseOperation = dateTimeFromToFilter.BitwiseOperation;


            var fromDate = dateTimeValueDateFrom.DateTime;
            DateTime? toDate = default;
            (ConstantExpression rightToDate, CompareExpressionType? ExpressionType) dateToInfoTuple = default;
            CompareExpressionType dateToExprType = default;
            if (dateTimeValueDateTo != null)
            {
                dateToExprType = dateTimeValueDateTo.ExpressionType ?? CompareExpressionType.LessThanOrEqual;
                toDate = dateTimeValueDateTo.DateTime;
                if (toDate.HasValue)
                {
                    var rightToDate = Expression.Constant(toDate.Value.Date);
                    dateToInfoTuple = (rightToDate, dateToExprType);
                }
            }

            var rightFromDate = Expression.Constant(fromDate.Value.Date);

            var dateFromExprType =
                dateTimeValueDateFrom.ExpressionType ?? CompareExpressionType.GreaterThanOrEqual;
            var dateFromInfoTuple = (rightFromDate, compareExpressionType: dateFromExprType);
            Expression<Func<TEntity, bool>> lambdaExpr;
            if (EntityPropertyType.IsNullable())

            {
                var ifTrue = Expression.Property(Expression.Property(PropertyOrField, "Value"), "Date")
                    .GreaterLessThanBuilderExpressions(dateFromInfoTuple, dateToInfoTuple,
                        BitwiseOperationExpressions.AndAlso);

                var ifFalse = PropertyOrField.GreaterLessThanBuilderExpressions(
                    (Expression.Constant(fromDate, typeof(DateTime?)),
                        dateFromExprType),
                    toDate == null
                        ? default
                        : (Expression.Constant(toDate, typeof(DateTime?)),
                            dateToExprType), BitwiseOperationExpressions.AndAlso);


                var conditionalExpression =
                    Expression.Condition(Expression.Property(PropertyOrField, "HasValue"), ifTrue, ifFalse);

                lambdaExpr = conditionalExpression.LambdaExpressionBuilder<TEntity>(item);
            }
            else
            {
                var entityPropTruncated = Expression.Property(PropertyOrField, "Date");

                var dateTimeExpr =
                    entityPropTruncated.GreaterLessThanBuilderExpressions(dateFromInfoTuple, dateToInfoTuple,
                        BitwiseOperationExpressions.AndAlso);

                lambdaExpr = dateTimeExpr.LambdaExpressionBuilder<TEntity>(item);
            }

            return lambdaExpr;
        }
    }
}
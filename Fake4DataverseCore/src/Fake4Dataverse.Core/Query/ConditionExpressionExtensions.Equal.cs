using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Query
{
    public static partial class ConditionExpressionExtensions
    {
        internal static Expression ToEqualExpression(this TypedConditionExpression c, IXrmFakedContext context, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));

            object unaryOperatorValue = null;

            switch (c.CondExpression.Operator)
            {
                case ConditionOperator.Today:
                    unaryOperatorValue = DateTime.Today;
                    break;
                case ConditionOperator.Yesterday:
                    unaryOperatorValue = DateTime.Today.AddDays(-1);
                    break;
                case ConditionOperator.Tomorrow:
                    unaryOperatorValue = DateTime.Today.AddDays(1);
                    break;
                case ConditionOperator.EqualUserId:
                case ConditionOperator.NotEqualUserId:
                    unaryOperatorValue = context.CallerProperties.CallerId.Id;
                    break;

                case ConditionOperator.EqualBusinessId:
                case ConditionOperator.NotEqualBusinessId:
                    unaryOperatorValue = context.CallerProperties.BusinessUnitId.Id;
                    break;
            }

            if (unaryOperatorValue != null)
            {
                //c.Values empty in this case
                var leftHandSideExpression = c.AttributeType.GetAppropiateCastExpressionBasedOnType(getAttributeValueExpr, unaryOperatorValue);
                var transformedExpression = leftHandSideExpression.TransformValueBasedOnOperator(c.CondExpression.Operator);

                expOrValues = Expression.Equal(transformedExpression,
                                TypeCastExpressions.GetAppropiateTypedValueAndType(unaryOperatorValue, c.AttributeType));
            }
#if FAKE_XRM_EASY_9
            else if (c.AttributeType == typeof(OptionSetValueCollection))
            {
                var conditionValue = c.GetSingleConditionValue();

                var leftHandSideExpression = c.AttributeType.GetAppropiateCastExpressionBasedOnType(getAttributeValueExpr, conditionValue);
                var rightHandSideExpression = Expression.Constant(OptionSetValueCollectionExtensions.ConvertToHashSetOfInt(conditionValue, isOptionSetValueCollectionAccepted: false));

                expOrValues = Expression.Equal(
                    Expression.Call(leftHandSideExpression, typeof(HashSet<int>).GetMethod("SetEquals"), rightHandSideExpression),
                    Expression.Constant(true));
            }
#endif
            else
            {
                foreach (object value in c.CondExpression.Values)
                {
                    var leftHandSideExpression = c.AttributeType.GetAppropiateCastExpressionBasedOnType(getAttributeValueExpr, value);
                    var transformedExpression = leftHandSideExpression.TransformValueBasedOnOperator(c.CondExpression.Operator);

                    expOrValues = Expression.Or(expOrValues, 
                                    Expression.Equal(transformedExpression,
                                                    TypeCastExpressions.GetAppropiateTypedValueAndType(value, c.AttributeType)
                                                                    .TransformValueBasedOnOperator(c.CondExpression.Operator)));


                }
            }

            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                expOrValues));
        }
    }
}

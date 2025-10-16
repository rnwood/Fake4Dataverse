#if FAKE_XRM_EASY_9

using System.Collections.Generic;
using System.Linq.Expressions;
using Fake4Dataverse.Extensions;

namespace Fake4Dataverse.Query
{
    public static partial class ConditionExpressionExtensions
    {
        internal static Expression ToContainsValuesExpression(this TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var leftHandSideExpression = tc.AttributeType.GetAppropiateCastExpressionBasedOnType(getAttributeValueExpr, null);
            var rightHandSideExpression = Expression.Constant(OptionSetValueCollectionExtensions.ConvertToHashSetOfInt(tc.CondExpression.Values, isOptionSetValueCollectionAccepted: false));

            return Expression.AndAlso(
                       containsAttributeExpr,
                       Expression.AndAlso(
                           Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                           Expression.Equal(
                               Expression.Call(leftHandSideExpression, typeof(HashSet<int>).GetMethod("Overlaps"), rightHandSideExpression),
                               Expression.Constant(true))));
        }

    }
}
#endif

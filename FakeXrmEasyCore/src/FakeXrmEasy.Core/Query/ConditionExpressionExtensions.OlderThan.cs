using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy.Query
{
    public static partial class ConditionExpressionExtensions
    {
        internal static Expression ToOlderThanExpression(this TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            var valueToAdd = 0;

            if (!int.TryParse(c.Values[0].ToString(), out valueToAdd))
            {
                throw new Exception(c.Operator + " requires an integer value in the ConditionExpression.");
            }

            if (valueToAdd <= 0)
            {
                throw new Exception(c.Operator + " requires a value greater than 0.");
            }

            DateTime toDate = default(DateTime);

            switch (c.Operator)
            {
                case ConditionOperator.OlderThanXMonths:
                    toDate = DateTime.UtcNow.AddMonths(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXMinutes:      
                    toDate = DateTime.UtcNow.AddMinutes(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXHours: 
                    toDate = DateTime.UtcNow.AddHours(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXDays: 
                    toDate = DateTime.UtcNow.AddDays(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXWeeks:              
                    toDate = DateTime.UtcNow.AddDays(-7 * valueToAdd);
                    break;              
                case ConditionOperator.OlderThanXYears: 
                    toDate = DateTime.UtcNow.AddYears(-valueToAdd);
                    break;
            }
                        
            return tc.ToOlderThanExpression(getAttributeValueExpr, containsAttributeExpr, toDate);
        }

        internal static Expression ToOlderThanExpression(this TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr, DateTime olderThanDate)
        {
            var lessThanExpression = Expression.LessThan(
                            tc.AttributeType.GetAppropiateCastExpressionBasedOnType(getAttributeValueExpr, olderThanDate),
                            TypeCastExpressions.GetAppropiateTypedValueAndType(olderThanDate, tc.AttributeType));

            return Expression.AndAlso(containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                lessThanExpression));
        }
    }
}

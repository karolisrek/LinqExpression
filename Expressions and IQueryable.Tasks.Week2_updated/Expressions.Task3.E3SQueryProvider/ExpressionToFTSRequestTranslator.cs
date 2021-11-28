using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        private List<string> MemberNames;
        private List<string> ConstantValues;

        public ExpressionToFtsRequestTranslator()
        {
            MemberNames = new List<string>();
            ConstantValues = new List<string>();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            var result = string.Join(" AND ", MemberNames.Select((name, i) => $"{name}:({ConstantValues[i]})"));

            return result;
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
            {
                return VisitMethodCallWhere(node);
            }
            else if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == "Contains")
                {
                    return VisitMethodCallStringContains(node);
                }
                else if (node.Method.Name == "EndsWith")
                {
                    return VisitMethodCallStringEndsWith(node);
                }
                else if (node.Method.Name == "StartsWith")
                {
                    return VisitMethodCallStringStartsWith(node);
                }
            }
            
            return base.VisitMethodCall(node);
        }

        private Expression VisitMethodCallStringStartsWith(MethodCallExpression node) => VisitMethodCallString(node, (x) => $"{x}*");

        private Expression VisitMethodCallStringEndsWith(MethodCallExpression node) => VisitMethodCallString(node, (x) => $"*{x}");

        private Expression VisitMethodCallStringContains(MethodCallExpression node) => VisitMethodCallString(node, (x) => $"*{x}*");

        private Expression VisitMethodCallString(MethodCallExpression node, Func<string, string> GetConstantValueWithNecessarySymbols)
        {
            Visit(node.Arguments[0]);
            ConstantValues[ConstantValues.Count - 1] = GetConstantValueWithNecessarySymbols(ConstantValues.Last());
            Visit(node.Object);

            return node;
        }

        private Expression VisitMethodCallWhere(MethodCallExpression node)
        {
            var predicate = node.Arguments[1];
            Visit(predicate);

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    return VisitBinaryOnEqual(node);
                case ExpressionType.AndAlso:
                    VisitExpression(node.Left);
                    VisitExpression(node.Right);

                    return node;
                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };
        }

        private void VisitExpression(Expression node)
        {
            if (node is BinaryExpression)
            {
                VisitBinary(node as BinaryExpression);
            } else if (node is MethodCallExpression)
            {
                VisitMethodCall(node as MethodCallExpression);
            } 
        }

        private Expression VisitBinaryOnEqual(BinaryExpression node)
        {
            Visit(node.Left);
            Visit(node.Right);

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            MemberNames.Add(node.Member.Name);

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            ConstantValues.Add(node.Value.ToString());

            return node;
        }

        #endregion
    }
}

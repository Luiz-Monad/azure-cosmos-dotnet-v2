using Microsoft.Azure.Documents.Spatial;
using Microsoft.Azure.Documents.Sql;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Constructs <see cref="T:Microsoft.Azure.Documents.Sql.SqlScalarExpression" /> from a geometry <see cref="T:System.Linq.Expressions.Expression" />.
	/// </summary>
	internal static class GeometrySqlExpressionFactory
	{
		/// <summary>
		/// Constructs <see cref="T:Microsoft.Azure.Documents.Sql.SqlScalarExpression" /> from a geometry <see cref="T:System.Linq.Expressions.Expression" />.
		/// </summary>
		/// <param name="geometryExpression">
		/// Expression of type <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" />.
		/// </param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.Sql.SqlScalarExpression" /> representing geometry <paramref name="geometryExpression" />.</returns>.
		public static SqlScalarExpression Construct(Expression geometryExpression)
		{
			if (!typeof(Geometry).IsAssignableFrom(geometryExpression.Type))
			{
				throw new ArgumentException("geometryExpression");
			}
			if (geometryExpression.NodeType == ExpressionType.Constant)
			{
				return FromJToken(JObject.FromObject(((ConstantExpression)geometryExpression).Value));
			}
			Geometry o;
			try
			{
				o = Expression.Lambda<Func<Geometry>>(geometryExpression, Array.Empty<ParameterExpression>()).Compile()();
			}
			catch (Exception innerException)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.FailedToEvaluateSpatialExpression), innerException);
			}
			return FromJToken(JObject.FromObject(o));
		}

		/// <summary>
		/// Constructs <see cref="T:Microsoft.Azure.Documents.Sql.SqlScalarExpression" /> from a geometry <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="jToken">Json token.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.Sql.SqlScalarExpression" />.</returns>
		private static SqlScalarExpression FromJToken(JToken jToken)
		{
			switch (jToken.Type)
			{
			case JTokenType.Array:
				return SqlArrayCreateScalarExpression.Create(jToken.Select(FromJToken).ToArray());
			case JTokenType.Boolean:
				return SqlLiteralScalarExpression.Create(SqlBooleanLiteral.Create(jToken.Value<bool>()));
			case JTokenType.Null:
				return SqlLiteralScalarExpression.SqlNullLiteralScalarExpression;
			case JTokenType.String:
				return SqlLiteralScalarExpression.Create(SqlStringLiteral.Create(jToken.Value<string>()));
			case JTokenType.Object:
				return SqlObjectCreateScalarExpression.Create((from p in ((JObject)jToken).Properties()
				select SqlObjectProperty.Create(SqlPropertyName.Create(p.Name), FromJToken(p.Value))).ToArray());
			case JTokenType.Integer:
			case JTokenType.Float:
				return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create(jToken.Value<double>()));
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.UnexpectedTokenType, jToken.Type));
			}
		}
	}
}

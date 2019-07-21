using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal class SqlGeoNearCallScalarExpression : SqlScalarExpression
	{
		public const string NearMinimumDistanceName = "@nearMinimumDistance";

		public const string NearMaximumDistanceName = "@nearMaximumDistance";

		public SqlScalarExpression PropertyRef
		{
			get;
			private set;
		}

		public SqlScalarExpression Geometry
		{
			get;
			set;
		}

		public uint? NumberOfPoints
		{
			get;
			set;
		}

		public double MinimumDistance
		{
			get;
			set;
		}

		public double MaximumDistance
		{
			get;
			set;
		}

		private SqlGeoNearCallScalarExpression(SqlScalarExpression propertyRef, SqlScalarExpression geometry, uint? num = default(uint?), double minDistance = 0.0, double maxDistance = 10000.0)
			: base(SqlObjectKind.GeoNearCallScalarExpression)
		{
			PropertyRef = propertyRef;
			Geometry = geometry;
			NumberOfPoints = num;
			MinimumDistance = Math.Max(0.0, minDistance);
			MaximumDistance = Math.Max(0.0, maxDistance);
		}

		public static SqlGeoNearCallScalarExpression Create(SqlScalarExpression propertyRef, SqlScalarExpression geometry, uint? num = default(uint?), double minDistance = 0.0, double maxDistance = 10000.0)
		{
			return new SqlGeoNearCallScalarExpression(propertyRef, geometry, num, minDistance, maxDistance);
		}

		public override void Accept(SqlObjectVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}

		public override void Accept(SqlScalarExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlScalarExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlScalarExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}

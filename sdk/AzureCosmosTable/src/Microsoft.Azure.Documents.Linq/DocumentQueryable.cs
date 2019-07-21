using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Query;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// This class provides extension methods for converting a <see cref="T:System.Linq.IQueryable`1" /> object to a <see cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery`1" /> object.
	/// </summary>
	/// <remarks>
	///  The <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class provides implementation of standard query methods for querying resources in Azure Cosmos DB. 
	///  These methods enable you to express traversal, filter, and projection operations over data persisted in the Azure Cosmos DB service.  They are defined as methods that 
	///  extend IQueryable, and do not perform any querying directly.  Instead, their functionality is to create queries 
	///  based the resource and query expression provided.  The actual query execution occurs when enumeration forces the expression tree associated with an IQueryable object to be executed.
	/// </remarks>
	/// <seealso cref="T:Microsoft.Azure.Documents.IDocumentClient" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Client.DocumentClient" />
	public static class DocumentQueryable
	{
		/// <summary>
		/// Converts an IQueryable to IDocumentQuery which supports pagination and asynchronous execution in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">the type of object to query.</typeparam>
		/// <param name="query">the IQueryable{T} to be converted.</param>
		/// <returns>An IDocumentQuery{T} that can evaluate the query.</returns>
		/// <example>
		/// This example shows how to run a query asynchronously using the AsDocumentQuery() interface.
		///
		/// <code language="c#">
		/// <![CDATA[
		/// using (var queryable = client.CreateDocumentQuery<Book>(
		///     collectionLink,
		///     new FeedOptions { MaxItemCount = 10 })
		///     .Where(b => b.Title == "War and Peace")
		///     .AsDocumentQuery())
		/// {
		///     while (queryable.HasMoreResults) 
		///     {
		///         foreach(Book b in await queryable.ExecuteNextAsync<Book>())
		///         {
		///             // Iterate through books
		///         }
		///     }
		/// }
		/// ]]>
		/// </code>
		/// </example>
		public static IDocumentQuery<T> AsDocumentQuery<T>(this IQueryable<T> query)
		{
			return (IDocumentQuery<T>)query;
		}

		/// <summary>
		/// Returns the maximum value in a generic <see cref="T:System.Linq.IQueryable`1" />.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="source">A sequence of values to determine the maximum of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The maximum value in the sequence.</returns>
		public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<TSource>(Expression.Call(GetMethodInfoOf<IQueryable<TSource>, TSource>(Queryable.Max<TSource>), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Returns the minimum value in a generic <see cref="T:System.Linq.IQueryable`1" />.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="source">A sequence of values to determine the minimum of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The minimum value in the sequence.</returns>
		public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<TSource>(Expression.Call(GetMethodInfoOf<IQueryable<TSource>, TSource>(Queryable.Min<TSource>), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Decimal" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<decimal>(Expression.Call(GetMethodInfoOf<IQueryable<decimal>, decimal>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<decimal?>(Expression.Call(GetMethodInfoOf<IQueryable<decimal?>, decimal?>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Double" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double>(Expression.Call(GetMethodInfoOf<IQueryable<double>, double>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double?>(Expression.Call(GetMethodInfoOf<IQueryable<double?>, double?>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Single" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<float>(Expression.Call(GetMethodInfoOf<IQueryable<float>, float>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<float?>(Expression.Call(GetMethodInfoOf<IQueryable<float?>, float?>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Int32" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double>(Expression.Call(GetMethodInfoOf<IQueryable<int>, double>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double?>(Expression.Call(GetMethodInfoOf<IQueryable<int?>, double?>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Int64" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double>(Expression.Call(GetMethodInfoOf<IQueryable<long>, double>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double?>(Expression.Call(GetMethodInfoOf<IQueryable<long?>, double?>(Queryable.Average), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Decimal" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<decimal>(Expression.Call(GetMethodInfoOf<IQueryable<decimal>, decimal>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<decimal?>(Expression.Call(GetMethodInfoOf<IQueryable<decimal?>, decimal?>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Double" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double>(Expression.Call(GetMethodInfoOf<IQueryable<double>, double>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<double?>(Expression.Call(GetMethodInfoOf<IQueryable<double?>, double?>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Single" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<float>(Expression.Call(GetMethodInfoOf<IQueryable<float>, float>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<float?>(Expression.Call(GetMethodInfoOf<IQueryable<float?>, float?>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Int32" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<int>(Expression.Call(GetMethodInfoOf<IQueryable<int>, int>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<int?>(Expression.Call(GetMethodInfoOf<IQueryable<int?>, int?>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Int64" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<long>(Expression.Call(GetMethodInfoOf<IQueryable<long>, long>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="T:System.Nullable`1" /> values.
		/// </summary>
		/// <param name="source">A sequence of values to calculate the average of.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The average value in the sequence.</returns>
		public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<long?>(Expression.Call(GetMethodInfoOf<IQueryable<long?>, long?>(Queryable.Sum), source.Expression), cancellationToken);
		}

		/// <summary>
		/// Returns the number of elements in a sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="source">The sequence that contains the elements to be counted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The number of elements in the input sequence.</returns>
		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((IDocumentQueryProvider)source.Provider).ExecuteAsync<int>(Expression.Call(GetMethodInfoOf<IQueryable<TSource>, int>(Queryable.Count<TSource>), source.Expression), cancellationToken);
		}

		internal static IQueryable<TResult> AsSQL<TSource, TResult>(this IOrderedQueryable<TSource> source, SqlQuerySpec querySpec)
		{
			if (querySpec == null)
			{
				throw new ArgumentNullException("querySpec");
			}
			if (string.IsNullOrEmpty(querySpec.QueryText))
			{
				throw new ArgumentException("querySpec.QueryText");
			}
			return source.Provider.CreateQuery<TResult>(Expression.Call(null, GetMethodInfoOf((Expression<Func<IQueryable<object>>>)(() => ((IOrderedQueryable<TSource>)null).AsSQL((SqlQuerySpec)null))), new Expression[2]
			{
				source.Expression,
				Expression.Constant(querySpec)
			}));
		}

		internal static IQueryable<dynamic> AsSQL<TSource>(this IOrderedQueryable<TSource> source, SqlQuerySpec querySpec)
		{
			if (querySpec == null)
			{
				throw new ArgumentNullException("querySpec");
			}
			if (string.IsNullOrEmpty(querySpec.QueryText))
			{
				throw new ArgumentException("querySpec.QueryText");
			}
			return source.Provider.CreateQuery<object>(Expression.Call(null, GetMethodInfoOf((Expression<Func<IQueryable<object>>>)(() => ((IOrderedQueryable<TSource>)null).AsSQL((SqlQuerySpec)null))), new Expression[2]
			{
				source.Expression,
				Expression.Constant(querySpec)
			}));
		}

		internal static IOrderedQueryable<Document> CreateDocumentQuery(this IDocumentQueryClient client, string collectionLink, FeedOptions feedOptions = null, object partitionKey = null)
		{
			return new DocumentQuery<Document>(client, ResourceType.Document, typeof(Document), collectionLink, feedOptions, partitionKey);
		}

		internal static IQueryable<dynamic> CreateDocumentQuery(this IDocumentQueryClient client, string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null, object partitionKey = null)
		{
			return new DocumentQuery<Document>(client, ResourceType.Document, typeof(Document), collectionLink, feedOptions, partitionKey).AsSQL(querySpec);
		}

		private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
		{
			return ((MethodCallExpression)expression.Body).Method;
		}

		private static MethodInfo GetMethodInfoOf<T1, T2>(Func<T1, T2> func)
		{
			return RuntimeReflectionExtensions.GetMethodInfo(func);
		}
	}
}

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	///   A strongly-typed resource class, for looking up localized strings, etc.
	/// </summary>
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class ClientResources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		/// <summary>
		///   Returns the cached ResourceManager instance used by this class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("Microsoft.Azure.Documents.ClientResources", typeof(ClientResources).GetAssembly());
				}
				return resourceMan;
			}
		}

		/// <summary>
		///   Overrides the current thread's CurrentUICulture property for all
		///   resource lookups using this strongly typed resource class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		/// <summary>
		///   Looks up a localized string similar to The client does not have any valid token for the requested resource {0}..
		/// </summary>
		internal static string AuthTokenNotFound => ResourceManager.GetString("AuthTokenNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, member {0} of type {1} is invalid..
		/// </summary>
		internal static string BadQuery_IllegalMemberAccess => ResourceManager.GetString("BadQuery_IllegalMemberAccess", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} is unsupported in this context. Supported expressions are MemberAccess and ArrayIndex..
		/// </summary>
		internal static string BadQuery_InvalidArrayIndexExpression => ResourceManager.GetString("BadQuery_InvalidArrayIndexExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Type {0} for an array index parameter is invalid. Array index parameter must be int..
		/// </summary>
		internal static string BadQuery_InvalidArrayIndexType => ResourceManager.GetString("BadQuery_InvalidArrayIndexType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} must either have LHS or RHS as constant..
		/// </summary>
		internal static string BadQuery_InvalidComparison => ResourceManager.GetString("BadQuery_InvalidComparison", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} of type {1} cannot be used in this context..
		/// </summary>
		internal static string BadQuery_InvalidComparisonType => ResourceManager.GetString("BadQuery_InvalidComparisonType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} is unsupported. Supported expressions are 'Queryable.Where', 'Queryable.Select' &amp; 'Queryable.SelectMany'.
		/// </summary>
		internal static string BadQuery_InvalidExpression => ResourceManager.GetString("BadQuery_InvalidExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} is not allowed in this context..
		/// </summary>
		internal static string BadQuery_InvalidLeftExpression => ResourceManager.GetString("BadQuery_InvalidLeftExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} is unsupported in this context. Supported expressions are parameter reference, array index and property reference..
		/// </summary>
		internal static string BadQuery_InvalidMemberAccessExpression => ResourceManager.GetString("BadQuery_InvalidMemberAccessExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, method call {0} is not allowed at this context. Allowed methods are {1}..
		/// </summary>
		internal static string BadQuery_InvalidMethodCall => ResourceManager.GetString("BadQuery_InvalidMethodCall", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to QueryType {0} is not supported..
		/// </summary>
		internal static string BadQuery_InvalidQueryType => ResourceManager.GetString("BadQuery_InvalidQueryType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression return type {0} is unsupported. Query must evaluate to IEnumerable..
		/// </summary>
		internal static string BadQuery_InvalidReturnType => ResourceManager.GetString("BadQuery_InvalidReturnType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query expression is invalid, expression {0} contains too many arguments. .
		/// </summary>
		internal static string BadQuery_TooManySelectManyArguments => ResourceManager.GetString("BadQuery_TooManySelectManyArguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to An error occured while evaluating the transform expression {0}..
		/// </summary>
		internal static string BadQuery_TransformQueryException => ResourceManager.GetString("BadQuery_TransformQueryException", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Session object retrieved from client with endpoint {0} cannot be used on a client initialized to endpoint {1}..
		/// </summary>
		internal static string BadSession => ResourceManager.GetString("BadSession", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Binary operator '{0}' is not supported..
		/// </summary>
		internal static string BinaryOperatorNotSupported => ResourceManager.GetString("BinaryOperatorNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constant of type '{0}' is not supported..
		/// </summary>
		internal static string ConstantTypeIsNotSupported => ResourceManager.GetString("ConstantTypeIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constructor invocation is not supported..
		/// </summary>
		internal static string ConstructorInvocationNotSupported => ResourceManager.GetString("ConstructorInvocationNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected a static IQueryable or IEnumerable extension method, received an instance method..
		/// </summary>
		internal static string ExpectedMethodCallsMethods => ResourceManager.GetString("ExpectedMethodCallsMethods", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression with NodeType '{0}' is not supported..
		/// </summary>
		internal static string ExpressionTypeIsNotSupported => ResourceManager.GetString("ExpressionTypeIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression tree cannot be processed because evaluation of Spatial expression failed..
		/// </summary>
		internal static string FailedToEvaluateSpatialExpression => ResourceManager.GetString("FailedToEvaluateSpatialExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Input is not of type IDocumentQuery..
		/// </summary>
		internal static string InputIsNotIDocumentQuery => ResourceManager.GetString("InputIsNotIDocumentQuery", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Incorrect number of arguments for method '{0}'. Expected '{1}' but received '{2}'..
		/// </summary>
		internal static string InvalidArgumentsCount => ResourceManager.GetString("InvalidArgumentsCount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This method should only be called within Linq expression to Invoke a User-defined function..
		/// </summary>
		internal static string InvalidCallToUserDefinedFunctionProvider => ResourceManager.GetString("InvalidCallToUserDefinedFunctionProvider", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Range low value must be less than or equal the high value..
		/// </summary>
		internal static string InvalidRangeError => ResourceManager.GetString("InvalidRangeError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The count value provided for a Skip expression must be an integer..
		/// </summary>
		internal static string InvalidSkipValue => ResourceManager.GetString("InvalidSkipValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The count value provided for a Take expression must be an integer..
		/// </summary>
		internal static string InvalidTakeValue => ResourceManager.GetString("InvalidTakeValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Method '{0}' can not be invoked for type '{1}'. Supported types are '[{2}]'..
		/// </summary>
		internal static string InvalidTypesForMethod => ResourceManager.GetString("InvalidTypesForMethod", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to MediaLink is invalid.
		/// </summary>
		internal static string MediaLinkInvalid => ResourceManager.GetString("MediaLinkInvalid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Member binding is not supported..
		/// </summary>
		internal static string MemberBindingNotSupported => ResourceManager.GetString("MemberBindingNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Method '{0}' is not supported..
		/// </summary>
		internal static string MethodNotSupported => ResourceManager.GetString("MethodNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Not supported..
		/// </summary>
		internal static string NotSupported => ResourceManager.GetString("NotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Method '{0}' is not supported. Only LINQ Methods are supported..
		/// </summary>
		internal static string OnlyLINQMethodsAreSupported => ResourceManager.GetString("OnlyLINQMethodsAreSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to extract partition key from document. Ensure that you have provided a valid PartitionKeyValueExtractor function..
		/// </summary>
		internal static string PartitionKeyExtractError => ResourceManager.GetString("PartitionKeyExtractError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Partition property not found in the document..
		/// </summary>
		internal static string PartitionPropertyNotFound => ResourceManager.GetString("PartitionPropertyNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to An IPartitionResolver already exists for this database.
		/// </summary>
		internal static string PartitionResolver_DatabaseAlreadyExist => ResourceManager.GetString("PartitionResolver_DatabaseAlreadyExist", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No IPartitionResolver available for this database.
		/// </summary>
		internal static string PartitionResolver_DatabaseDoesntExist => ResourceManager.GetString("PartitionResolver_DatabaseDoesntExist", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only path expressions are supported for SelectMany..
		/// </summary>
		internal static string PathExpressionsOnly => ResourceManager.GetString("PathExpressionsOnly", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A containing range for {0} doesn't exist in the partition map..
		/// </summary>
		internal static string RangeNotFoundError => ResourceManager.GetString("RangeNotFoundError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The right hand side of string.CompareTo() comparison must be constant '0'.
		/// </summary>
		internal static string StringCompareToInvalidConstant => ResourceManager.GetString("StringCompareToInvalidConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid operator for string.CompareTo(). Vaid operators are ('==', '&lt;', '&lt;=', '&gt;' or '&gt;=').
		/// </summary>
		internal static string StringCompareToInvalidOperator => ResourceManager.GetString("StringCompareToInvalidOperator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to User-defined function name can not be null or empty..
		/// </summary>
		internal static string UdfNameIsNullOrEmpty => ResourceManager.GetString("UdfNameIsNullOrEmpty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unary operator '{0}' is not supported..
		/// </summary>
		internal static string UnaryOperatorNotSupported => ResourceManager.GetString("UnaryOperatorNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected authorization token type '({0})'. Expected '{1}'..
		/// </summary>
		internal static string UnexpectedAuthTokenType => ResourceManager.GetString("UnexpectedAuthTokenType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected token type: {0}.
		/// </summary>
		internal static string UnexpectedTokenType => ResourceManager.GetString("UnexpectedTokenType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unsupported type {0} for partitionKey..
		/// </summary>
		internal static string UnsupportedPartitionKey => ResourceManager.GetString("UnsupportedPartitionKey", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Instantiation of only value types, anonymous types and spatial types are supported..
		/// </summary>
		internal static string ValueAndAnonymousTypesAndGeometryOnly => ResourceManager.GetString("ValueAndAnonymousTypesAndGeometryOnly", resourceCulture);

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal ClientResources()
		{
		}
	}
}

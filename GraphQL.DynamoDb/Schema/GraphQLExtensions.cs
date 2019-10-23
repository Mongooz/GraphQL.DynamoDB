using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.DynamoDb.Schema
{
    internal static class GraphQLExtensions
    {
        private static FieldType AddDynamoDBField(this ComplexGraphType<Object> indexType, string attributeName, string attributeType)
        {
            switch (attributeType)
            {
                case "N":
                    return indexType.Field<IntGraphType>(attributeName, resolve: context => (context.Source as Dictionary<string, AttributeValue>)?[attributeName].N);
                case "S":
                    return indexType.Field<StringGraphType>(attributeName, resolve: context => (context.Source as Dictionary<string, AttributeValue>)?[attributeName].S);
                case "BOOL":
                    return indexType.Field<BooleanGraphType>(attributeName, resolve: context => (context.Source as Dictionary<string, AttributeValue>)?[attributeName].BOOL);
                case "SS":
                    return indexType.Field<ListGraphType<StringGraphType>>(attributeName, resolve: context => (context.Source as Dictionary<string, AttributeValue>)?[attributeName].SS);
                default:
                    return null;
            }
        }

        internal static IEnumerable<QueryArgument> ToQueryArguments(this GlobalSecondaryIndexDescription index, IEnumerable<AttributeDefinition> attributes)
        {
            foreach (var key in index.KeySchema)
            {
                yield return ToQueryArgument(attributes, key.AttributeName);
            }
            foreach (var attributeName in index.Projection.NonKeyAttributes)
            {
                yield return ToQueryArgument(attributes, attributeName);
            }
        }

        internal static IEnumerable<QueryArgument> ToQueryArguments(this LocalSecondaryIndexDescription index, IEnumerable<AttributeDefinition> attributes)
        {
            foreach (var key in index.KeySchema)
            {
                yield return ToQueryArgument(attributes, key.AttributeName);
            }
            foreach (var attributeName in index.Projection.NonKeyAttributes)
            {
                yield return ToQueryArgument(attributes, attributeName);
            }
        }
        internal static IEnumerable<QueryArgument> ToQueryArguments(this IEnumerable<KeySchemaElement> keySchemaElements, IEnumerable<AttributeDefinition> attributes)
        {
            foreach (var key in keySchemaElements)
            {
                yield return ToQueryArgument(attributes, key.AttributeName);
            }
        }

        private static QueryArgument ToQueryArgument(IEnumerable<AttributeDefinition> attributes, string attributeName)
        {
            string attributeType = attributes.FirstOrDefault(attribute => attribute.AttributeName == attributeName)?.AttributeType;

            switch (attributeType)
            {
                case "N": return new QueryArgument(new IntGraphType { Name = attributeName }) { Name = attributeName };
                default: return new QueryArgument(new StringGraphType { Name = attributeName }) { Name = attributeName };
            }
        }

        public static ObjectGraphType ToObjectGraphType(this IEnumerable<AttributeDefinition> attributes, string name, IEnumerable<(string, string)> additionalColumns)
        {
            var type = new ObjectGraphType { Name = name };

            foreach (var attribute in attributes)
            {
                type.AddDynamoDBField(attribute.AttributeName, attribute.AttributeType);
            }

            if (additionalColumns != null)
            {
                foreach (var attribute in additionalColumns.Where(attribute => attributes.Any(_ => _.AttributeName == attribute.Item1) == false))
                {
                    type.AddDynamoDBField(attribute.Item1, attribute.Item2);
                }
            }

            return type;
        }

        public static InputObjectGraphType ToInputObjectGraphType(this IEnumerable<AttributeDefinition> attributes, string name, IEnumerable<(string, string)> additionalColumns)
        {
            var input = new InputObjectGraphType { Name = name };

            foreach (var attribute in attributes)
            {
                input.AddDynamoDBField(attribute.AttributeName, attribute.AttributeType);
            }

            if (additionalColumns != null)
            {
                foreach (var attribute in additionalColumns.Where(attribute => attributes.Any(_ => _.AttributeName == attribute.Item1) == false))
                {
                    input.AddDynamoDBField(attribute.Item1, attribute.Item2);
                }
            }

            return input;
        }
    }
}

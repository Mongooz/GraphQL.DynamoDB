using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

namespace GraphQL.DynamoDB.Schema
{
    public class DynamoDBTable
    {
        public TableDescription TableDescription { get; set; }
        public IEnumerable<(string, string)> AdditionalColumns { get; set; }
    }
}

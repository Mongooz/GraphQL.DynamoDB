using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

namespace GraphQL.DynamoDb.Schema
{
    public class DynamoDBTable
    {
        public TableDescription TableDescription { get; set; }
        public IEnumerable<(string, string)> AdditionalColumns { get; set; }
    }
}

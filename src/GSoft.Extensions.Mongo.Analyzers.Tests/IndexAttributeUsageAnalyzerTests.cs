﻿namespace GSoft.Extensions.Mongo.Analyzers.Tests;

public class IndexAttributeUsageAnalyzerTests : BaseAnalyzerTest<IndexAttributeUsageAnalyzer>
{
    [Theory]
    [InlineData("[IndexedBy(\"PrimaryKey\")]")]
    [InlineData("[NoIndexNeeded(\"Default index used\")]")]
    public async Task Given_IndexAttribute_When_Analyze_Then_No_Diagnostics(string attribute)
    {
        const string source = @"
public class PersonDocument : IMongoDocument {{ }}

{0}
public class MyWorker
{{
    public void DoSomething()
    {{
        var collection = (IMongoCollection<PersonDocument>)null!;
        _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
    }}
}}";

        await this.WithSourceCode(string.Format(source, attribute))
            .RunAsync();
    }

    [Fact]
    public async Task Given_No_IndexAttribute_When_Analyze_Then_Diagnostic()
    {
        const string source = @"
public class PersonDocument : IMongoDocument { }

public class MyWorker
{
    public void DoSomething()
    {
        var collection = (IMongoCollection<PersonDocument>)null!;
        _ = collection.CountDocuments(FilterDefinition<PersonDocument>.Empty);
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 4, startColumn: 14, endLine: 4, endColumn: 22)
            .RunAsync();
    }

    [Fact]
    public async Task Given_No_IndexAttribute_And_Multiple_Mongo_Usage_When_Analyze_Then_Only_One_Diagnostic()
    {
        const string source = @"
public class PersonDocument : IMongoDocument { }

public class MyWorker
{
    public void DoSomething()
    {
        var collection = (IMongoCollection<PersonDocument>)null!;
        _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
        _ = collection.CountDocuments(FilterDefinition<PersonDocument>.Empty);
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 4, startColumn: 14, endLine: 4, endColumn: 22)
            .RunAsync();
    }

    [Fact]
    public async Task Given_No_IndexAttribute_And_Partial_Class_When_Analyze_Then_Diagnostic_On_Each_Class()
    {
        const string source = @"
public class PersonDocument : IMongoDocument { }

public partial class MyWorker
{
    public void DoSomething()
    {
    }
}

public partial class MyWorker
{
    public void DoSomething2()
    {
        var collection = (IMongoCollection<PersonDocument>)null!;
        _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 4, startColumn: 22, endLine: 4, endColumn: 30)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Two_Classes_In_Different_Namespace_When_Analyze_And_Only_One_Uses_Mongo_Then_Diagnostic_On_Only_One_Class()
    {
        const string source = @"
namespace FirstClass
{
    public class PersonDocument : IMongoDocument { }

    public class MyWorker
    {
        public void DoSomething()
        {
        }
    }
}

namespace SecondClass
{
    public class PersonDocument : IMongoDocument { }

    public class MyWorker
    {
        public void DoSomething()
        {
            var collection = (IMongoCollection<PersonDocument>)null!;
            _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
        }
    }
}
";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 18, startColumn: 18, endLine: 18, endColumn: 26)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Two_Classes_In_Different_Namespace_When_Analyze_And_One_Uses_Attribute_Then_Diagnostic_On_Only_One_Class()
    {
        const string source = @"
namespace FirstClass
{
    public class PersonDocument : IMongoDocument { }

    [NoIndexNeeded(""Default index used"")]
    public class MyWorker
    {
        public void DoSomething()
        {
            var collection = (IMongoCollection<PersonDocument>)null!;
            _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
        }
    }
}

namespace SecondClass
{
    public class PersonDocument : IMongoDocument { }

    public class MyWorker
    {
        public void DoSomething()
        {
            var collection = (IMongoCollection<PersonDocument>)null!;
            _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
        }
    }
}
";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 21, startColumn: 18, endLine: 21, endColumn: 26)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Two_Classes_In_Different_Namespace_When_Analyze_Both_Miss_Attribute_Then_Diagnostic_Both_Classes()
    {
        const string source = @"
namespace FirstClass
{
    public class PersonDocument : IMongoDocument { }

    public class MyWorker
    {
        public void DoSomething()
        {
            var collection = (IMongoCollection<PersonDocument>)null!;
            _ = collection.CountDocuments(FilterDefinition<PersonDocument>.Empty);
        }
    }
}

namespace SecondClass
{
    public class PersonDocument : IMongoDocument { }

    public class MyWorker
    {
        public void DoSomething()
        {
            var collection = (IMongoCollection<PersonDocument>)null!;
            _ = collection.Find(FilterDefinition<PersonDocument>.Empty);
        }
    }
}
";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 6, startColumn: 18, endLine: 6, endColumn: 26)
            .WithExpectedDiagnostic(IndexAttributeUsageAnalyzer.UseIndexAttributeRule, startLine: 20, startColumn: 18, endLine: 20, endColumn: 26)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Non_Mongo_Invocation_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
public class PersonDocument : IMongoDocument { }

public class MyWorker
{
    public void DoSomething()
    {
        var number = int.Parse(""42"");
    }
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }
}
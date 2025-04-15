// This file is based on https://github.com/mongodb/mongo-csharp-driver/blob/v3.3.0/src/MongoDB.Driver/Core/Operations/IndexNameHelper.cs
// Copyright 2010-present MongoDB Inc., licensed under the Apache License, Version 2.0.
//
// The file was modified for conditional compilation, StringBuilder optimizations and nullable reference types,
// and these modifications are Copyright (c) Workleap, 2025.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if MONGODB_V3

using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

#pragma warning disable IDE0130 // Namespace does not match folder structure - to match the original file
namespace MongoDB.Driver.Core.Operations;
#pragma warning restore IDE0130

internal static class IndexNameHelper
{
    public static string GetIndexName(BsonDocument keys)
    {
        Ensure.IsNotNull(keys, nameof(keys));
        var sb = new StringBuilder();

        foreach (var element in keys)
        {
            var value = element.Value;
            string direction;
            switch (value.BsonType)
            {
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                    direction = value.ToInt32().ToString();
                    break;
                case BsonType.String:
                    direction = value.ToString()!.Replace(' ', '_');
                    break;
                default:
                    direction = "x";
                    break;
            }

            if (sb.Length > 0)
            {
                sb.Append('_');
            }

            sb.Append(element.Name.Replace(' ', '_'));
            sb.Append('_');
            sb.Append(direction);
        }

        return sb.ToString();
    }

    public static string GetIndexName(string[] keyNames)
    {
        Ensure.IsNotNull(keyNames, nameof(keyNames));
        var sb = new StringBuilder();

        foreach (var name in keyNames)
        {
            if (sb.Length > 0)
            {
                sb.Append('_');
            }

            sb.Append(name.Replace(' ', '_'));
            sb.Append("_1");
        }

        return sb.ToString();
    }
}

#endif
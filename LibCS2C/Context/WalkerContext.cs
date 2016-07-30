﻿using LibCS2C.Generators;
using LibCS2C.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace LibCS2C.Context
{
    public class WalkerContext
    {
        /// <summary>
        /// Generic type conversion helper
        /// </summary>
        public GenericTypeConversion GenericTypeConvert { get; private set; } = new GenericTypeConversion();

        /// <summary>
        /// Type conversion helper
        /// </summary>
        public TypeConversion TypeConvert { get; private set; }

        /// <summary>
        /// .cctor list
        /// </summary>
        public List<string> CctorList { get; private set; } = new List<string>();

        /// <summary>
        /// The current class
        /// </summary>
        public ClassDeclarationSyntax CurrentClass { get; set; }

        /// <summary>
        /// The current namespace
        /// </summary>
        public NamespaceDeclarationSyntax CurrentNamespace { get; set; }
        
        /// <summary>
        /// Gets the semantic Model
        /// </summary>
        public SemanticModel Model { get; set; }

        /// <summary>
        /// Gets the current namespace name formatted
        /// </summary>
        public string CurrentNamespaceFormatted { get { return CurrentNamespace.Name.ToString().Replace(".", "_"); } }

        /// <summary>
        /// A list with all the generators
        /// </summary>
        public AllGenerators Generators { get; private set; }

        /// <summary>
        /// The writer
        /// </summary>
        public Writer Writer { get; private set; }

        /// <summary>
        /// Contextwalker
        /// </summary>
        /// <param name="sb">The formatted string builder</param>
        public WalkerContext()
        {
            Generators = new AllGenerators(this);
            TypeConvert = new TypeConversion(this);
            Writer = new Writer();
        }
        
        /// <summary>
        /// Converts the C# type to a C type name
        /// </summary>
        /// <param name="type">The C# type</param>
        /// <returns>The C type name</returns>
        public string ConvertTypeName(SyntaxNode type)
        {
            if (GenericTypeConvert.IsGeneric(type as TypeSyntax))
            {
                return GenericTypeConvert.Convert(type as TypeSyntax);
            }
            else
            {
                return TypeConvert.ConvertTypeName(type);
            }
        }
    }
}

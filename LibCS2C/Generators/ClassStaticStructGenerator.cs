﻿using LibCS2C.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibCS2C.Generators
{
    class ClassStaticStructGenerator : GeneratorBase<ClassDeclarationSyntax>
    {
        private ClassCodeData m_classCode;

        /// <summary>
        /// Class struct generator
        /// </summary>
        /// <param name="context">The walker context</param>
        /// <param name="classCode">Class code</param>
        public ClassStaticStructGenerator(WalkerContext context, ClassCodeData classCode)
        {
            m_context = context;
            m_classCode = classCode;
        }

        /// <summary>
        /// Generate the initializers
        /// </summary>
        /// <param name="types">Types</param>
        /// <param name="values">Values</param>
        private void GenerateInitializer(Dictionary<string, TypeSyntax> types, Dictionary<string, EqualsValueClauseSyntax> values)
        {
            // Loop through and initialize if possible
            foreach (KeyValuePair<string, TypeSyntax> pair in types)
            {
                EqualsValueClauseSyntax value;
                if (values.TryGetValue(pair.Key, out value))
                {
                    ExpressionSyntax expression = value.Value;

                    // If it's a literal, we can initialize it safely inside the struct
                    if (m_context.Generators.Expression.IsLiteralExpression(expression.Kind()))
                    {
                        m_context.Generators.Expression.Generate(expression);
                        m_context.Writer.AppendLine(",");
                    }
                    else
                    {
                        // Uninitialized for now
                        m_context.Writer.AppendLine("0,");
                    }
                }
                else
                {
                    // Uninitialized for now
                    // About C: if a struct should be initialized to zeroes, we need {0} instead of 0
                    ITypeSymbol typeSymbol = m_context.Model.GetTypeInfo(pair.Value).Type;
                    if (!m_context.GenericTypeConvert.IsGeneric(typeSymbol) && typeSymbol.TypeKind == TypeKind.Struct)
                    {
                        m_context.Writer.AppendLine("{0},");
                    }
                    else
                    {
                        m_context.Writer.AppendLine("0,");
                    }
                }
            }
        }

        /// <summary>
        /// Generates a struct member
        /// </summary>
        /// <param name="name">The name of the member</param>
        /// <param name="type">The type of the member</param>
        private void GenerateStructMember(string name, TypeSyntax type)
        {
            string typeName = m_context.ConvertTypeName(type);

            // Check if there's a variable initializer
            // If there is one, we need to change the type from pointer to array
            // so C knows that it needs to reserve memory
            if (m_classCode.staticFields.ContainsKey(name))
            {
                ExpressionSyntax expression = m_classCode.staticFields[name].Value;
                if (expression.Kind() == SyntaxKind.ArrayInitializerExpression)
                {
                    InitializerExpressionSyntax initializer = expression as InitializerExpressionSyntax;
                    typeName = typeName.Substring(0, typeName.Length - 1);
                    name += "[" + initializer.Expressions.Count() + "]";
                }
            }

            // Check for extra modifiers
            IEnumerable<SyntaxToken> tokens = type.Parent.Parent.ChildTokens();
            foreach (SyntaxToken token in tokens)
            {
                if (token.Kind() == SyntaxKind.VolatileKeyword)
                {
                    m_context.Writer.Append("volatile ");
                    break;
                }
            }

            m_context.Writer.AppendLine(string.Format("{0} {1};", typeName, name));
        }

        /// <summary>
        /// Generates the static fields class struct
        /// </summary>
        /// <param name="node">The class declaration</param>
        public override void Generate(ClassDeclarationSyntax node)
        {
            // Are there even static fields?
            if (m_classCode.staticFieldTypes.Count == 0 && m_classCode.propertyTypesStatic.Count == 0)
                return;

            string convertedClassName = m_context.TypeConvert.ConvertClassName(node.Identifier.ToString());

            m_context.Writer.AppendLine("struct");
            m_context.Writer.AppendLine("{");

            foreach (KeyValuePair<string, TypeSyntax> pair in m_classCode.staticFieldTypes)
            {
                GenerateStructMember(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, TypeSyntax> pair in m_classCode.propertyTypesStatic)
            {
                GenerateStructMember("prop_" + pair.Key, pair.Value);
            }

            m_context.Writer.Append("} classStatics_");
            m_context.Writer.Append(convertedClassName);

            // Initializers
            m_context.Writer.AppendLine(" = {");
            m_context.Writer.Indent();

            GenerateInitializer(m_classCode.staticFieldTypes, m_classCode.staticFields);
            GenerateInitializer(m_classCode.propertyTypesStatic, m_classCode.propertyInitialValuesStatic);

            m_context.Writer.UnIndent();
            m_context.Writer.AppendLine("};");
        }
    }
}

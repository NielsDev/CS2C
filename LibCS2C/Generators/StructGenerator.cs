﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCS2C.Generators
{
    public class StructGenerator : GeneratorBase<StructDeclarationSyntax>
    {
        /// <summary>
        /// struct generator
        /// </summary>
        /// <param name="context">The walker context</param>
        public StructGenerator(WalkerContext context)
        {
            m_context = context;
        }

        /// <summary>
        /// Generates a struct declaration
        /// </summary>
        /// <param name="node">The struct declaration</param>
        public override void Generate(StructDeclarationSyntax node)
        {
            WriterDestination destination = m_context.CurrentDestination;
            m_context.CurrentDestination = WriterDestination.Structs;

            // Check for attributes
            bool packed = false;

            SyntaxList<AttributeListSyntax> attribLists = node.AttributeLists;
            foreach(AttributeListSyntax attribList in attribLists)
            {
                SeparatedSyntaxList<AttributeSyntax> attribs = attribList.Attributes;
                foreach(AttributeSyntax attrib in attribs)
                {
                    IdentifierNameSyntax name = attrib.ChildNodes().First() as IdentifierNameSyntax;
                    string identifier = name.Identifier.ToString();
                    
                    // Defines layout of the struct
                    if(identifier.Equals("StructLayoutAttribute") || identifier.Equals("StructLayout"))
                    {
                        SeparatedSyntaxList<AttributeArgumentSyntax> argsList = attrib.ArgumentList.Arguments;
                        foreach(AttributeArgumentSyntax arg in argsList)
                        {
                            SyntaxNode first = arg.ChildNodes().First();
                            SyntaxKind kind = first.Kind();
                            
                            if(kind == SyntaxKind.NameEquals)
                            {
                                NameEqualsSyntax nameEquals = first as NameEqualsSyntax;
                                string nameIdentifier = nameEquals.Name.Identifier.ToString();

                                if(nameIdentifier.Equals("Pack"))
                                {
                                    // TODO: support more sizes for packing
                                    packed = true;
                                }
                            }
                        }
                    }
                    // Unknown attribute
                    else
                    {
                        Console.WriteLine("Unknown attribute on struct: " + identifier);
                    }
                }
            }

            // Create struct name
            string structName;
            if (node.Parent is ClassDeclarationSyntax)
            {
                structName = string.Format("{0}_{1}", m_context.CurrentClassNameFormatted, node.Identifier.ToString());
            }
            else
            {
                structName = string.Format("{0}_{1}", m_context.CurrentNamespaceFormatted, node.Identifier.ToString());
            }

            // Temporarily hold all the data
            Dictionary<string, EqualsValueClauseSyntax> data = new Dictionary<string, EqualsValueClauseSyntax>();
            Dictionary<string, TypeSyntax> dataTypes = new Dictionary<string, TypeSyntax>();

            // Collect the data and put it in the dictionaries
            IEnumerable<SyntaxNode> children = node.ChildNodes();
            foreach (SyntaxNode child in children)
            {
                SyntaxKind kind = child.Kind();

                if (kind == SyntaxKind.FieldDeclaration)
                {
                    FieldDeclarationSyntax field = child as FieldDeclarationSyntax;
                    IEnumerable<SyntaxNode> fieldChildren = field.ChildNodes();

                    foreach (VariableDeclarationSyntax fieldChild in fieldChildren)
                    {
                        foreach (VariableDeclaratorSyntax variable in fieldChild.Variables)
                        {
                            string identifier = "field_" + variable.Identifier.ToString();
                            dataTypes.Add(identifier, fieldChild.Type);
                        }
                    }
                }
                else if(kind == SyntaxKind.PropertyDeclaration)
                {
                    PropertyDeclarationSyntax property = child as PropertyDeclarationSyntax;
                    string identifier = "prop_" + property.Identifier.ToString();
                    dataTypes.Add(identifier, property.Type);
                }
            }

            // Struct
            m_context.Writer.AppendLine(string.Format("struct struct_{0}", structName));
            m_context.Writer.AppendLine("{");
            
            foreach (KeyValuePair<string, TypeSyntax> pair in dataTypes)
            {
                m_context.Writer.AppendLine(string.Format("\t{0} {1};", m_context.ConvertTypeName(pair.Value), pair.Key));
            }

            // Attributes
            if(packed)
                m_context.Writer.AppendLine("} __attribute__((packed));");
            else
                m_context.Writer.AppendLine("};");
            

            // Method prototype of init code
            string methodName = string.Format("struct struct_{0} structInit_{0}(void)", structName);
            m_context.CurrentDestination = WriterDestination.MethodPrototypes;
            m_context.Writer.Append(methodName);
            m_context.Writer.AppendLine(";");

            // Init method declaration
            m_context.CurrentDestination = WriterDestination.MethodDeclarations;
            m_context.Writer.AppendLine(methodName);
            m_context.Writer.AppendLine("{");
            m_context.Writer.AppendLine(string.Format("\tstruct struct_{0} object;", structName));

            // Loop through the fields and initialize them
            foreach (KeyValuePair<string, EqualsValueClauseSyntax> pair in data)
            {
                m_context.Writer.Append(string.Format("\tobject.field_{0} = ", pair.Key));
                ExpressionSyntax expression = pair.Value.Value;
                m_context.Generators.Expression.Generate(expression);
                m_context.Writer.AppendLine(";");
            }

            m_context.Writer.AppendLine("\treturn object;");
            m_context.Writer.AppendLine("}");

            m_context.CurrentDestination = destination;
        }
    }
}

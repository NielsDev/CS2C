﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_2_C.Generators
{
    class ClassCodeGenerator : GeneratorBase<ClassDeclarationSyntax>
    {
        /// <summary>
        /// Class field generator
        /// </summary>
        /// <param name="context">The walker context</param>
        public ClassCodeGenerator(WalkerContext context)
        {
            m_context = context;
        }

        /// <summary>
        /// Calls the class fields and .cctor generators
        /// </summary>
        /// <param name="node">The class declaration</param>
        public override void Generate(ClassDeclarationSyntax node)
        {
            // Temporarily hold all the fields/properties so we can put them in the initialization method
            Dictionary<string, EqualsValueClauseSyntax> staticFields = new Dictionary<string, EqualsValueClauseSyntax>();
            Dictionary<string, EqualsValueClauseSyntax> nonStaticFields = new Dictionary<string, EqualsValueClauseSyntax>();
            Dictionary<string, TypeSyntax> staticFieldTypes = new Dictionary<string, TypeSyntax>();
            Dictionary<string, TypeSyntax> nonStaticFieldTypes = new Dictionary<string, TypeSyntax>();
            Dictionary<string, TypeSyntax> propertyTypes = new Dictionary<string, TypeSyntax>();
            Dictionary<string, EqualsValueClauseSyntax> propertyInitialValues = new Dictionary<string, EqualsValueClauseSyntax>();

            // Loop through the children to find the fields
            IEnumerable<SyntaxNode> nodes = node.ChildNodes();
            foreach (SyntaxNode childNode in nodes)
            {
                SyntaxKind kind = childNode.Kind();

                // Found a field
                if (kind == SyntaxKind.FieldDeclaration)
                {
                    FieldDeclarationSyntax fieldNode = childNode as FieldDeclarationSyntax;
                    IEnumerable<SyntaxNode> fieldNodeChildren = fieldNode.ChildNodes();

                    bool isStatic = false;

                    IEnumerable<SyntaxToken> fieldNodeTokens = fieldNode.ChildTokens();
                    foreach (SyntaxToken token in fieldNodeTokens)
                    {
                        if (token.Kind() == SyntaxKind.StaticKeyword)
                            isStatic = true;
                    }

                    foreach (SyntaxNode fieldNodeChild in fieldNodeChildren)
                    {
                        VariableDeclarationSyntax variableDeclaration = fieldNodeChild as VariableDeclarationSyntax;
                        foreach (VariableDeclaratorSyntax variable in variableDeclaration.Variables)
                        {
                            string identifier = variable.Identifier.ToString();
                            if (isStatic)
                            {
                                if (variable.Initializer != null)
                                    staticFields.Add(identifier, variable.Initializer);

                                staticFieldTypes.Add(identifier, variableDeclaration.Type);
                            }
                            else
                            {
                                if (variable.Initializer != null)
                                    nonStaticFields.Add(identifier, variable.Initializer);

                                nonStaticFieldTypes.Add(identifier, variableDeclaration.Type);
                            }
                        }
                    }
                }
                // Found a property
                else if(kind == SyntaxKind.PropertyDeclaration)
                {
                    PropertyDeclarationSyntax propertyDeclaration = childNode as PropertyDeclarationSyntax;
                    string identifier = propertyDeclaration.Identifier.ToString();
                    
                    propertyTypes.Add(identifier, propertyDeclaration.Type);
                    propertyInitialValues.Add(identifier, propertyDeclaration.Initializer);
                }
            }

            // Other generators
            ClassStructGenerator structGen = new ClassStructGenerator(m_context, nonStaticFieldTypes, propertyTypes);
            ClassStaticStructGenerator staticStructGen = new ClassStaticStructGenerator(m_context, staticFieldTypes);
            ClassCctorGenerator cctorGen = new ClassCctorGenerator(m_context, staticFields);
            ClassInitGenerator classInitGen = new ClassInitGenerator(m_context, nonStaticFields, propertyInitialValues);

            structGen.Generate(node);
            staticStructGen.Generate(node);
            cctorGen.Generate(node);
            classInitGen.Generate(node);
        }
    }
}

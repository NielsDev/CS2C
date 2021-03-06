﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LibCS2C.Context;

namespace LibCS2C.Compilation
{
    public class SyntaxWalker : CSharpSyntaxWalker
    {
        private WalkerContext m_context;
        private string m_initSuffix;

        /// <summary>
        /// Walks through the syntax and outputs C code to a string
        /// </summary>
        public SyntaxWalker(string initSuffix) : base(SyntaxWalkerDepth.Node)
        {
            m_context = new WalkerContext();
            m_initSuffix = initSuffix;
        }

        /// <summary>
        /// Sets the current document
        /// </summary>
        /// <param name="doc">The document</param>
        public void SetDocument(Document doc)
        {
            m_context.Model = doc.GetSemanticModelAsync().Result;
        }

        /// <summary>
        /// Visits a struct declaration
        /// </summary>
        /// <param name="node">The struct declaration node</param>
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            m_context.Writer.CurrentDestination = WriterDestination.Structs;
            m_context.Generators.Struct.Generate(node);
            base.VisitStructDeclaration(node);
        }

        /// <summary>
        /// Visits a class declaration
        /// </summary>
        /// <param name="node">The class declaration node</param>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            m_context.CurrentClass = node;
            m_context.MethodTable.AddCurrentClass();
            m_context.Generators.ClassCode.Generate(node);
            base.VisitClassDeclaration(node);
        }

        /// <summary>
        /// Visits an interface declaration
        /// </summary>
        /// <param name="node">The interface declaration node</param>
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            m_context.Generators.Interface.Generate(node);
            base.VisitInterfaceDeclaration(node);
        }

        /// <summary>
        /// Visit a constructor declaration
        /// </summary>
        /// <param name="node">The constructor declaration node</param>
        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            m_context.Generators.MethodDeclaration.Generate(node);
            base.VisitConstructorDeclaration(node);
        }

        /// <summary>
        /// Visits a method declaration
        /// </summary>
        /// <param name="node">The method declaration node</param>
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            m_context.Generators.MethodDeclaration.Generate(node);
            base.VisitMethodDeclaration(node);
        }

        /// <summary>
        /// Visits a property declaration
        /// </summary>
        /// <param name="node">The property node</param>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            m_context.Generators.Property.Generate(node);
            base.VisitPropertyDeclaration(node);
        }

        /// <summary>
        /// Visits an enum declaration
        /// </summary>
        /// <param name="node">The enum node</param>
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            m_context.Generators.Enum.Generate(node);
            base.VisitEnumDeclaration(node);
        }

        /// <summary>
        /// Visits a namespace declaration
        /// </summary>
        /// <param name="node">The namespace declaration node</param>
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            m_context.CurrentNamespace = node;
            base.VisitNamespaceDeclaration(node);
        }

        /// <summary>
        /// Visits a delegate declaration
        /// </summary>
        /// <param name="node">The delegate declaration node</param>
        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            m_context.Generators.DelegateDeclaration.Generate(node);
            base.VisitDelegateDeclaration(node);
        }

        /// <summary>
        /// Generates the header code
        /// </summary>
        /// <returns>The header</returns>
        public StringBuilder GetHeaderCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#ifndef __TYPES_DEFINED__");
            sb.AppendLine("#define __TYPES_DEFINED__");

            // Error message
            string[][] errors =
            {
                new string[] { CompilerSettings.RuntimeErrorNullCalledName, CompilerSettings.RuntimeErrorNullCalled }
            };
            foreach (string[] error in errors)
            {
                sb.AppendLine(string.Format("const static char* {0} = \"{1}\";", error));
            }

            // Types
            string[][] types =
            {
                new string[]{ "action_t", "void*" },
                new string[]{ "object_t", "void*" },
                new string[]{ "bool_t", "int32_t" },
                new string[]{ "string_t", "char*" }
            };
            foreach (string[] type in types)
            {
                sb.AppendLine(string.Format("typedef {1} {0};", type));
            }

            // Default class
            sb.AppendLine("struct base_class");
            sb.AppendLine("{");
            sb.AppendLine("\tvoid** lookup_table;");
            sb.AppendLine("};");

            sb.AppendLine("#endif");

            // .cctor prototype
            sb.AppendLine(string.Format("void init{0}(void);", m_initSuffix));

            // Code
            sb.AppendLine(m_context.Writer.SbEnums.ToString());
            sb.AppendLine(m_context.Writer.SbStructPrototypes.ToString());
            sb.AppendLine(m_context.Writer.SbDelegates.ToString());
            sb.AppendLine(m_context.Writer.SbStructs.ToString());
            sb.AppendLine(m_context.Writer.SbClassStructs.ToString());
            sb.AppendLine(m_context.Writer.SbMethodPrototypes.ToString());
            sb.AppendLine(m_context.MethodTable.ToPrototypeArrayCode());

            return sb;
        }

        /// <summary>
        /// Outputs the source code
        /// </summary>
        /// <returns>The source code</returns>
        public StringBuilder GetSourceCode()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine(m_context.Writer.SbMethodDeclarations.ToString());
            sb.AppendLine(m_context.MethodTable.ToArrayCode());

            // Add .cctor calls in init method
            sb.AppendLine(string.Format("void init{0}(void)", m_initSuffix));
            sb.AppendLine("{");
            foreach (string cctor in m_context.CctorList)
            {
                sb.AppendLine("\t" + cctor + "();");
            }
            sb.AppendLine("}");
            
            return sb;
        }
    }
}

﻿using LibCS2C.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCS2C.Generators
{
    class ClassStructGenerator : GeneratorBase<ClassDeclarationSyntax>
    {
        private ClassCodeData m_classCode;

        /// <summary>
        /// Class struct generator
        /// </summary>
        /// <param name="context">The walker context</param>
        /// <param name="classCode">Class code</param>
        public ClassStructGenerator(WalkerContext context, ClassCodeData classCode)
        {
            m_context = context;
            m_classCode = classCode;
        }

        /// <summary>
        /// Generates the class struct
        /// </summary>
        /// <param name="node">The class declaration</param>
        public override void Generate(ClassDeclarationSyntax node)
        {
            m_context.Writer.AppendLine(m_context.TypeConvert.CurrentClassStructName);
            m_context.Writer.AppendLine("{");

            // Usage count for garbage collector
            m_context.Writer.AppendLine("\tint32_t usage_count;");

            foreach (KeyValuePair<string, TypeSyntax> pair in m_classCode.nonStaticFieldTypes)
            {
                m_context.Writer.AppendLine("\t/* Field: " + pair.Key + " */");
                m_context.Writer.AppendLine(string.Format("\t{0} field_{1};", m_context.ConvertTypeName(pair.Value), pair.Key));
            }

            foreach (KeyValuePair<string, TypeSyntax> pair in m_classCode.propertyTypesNonStatic)
            {
                m_context.Writer.AppendLine("\t/* Property: " + pair.Key + " */");
                m_context.Writer.AppendLine(string.Format("\t{0} prop_{1};", m_context.ConvertTypeName(pair.Value), pair.Key));
            }

            m_context.Writer.AppendLine("};");
            m_context.Writer.AppendLine("");
        }
    }
}

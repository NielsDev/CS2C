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
    class ReturnStatementGenerator : GeneratorBase<ReturnStatementSyntax>
    {
        private ExpressionGenerator m_expressionGen;

        /// <summary>
        /// Return statement generator
        /// </summary>
        /// <param name="context">The walker context</param>
        public ReturnStatementGenerator(WalkerContext context)
        {
            m_context = context;
            m_expressionGen = new ExpressionGenerator(m_context);
        }

        /// <summary>
        /// Generates the return statement code
        /// </summary>
        /// <param name="node">The return statement</param>
        public override void Generate(ReturnStatementSyntax node)
        {
            m_context.Writer.Append("return ");
            m_expressionGen.Generate(node.Expression);
            m_context.Writer.AppendLine(";");
        }
    }
}
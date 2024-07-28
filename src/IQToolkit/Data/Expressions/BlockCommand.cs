// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A sequence of command expressions.
    /// </summary>
    public sealed class BlockCommand : CommandExpression
    {
        /// <summary>
        /// The commands that are executed sequentially in the block.
        /// </summary>
        public IReadOnlyList<Expression> Commands { get; }

        public BlockCommand(IReadOnlyList<Expression> commands)
            : base(commands[commands.Count-1].Type)
        {
            this.Commands = commands.ToReadOnly();
        }

        public BlockCommand(params Expression[] commands) 
            : this((IReadOnlyList<Expression>)commands)
        {
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Block;

        public BlockCommand Update(IReadOnlyList<Expression> commands)
        {
            if (this.Commands != commands)
            {
                return new BlockCommand(commands);
            }
            else
            {
                return this;
            }
        }
    }
}

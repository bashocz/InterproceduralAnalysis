using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    enum Tokens
    {
        // process tokens
        End = 0,
        Error,
        Comment,
        // command tokens
        VarCmd,
        FunctionCmd,
        IfCmd,
        ElseCmd,
        ForCmd,
        WhileCmd,
        GotoCmd,
        ReturnCmd,
        // variable tokens
        Identifier,
        // number tokens
        Number,
        // paranthesis tokens
        ParenthesisLeft,
        ParenthesisRight,
        BraceLeft,
        BraceRight,
        // opearation tokens
        Plus,
        Minus,
        Multi,
        PlusPlus,
        MinusMinus,
        Equals,
        // comparision tokens
        EqualsEquals,
        Less,
        More,
        LessOrEquals,
        MoreOrEquals,
        NotEquals,
        // logical tokens
        Or,
        And,
        Neg,
        // delimiter tokens
        Semicolon,
        Comma,
        Colon,
    }

    class BaseAstNode
    {
        public Tokens Token { get; set; }
        public string TokenText { get; set; }

        public int LineStart { get; set; }
        public int ColStart { get; set; }

        public int LineEnd { get; set; }
        public int ColEnd { get; set; }
    }

    class StatementAstNode : BaseAstNode
    {
        public StatementAstNode()
        {
            Commands = new List<BaseAstNode>();
        }

        public List<BaseAstNode> Commands { get; private set; }
    }

    class FunctionAstNode : BaseAstNode
    {
        public BaseAstNode Name { get; set; }

        public StatementAstNode Body { get; set; }
    }

    class ConditionAstNode : BaseAstNode
    {
        public BaseAstNode Condition { get; set; }
    }

    class IfAstNode : BaseAstNode
    {
        // just for graph construct purpose
        public BaseAstNode ParentL { get; set; }
        public BaseAstNode ParentR { get; set; }
        // just for graph construct purpose

        public BaseAstNode Condition { get; set; }
        public BaseAstNode IfBody { get; set; }
        public BaseAstNode ElseBody { get; set; }
    }

    class WhileAstNode : BaseAstNode
    {
        // just for graph construct purpose
        public BaseAstNode ParentL { get; set; }
        public BaseAstNode ParentR { get; set; }
        // just for graph construct purpose

        public BaseAstNode Condition { get; set; }
        public BaseAstNode WhileBody { get; set; }
    }

    class ForAstNode : BaseAstNode
    {
        // just for graph construct purpose
        public BaseAstNode ParentL { get; set; }
        public BaseAstNode ParentR { get; set; }
        // just for graph construct purpose

        public BaseAstNode Init { get; set; }
        public BaseAstNode Condition { get; set; }
        public BaseAstNode Close { get; set; }
        public BaseAstNode ForBody { get; set; }
    }

    class GotoAstNode : BaseAstNode
    {
        public BaseAstNode Label { get; set; }
    }

    class LabelAstNode : BaseAstNode
    {
    }

    class FunctionCallAstNode : BaseAstNode
    {
    }

    class ReturnAstNode : BaseAstNode
    {
        public BaseAstNode Return { get; set; }
    }

    class OperationAstNode : BaseAstNode
    {
        public int Priority { get; set; }
        public BaseAstNode Left { get; set; }
        public BaseAstNode Right { get; set; }
    }

    class VariableAstNode : BaseAstNode
    {
    }

    class NumberAstNode : BaseAstNode
    {
        public int Number { get; set; }
    }
}

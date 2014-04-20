using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class BaseModel
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
    }

    // modely lexikalni analyzy

    enum TokenTypes
    {
        // procesni "tokeny"
        End = 0,
        Error,
        Comment,
        Whitespace,
        // tokeny pro reservovana slova
        VarRW,
        FunctionRW,
        IfRW,
        ElseRW,
        ForRW,
        WhileRW,
        GotoRW,
        ReturnRW,
        // tokeny identifikatoru
        Identifier,
        // tokeny cisel
        Number,
        // tokeny zavorek
        ParenthesisLeft,
        ParenthesisRight,
        BraceLeft,
        BraceRight,
        // tokeny matematickych operatoru
        PlusPlus,
        MinusMinus,
        Multi,
        Plus,
        Minus,
        Equals,
        // token pro porovnani
        Less,
        More,
        LessOrEquals,
        MoreOrEquals,
        EqualsEquals,
        NotEquals,
        // tokeny logickych operatoru
        Neg,
        And,
        Or,
        // tokeny oddelovacu
        Semicolon,
        Comma,
        Colon,
    }

    class TokenModel : BaseModel
    {
        public TokenTypes Token { get; set; }
        public string TokenText { get; set; }
        public int TokenStartLine { get; set; }
        public int TokenStartColumn { get; set; }
    }

    // modely syntakticke analyzy

    enum AstNodeTypes
    {
        None = 0,
        // uzly programu
        Program,
        Function,
        Block,
        // uzly prikazu
        If,
        While,
        For,
        Goto,
        Label,
        FunctionCall,
        Return,
        // uzly vyrazu
        Variable,
        Number,
        Operator,

        // uzly upravenych prikazu
        Condition,
    }

    class BaseAst : TokenModel
    {
        public AstNodeTypes AstType { get; set; }

        public IaNode Node { get; set; }

        public static BaseAst GetEndAstNode()
        {
            return new BaseAst { Token = TokenTypes.End };
        }

        public static BaseAst GetInitLoopAstNode()
        {
            return new BaseAst { Token = TokenTypes.Comment };
        }

        public static BaseAst GetErrorAstNode(string errorMsg)
        {
            return new BaseAst { IsError = true, ErrorMessage = errorMsg, Token = TokenTypes.Error };
        }
    }

    class BlockAst : BaseAst
    {
        private List<BaseAst> statements;
        public List<BaseAst> Statements
        {
            get { return statements ?? (statements = new List<BaseAst>()); }
        }
    }

    class FunctionAst : BaseAst
    {
        public BlockAst Body { get; set; }
    }

    class ConditionAst : BaseAst
    {
        public BaseAst Condition { get; set; }
    }

    class IfAst : ConditionAst
    {
        public BaseAst IfBody { get; set; }
        public BaseAst ElseBody { get; set; }

        public BaseAst ConvertCondition { get; set; }
        //public BaseAst ConvertIfBody { get; set; }
        //public BaseAst ConvertElseBody { get; set; }
    }

    class WhileAst : ConditionAst
    {
        public BaseAst WhileBody { get; set; }

        public BaseAst ConvertCondition { get; set; }
        //public BaseAst ConvertWhileBody { get; set; }
    }

    class ForAst : ConditionAst
    {
        public BaseAst Init { get; set; }
        public BaseAst Close { get; set; }
        public BaseAst ForBody { get; set; }

        public BaseAst ConvertInit { get; set; }
        public BaseAst ConvertCondition { get; set; }
        public BaseAst ConvertClose { get; set; }
        //public BaseAst ConvertForBody { get; set; }
    }

    class GotoAst : BaseAst
    {
        public string Label { get; set; }
    }

    class NumberAst : BaseAst
    {
        public int Number { get; set; }
    }

    class OperatorAst : BaseAst
    {
        public int Priority { get; set; }
        public BaseAst Left { get; set; }
        public BaseAst Right { get; set; }
    }

    class ProgramAst : BaseAst
    {
        public ProgramAst()
        {
            AstType = AstNodeTypes.Program;
        }

        private List<string> vars;
        public List<string> Vars
        {
            get { return vars ?? (vars = new List<string>()); }
        }

        private Dictionary<string, BaseAst> varsDecl;
        public Dictionary<string, BaseAst> VarsDecl
        {
            get { return varsDecl ?? (varsDecl = new Dictionary<string, BaseAst>()); }
        }

        private Dictionary<string, FunctionAst> origFncs;
        public Dictionary<string, FunctionAst> OrigFncs
        {
            get { return origFncs ?? (origFncs = new Dictionary<string, FunctionAst>()); }
        }

        private Dictionary<string, FunctionAst> convFncs;
        public Dictionary<string, FunctionAst> ConvFncs
        {
            get { return convFncs ?? (convFncs = new Dictionary<string, FunctionAst>()); }
        }

        private Dictionary<string, IaNode> graph;
        public Dictionary<string, IaNode> Graph
        {
            get { return graph ?? (graph = new Dictionary<string, IaNode>()); }
        }

        private Dictionary<string, IaNode> lastNode;
        public Dictionary<string, IaNode> LastNode
        {
            get { return lastNode ?? (lastNode = new Dictionary<string, IaNode>()); }
        }
    }

    // modely grafu

    class IaNode
    {
        public GeneratorSet GeneratorSet { get; set; }

        public GeneratorSet FunctionGSet { get; set; }

        public LinearEquations LinearEquations { get; set; }

        public string FncName { get; set; }

        public string Name { get; set; }

        public BaseAst ReverseAst { get; set; }

        public IaEdge Next { get; set; } // for all statements

        public IaEdge IsTrue { get; set; } // for if statement
        public IaEdge IsFalse { get; set; } // for if statement

        public IEnumerable<IaEdge> Edges
        {
            get
            {
                if (IsTrue != null)
                    yield return IsTrue;
                if (IsFalse != null)
                    yield return IsFalse;
                if (Next != null)
                    yield return Next;
            }
        }
    }

    class IaEdge
    {
        public TransitionMatrixSet MatrixSet { get; set; }

        public string Name { get; set; }

        public BaseAst Ast { get; set; }

        public IaNode From { get; set; }
        public IaNode To { get; set; }
    }

    class NodeVector
    {
        public IaNode Node { get; set; }
        public LeadVector Vector { get; set; }
    }

    class NodeMatrix
    {
        public IaNode Node { get; set; }
        public long[][] Matrix { get; set; }
    }
}

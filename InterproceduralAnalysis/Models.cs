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

    // Lexical analysis models

    enum TokenType
    {
        // process "tokens"
        End = 0,
        Error,
        Comment,
        Whitespace,
        // reserved word tokens
        VarRW,
        FunctionRW,
        IfRW,
        ElseRW,
        ForRW,
        WhileRW,
        GotoRW,
        ReturnRW,
        // identifier tokens
        Identifier,
        // number tokens
        Number,
        // paranthesis tokens
        ParenthesisLeft,
        ParenthesisRight,
        BraceLeft,
        BraceRight,
        // expression operator tokens
        PlusPlus,
        MinusMinus,
        Multi,
        Plus,
        Minus,
        Equals,
        // comparision operator tokens
        Less,
        More,
        LessOrEquals,
        MoreOrEquals,
        EqualsEquals,
        NotEquals,
        // logical operator tokens
        Neg,
        And,
        Or,
        // delimiter tokens
        Semicolon,
        Comma,
        Colon,
    }

    class TokenModel : BaseModel
    {
        public TokenType Token { get; set; }
        public string TokenText { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // Lexical analysis models

    // Syntactic analysis models

    // Syntactic analysis models
}

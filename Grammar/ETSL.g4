grammar ETSL;

// ---------------------------------------------------------
// PARSER RULES
// ---------------------------------------------------------

/**
 * Entry point for the ETSL language.
 * A program consists of a sequence of declarations or top-level actions.
 */
program : (declaration | action)* EOF ;

/**
 * Declarations define the structure and state of the script.
 */
declaration 
    : eventDecl 
    | agentDecl 
    | varDecl 
    | funcDecl
    | action // Allows top-level statements for immediate execution
    ;

// Variable declaration (e.g., int x = 10;)
varDecl : type ID '=' expression ';' ;

// Function definition (e.g., function calculate(int n) { ... })
funcDecl : 'function' ID '(' paramList? ')' block ;

// Event definition for signaling within the system
eventDecl : 'event' ID '(' paramList? ')' ;

// Agent definition: triggers logic based on specific events
agentDecl : 'agent' ID 'on' '(' ID ')' ':=' action ;

// Parameter lists for function and event signatures
paramList : param (',' param)* ;
param : type ID ;

// Supported primitive types in ETSL
type : 'int' | 'bool' | 'float' | 'string' ;

// A block is a scoped collection of actions enclosed in curly braces
block : '{' action* '}' ;

/**
 * Actions represent executable statements that change state or control flow.
 */
action 
    : block                                     // Scoped block of code
    | varDecl                                   // Local variable declaration
    | 'log' expression ';'                      // Standard output / debugging
    | 'if' '(' expression ')' 'then' action      // Conditional branch
    | ID '(' argList? ')' ';'                   // Standalone function call
    | ID '=' expression ';'                     // Variable re-assignment
    | 'return' expression ';'                   // Function return statement
    | 'skip' ';'                                // No-operation placeholder
    ;

/**
 * Expression rules with labeled alternatives for the Visitor.
 * Priority is determined by the order of rules (e.g., MulDiv before AddSub).
 */
expression
    : '(' expression ')'                                        #ParenExpr
    | expression op=('*'|'/') expression                        #MulDivExpr
    | expression op=('+'|'-') expression                        #AddSubExpr
    | expression op=('=='|'!='|'>'|'<'|'>='|'<=') expression    #CompareExpr
    | BOOLEAN                                                   #BooleanExpr
    | ID                                                        #IdExpr
    | INT                                                       #IntExpr
    | FLOAT                                                     #FloatExpr
    | STRING                                                    #StringExpr
    | ID '(' argList? ')'                                       #CallExpr
    ;

// List of arguments passed to function or event calls
argList : expression (',' expression)* ;


// ---------------------------------------------------------
// LEXER RULES
// ---------------------------------------------------------

// Keywords (Must be defined before ID to avoid being parsed as identifiers)
EVENT : 'event' ;
AGENT : 'agent' ;
ON    : 'on' ;
IF    : 'if' ;
THEN  : 'then' ;
FUNC  : 'function' ;
RET   : 'return' ;
LOG   : 'log' ;

// Boolean Literals
BOOLEAN : 'true' | 'false' ;

// Identifiers: Standard alphanumeric naming (e.g., myVariable1)
ID    : [a-zA-Z] [a-zA-Z_0-9]* ;

// Numeric Literals
INT   : [0-9]+ ;
FLOAT : [0-9]+ '.' [0-9]+ ;

// String Literals: Supports escape characters
STRING : '"' ( ~["\\] | '\\' . )* '"' ;

// Hidden tokens (Ignored by the parser)
WS           : [ \t\r\n]+ -> skip ;             // Whitespace
COMMENT      : '/*' .*? '*/' -> skip ;          // Block comments
LINE_COMMENT : '//' ~[\r\n]* -> skip ;          // Line comments
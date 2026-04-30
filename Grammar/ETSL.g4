grammar ETSL;

// ---------------------------------------------------------
// PARSER RULES
// ---------------------------------------------------------

program : (declaration | action)* EOF ;

declaration 
    : eventDecl 
    | agentDecl 
    | varDecl 
    | funcDecl
    | action
    ;

varDecl : type ID '=' expression ';' ;

funcDecl : 'function' ID '(' paramList? ')' block ;

eventDecl : 'event' ID '(' paramList? ')' ;

agentDecl : 'agent' ID 'on' '(' ID ')' ':=' action ;

paramList : param (',' param)* ;
param : type ID ;

type : 'int' | 'bool' | 'float' | 'string' ;

block : '{' action* '}' ;

action 
    : block                                     #BlockAction
    | varDecl                                   #VarDeclAction
    | 'log' expression ';'                      #LogAction
    | 'if' '(' expression ')' 'then' action     #IfAction
    | 'return' expression ';'                   #ReturnAction
    | expression ';'                            #ExprAction
    | 'skip' ';'                                #SkipAction
    ;

expression
    : '(' expression ')'                                        #ParenExpr
    | expression op=('*'|'/') expression                        #MulDivExpr
    | expression op=('+'|'-') expression                        #AddSubExpr
    | expression op=('=='|'!='|'>'|'<'|'>='|'<=') expression    #CompareExpr
    | ID '=' expression                                         #AssignExpr
    | ID '(' argList? ')'                                       #CallExpr
    | BOOLEAN                                                   #BooleanExpr
    | ID                                                        #IdExpr
    | INT                                                       #IntExpr
    | FLOAT                                                     #FloatExpr
    | STRING                                                    #StringExpr
    ;

argList : expression (',' expression)* ;


// ---------------------------------------------------------
// LEXER RULES
// ---------------------------------------------------------

EVENT : 'event' ;
AGENT : 'agent' ;
ON    : 'on' ;
IF    : 'if' ;
THEN  : 'then' ;
FUNC  : 'function' ;
RET   : 'return' ;
LOG   : 'log' ;
SKIP_STMT : 'skip' ;

BOOLEAN : 'true' | 'false' ;

ID    : [a-zA-Z] [a-zA-Z_0-9]* ;

INT   : [0-9]+ ;
FLOAT : [0-9]+ '.' [0-9]+ ;

STRING : '"' ( ~["\\] | '\\' . )* '"' ;

WS           : [ \t\r\n]+ -> channel(HIDDEN) ;
COMMENT      : '/*' .*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT : '//' ~[\r\n]* -> channel(HIDDEN) ;
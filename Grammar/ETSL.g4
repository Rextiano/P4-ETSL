grammar ETSL;

// --- PARSER RULES ---
// The entry point of the program
program : (declaration)* EOF ;

// Declarations can be events or agents
declaration 
    : eventDecl 
    | agentDecl 
    ;

// Event definition: event Trade(int price, int volume)
eventDecl 
    : 'event' ID '(' paramList? ')' 
    ;

// Agent definition: agent MyAgent on (Trade) := { log "Trade seen" }
agentDecl 
    : 'agent' ID 'on' '(' ID ')' ':=' action 
    ;

paramList 
    : param (',' param)* ;

param 
    : type ID 
    ;

type 
    : 'int' | 'bool' | 'float' | 'string' 
    ;

// Actions: The logic inside an agent
action 
    : '{' action* '}'
    | 'log' STRING
    | 'skip'
    | 'if' '(' expression ')' 'then' action
    ;

expression 
    : ID
    | INT
    | FLOAT
    | STRING
    | expression ( '*' | '/' ) expression
    | expression ( '+' | '-' ) expression
    ;

// --- LEXER RULES ---
// Keywords from report
EVENT : 'event' ;
AGENT : 'agent' ;
ON    : 'on' ;
IF    : 'if' ;
THEN  : 'then' ;

// Tokens defined in report (Table 1.2.3)
ID     : [a-zA-Z] [a-zA-Z_0-9]* ;
INT    : [0-9]+ ;
FLOAT  : [0-9]+ '.' [0-9]+ ;
STRING : '"' ( ~["\\] | '\\' . )* '"' ;

// Ignore whitespace and comments
WS : [ \t\r\n]+ -> skip ;
COMMENT : '/*' .*? '*/' -> skip ;
LINE_COMMENT : '//' ~[\r\n]* -> skip ;